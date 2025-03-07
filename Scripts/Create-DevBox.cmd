@REM Usage: Create-DevBox.cmd ^<DEVBOX_ENDPOINT^> ^<DEVBOX_PROJECTNAME^> ^<DEVBOX_NAME^> ^<DEVBOX_PROJECT_POOLNAME^>
@REM Example: Create-DevBox.cmd https://f5e428a3-8470-4e16-aa8b-686ea3975139-raz-devcenter.eastus.devcenter.azure.com RazType MyScriptedDevBox RazType-Dev-User

@echo off
setlocal EnableDelayedExpansion

:: DevBox parameters
set "DEVBOX_ENDPOINT=%~1"
set "DEVBOX_PROJECTNAME=%~2"
set "DEVBOX_NAME=%~3"
set "DEVBOX_PROJECT_POOLNAME=%~4"

:: Check if required parameters are provided
if "%DEVBOX_ENDPOINT%"=="" (
    echo Error: DevBox endpoint not specified.
    echo Usage: Create-DevBox.cmd ^<DEVBOX_ENDPOINT^> ^<DEVBOX_PROJECTNAME^> ^<DEVBOX_NAME^> ^<DEVBOX_PROJECT_POOLNAME^>
    exit /b 1
)
if "%DEVBOX_PROJECTNAME%"=="" (
    echo Error: DevBox project name not specified.
    echo Usage: Create-DevBox.cmd ^<DEVBOX_ENDPOINT^> ^<DEVBOX_PROJECTNAME^> ^<DEVBOX_NAME^> ^<DEVBOX_PROJECT_POOLNAME^>
    exit /b 1
)
if "%DEVBOX_NAME%"=="" (
    echo Error: DevBox name not specified.
    echo Usage: Create-DevBox.cmd ^<DEVBOX_ENDPOINT^> ^<DEVBOX_PROJECTNAME^> ^<DEVBOX_NAME^> ^<DEVBOX_PROJECT_POOLNAME^>
    exit /b 1
)
if "%DEVBOX_PROJECT_POOLNAME%"=="" (
    echo Error: DevBox project pool name not specified.
    echo Usage: Create-DevBox.cmd ^<DEVBOX_ENDPOINT^> ^<DEVBOX_PROJECTNAME^> ^<DEVBOX_NAME^> ^<DEVBOX_PROJECT_POOLNAME^>
    exit /b 1
)

echo Getting device code for Azure login...
echo.

:: Get device code
curl -s -X POST ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "client_id=04b07795-8ddb-461a-bbee-02f9e1bf7b46" ^
    -d "scope=https://devcenter.azure.com/.default" ^
    "https://login.microsoftonline.com/common/oauth2/v2.0/devicecode" > device_code_response.json

:: Check if we got a valid response
type device_code_response.json | find "device_code" >nul
if !errorlevel! neq 0 (
    echo Error: Failed to get device code. Response:
    type device_code_response.json
    del device_code_response.json
    exit /b 1
)

:: Extract device_code and user_code using PowerShell for reliable JSON parsing
for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw device_code_response.json | ConvertFrom-Json; Write-Output $json.device_code"') do (
    set "DEVICE_CODE=%%a"
)

for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw device_code_response.json | ConvertFrom-Json; Write-Output $json.user_code"') do (
    set "USER_CODE=%%a"
)

:: Delete temporary file
del device_code_response.json

:: Copy the code to clipboard
echo !USER_CODE!| clip
echo Your code is: !USER_CODE! (automatically copied to clipboard)
echo.
echo Please open your browser and go to: https://microsoft.com/devicelogin
echo Then paste the code (Ctrl+V) to authenticate.
echo.
echo Waiting for authentication...

:: Initialize attempt counter
set ATTEMPTS=0
set MAX_ATTEMPTS=60

:poll_token
:: Increment attempts
set /a ATTEMPTS+=1
if !ATTEMPTS! gtr !MAX_ATTEMPTS! (
    echo Timeout waiting for authentication.
    echo Please run the script again.
    exit /b 1
)

curl -s -X POST ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "grant_type=device_code" ^
    -d "client_id=04b07795-8ddb-461a-bbee-02f9e1bf7b46" ^
    -d "device_code=!DEVICE_CODE!" ^
    "https://login.microsoftonline.com/common/oauth2/v2.0/token" > token_response.json

:: Check for specific status conditions in the token response
type token_response.json | find "authorization_pending" >nul
if !errorlevel! equ 0 (
    echo Still waiting for authentication in browser... Attempt: !ATTEMPTS! of !MAX_ATTEMPTS!
    del token_response.json
    timeout /t 5 >nul
    goto poll_token
)

type token_response.json | find "expired_token" >nul
if !errorlevel! equ 0 (
    echo The authentication request has expired. Please run the script again.
    del token_response.json
    exit /b 1
)

type token_response.json | find "invalid_grant" >nul
if !errorlevel! equ 0 (
    echo Invalid grant error detected. This often means the device code is malformed.
    echo Device code: [!DEVICE_CODE!]
    del token_response.json
    exit /b 1
)

:: Check if we have an access_token in the response (success case)
type token_response.json | find "access_token" >nul
if !errorlevel! neq 0 (
    :: No access token found and no known error condition detected
    echo Unknown error occurred while getting token:
    type token_response.json
    echo.
    echo Please try running the script again.
    del token_response.json
    exit /b 1
)

:: Extract access_token using PowerShell for reliable JSON parsing
for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw token_response.json | ConvertFrom-Json; Write-Output $json.access_token"') do (
    set "ACCESS_TOKEN=%%a"
)

:: Verify the token was extracted correctly
if "!ACCESS_TOKEN!"=="TOKEN_NOT_FOUND" (
    echo Failed to extract access token from the response.
    echo Response content:
    type token_response.json
    del token_response.json
    exit /b 1
)

:: Check if access token is empty
if "!ACCESS_TOKEN!"=="" (
    echo Extracted access token is empty. Something went wrong.
    echo Response content:
    type token_response.json
    del token_response.json
    exit /b 1
)

:: Token looks good, proceed with API call
echo.
echo Authentication successful!
echo.

:: Create JSON request body file with the pool name
echo {> request_body.json
echo   "poolName": "%DEVBOX_PROJECT_POOLNAME%">> request_body.json
echo }>> request_body.json

:: Create DevBox using the API
echo.
echo Creating DevBox at %DEVBOX_ENDPOINT%/projects/%DEVBOX_PROJECTNAME%/users/me/devboxes/%DEVBOX_NAME%
echo Request body:
type request_body.json
echo.

@REM echo.
@REM echo authorization: Bearer !ACCESS_TOKEN!
@REM echo .

:: Use curl command with better response capture
curl --request PUT ^
  --url "%DEVBOX_ENDPOINT%/projects/%DEVBOX_PROJECTNAME%/users/me/devboxes/%DEVBOX_NAME%?api-version=2025-02-01" ^
  --header "authorization: Bearer !ACCESS_TOKEN!" ^
  --header "content-type: application/json" ^
  --data @request_body.json ^
  -i ^
  -o response_full.txt 2> curl_error.txt

:: Check if the response file exists and has content
if not exist response_full.txt (
    echo Error: No response received from API. Check curl_error.txt for details.
    if exist curl_error.txt (
        echo Curl errors:
        type curl_error.txt
    )
    goto cleanup
)

:: Extract the HTTP status code from the response
for /f "tokens=2 delims= " %%s in ('findstr /B "HTTP" response_full.txt') do (
    set "HTTP_STATUS=%%s"
)

:: Check if the response contains success status
set success_status=0

:: Check for HTTP success codes
findstr /C:"HTTP/1.1 200" /C:"HTTP/1.1 201" /C:"HTTP/1.1 202" response_full.txt > nul
if !errorlevel! equ 0 set success_status=1

:: Also check for success status in JSON response if available
findstr /C:"\"status\":\"succeeded\"" /C:"\"status\":\"accepted\"" response_full.txt > nul
if !errorlevel! equ 0 set success_status=1

if !success_status! equ 1 (
    echo.
    echo DevBox creation request has been executed successfully, please wait 65 minutes for the provisioning to complete.
    echo.
) else (
    echo.
    echo DevBox creation encountered an error. Response details:
    echo HTTP Status: !HTTP_STATUS!
    echo.
    echo Full response:
    type response_full.txt
    echo.
    if exist curl_error.txt (
        echo Curl errors:
        type curl_error.txt
    )
)

:cleanup
:: Clean up temporary files
if exist response_full.txt del response_full.txt
if exist curl_error.txt del curl_error.txt
if exist token_response.json del token_response.json
if exist request_body.json del request_body.json

endlocal
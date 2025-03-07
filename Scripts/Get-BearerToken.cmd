@echo off
setlocal EnableDelayedExpansion

echo Getting device code for Azure login...
echo.

:: Get device code
curl -s -X POST ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "client_id=04b07795-8ddb-461a-bbee-02f9e1bf7b46" ^
    -d "scope=https://management.azure.com/.default" ^
    "https://login.microsoftonline.com/common/oauth2/v2.0/devicecode" > device_code_response.json

:: Check if we got a valid response
type device_code_response.json | find "device_code" >nul
if !errorlevel! neq 0 (
    echo Error: Failed to get device code. Response:
    type device_code_response.json
    del device_code_response.json
    exit /b 1
)

:: Display raw response for debugging
@REM echo Raw device code response:
@REM echo --------------------------------------------
@REM type device_code_response.json
@REM echo --------------------------------------------

:: Extract device_code and user_code using PowerShell for reliable JSON parsing
for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw device_code_response.json | ConvertFrom-Json; Write-Output $json.device_code"') do (
    set "DEVICE_CODE=%%a"
)

for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw device_code_response.json | ConvertFrom-Json; Write-Output $json.user_code"') do (
    set "USER_CODE=%%a"
)

:: Delete temporary file
del device_code_response.json

:: Debug information - show exact values
@REM echo Device code value: [!DEVICE_CODE!]
@REM echo User code value: [!USER_CODE!]

:: Copy the code to clipboard
echo !USER_CODE!| clip
echo Your code is: !USER_CODE! (automatically copied to clipboard)
echo.
echo Opening browser for authentication...
start https://microsoft.com/devicelogin
echo Please paste the code in your browser (Ctrl+V).
echo.
echo Waiting 30 seconds before checking authentication status...
timeout /t 30 >nul
echo Checking authentication status...
echo.

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

echo Polling for token... (Attempt !ATTEMPTS! of !MAX_ATTEMPTS!)
:: Poll for token with explicit error display and debug output
@REM echo API request parameters:
@REM echo grant_type: device_code
@REM echo client_id: 04b07795-8ddb-461a-bbee-02f9e1bf7b46
@REM echo device_code: [!DEVICE_CODE!]

curl -s -X POST ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "grant_type=device_code" ^
    -d "client_id=04b07795-8ddb-461a-bbee-02f9e1bf7b46" ^
    -d "device_code=!DEVICE_CODE!" ^
    "https://login.microsoftonline.com/common/oauth2/v2.0/token" > token_response.json

:: Display raw token response with improved formatting
@REM echo Raw token response:
@REM echo --------------------------------------------
@REM type token_response.json
@REM echo --------------------------------------------

:: Extract access_token using PowerShell for reliable JSON parsing
for /f "tokens=* delims=" %%a in ('powershell -Command "$json = Get-Content -Raw token_response.json | ConvertFrom-Json; Write-Output $json.access_token"') do (
    set "ACCESS_TOKEN=%%a"
)

:: Extract User Object ID
for /f "tokens=* delims=" %%a in ('powershell -Command "try { $token = $env:ACCESS_TOKEN; $tokenPayload = $token.Split('.')[1]; while ($tokenPayload.Length %% 4) { $tokenPayload += '=' }; $tokenJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenPayload)) | ConvertFrom-Json; Write-Output $tokenJson.oid } catch { Write-Output '' }"') do (
    set "USER_ID=%%a"
)

:: Check if access token was found
if defined ACCESS_TOKEN (
    if "!ACCESS_TOKEN!" NEQ "" (
        echo.
        echo Your bearer token is:
        echo !ACCESS_TOKEN!
        
        del token_response.json
        goto end
    )
)

:: Check for other responses
type token_response.json | find "authorization_pending" >nul
if !errorlevel! equ 0 (
    echo The authentication is still pending. Please complete the authentication in the browser.
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

echo Unknown error occurred while getting token:
type token_response.json
echo.
echo Please try running the script again.
del token_response.json
exit /b 1

:end
endlocal
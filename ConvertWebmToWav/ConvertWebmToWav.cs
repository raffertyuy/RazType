#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    var temp = @"D:\home\site\wwwroot\temp\" + Guid.NewGuid().ToString() + ".webm";
    var tempOut = @"D:\home\site\wwwroot\temp\" + Guid.NewGuid().ToString() + ".wav";

    try
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        int commaStart = requestBody.IndexOf(",") + 1; // remove "data:audio/webm;base64," and similar 
        string webmstrdata = requestBody.Substring(commaStart).Trim();
        log.LogInformation(webmstrdata);
        
        try {
            byte[] webmbytesdata = Convert.FromBase64String(webmstrdata);
            File.WriteAllBytes(temp, webmbytesdata);
        }
        catch (Exception ex) {
            log.LogInformation("Exception1: " + ex.Message + " (TRYING AGAIN)");
            
            // Try unescape json string, made the string invalid according to https://base64.guru/tools/validator
            //webmstrdata = JsonConvert.DeserializeObject<string>(webmstrdata);

            // Try suggestion from https://stackoverflow.com/questions/50524665/convert-frombase64string-throws-invalid-base-64-string-error
            //webmstrdata = webmstrdata.Replace('-', '+').Replace('_', '/');
            webmstrdata = webmstrdata.Replace('-', '+').Replace('_', '/').PadRight(4*((webmstrdata.Length+3)/4), '=');
            log.LogInformation(webmstrdata);

            byte[] webmbytesdata = Convert.FromBase64String(webmstrdata);
            File.WriteAllBytes(temp, webmbytesdata);
        }
    }
    catch (Exception ex)
    {
        log.LogInformation(ex.Message);
        return new BadRequestObjectResult("Exception2: " + ex.Message);
    }

    try {
        try {
            var psi = new ProcessStartInfo();
            psi.FileName = @"D:\home\site\wwwroot\tools\ffmpeg.exe";
            psi.Arguments = $"-i \"{temp}\" \"{tempOut}\"";
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;

            log.LogInformation($"Args: {psi.Arguments}");
            var process = Process.Start(psi);
            process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds);

            string stdoutput = process.StandardOutput.ReadToEnd();
            string stderror = process.StandardError.ReadToEnd();
            // log.LogInformation("FFMPEG: exitcode: " + process.ExitCode + "\r\n" + stdoutput + "\r\n" + stderror);
            // log.LogInformation("FFMPEG: stdoutput: " + stdoutput);
            // log.LogInformation("FFMPEG: stderror: " + stderror);

            if (process.ExitCode != 0)
                return new BadRequestObjectResult("Exception3: " + stderror);
        }
        catch(Exception ex) {
            log.LogInformation(ex.Message);

            return new BadRequestObjectResult("Exception4: " + ex.Message);
        }

        var bytes = File.ReadAllBytes(tempOut);

        await Task.Run(() => { });
        return new FileContentResult(bytes, "audio/wav");
    }
    catch (Exception ex) {
        log.LogInformation(ex.Message);

        return new BadRequestObjectResult("Exception5: " + ex.Message);
    }
    finally {
        File.Delete(tempOut);
        File.Delete(temp);
    }
}
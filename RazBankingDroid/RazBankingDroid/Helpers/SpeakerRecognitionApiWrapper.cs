using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Newtonsoft.Json;

namespace RazBankingDroid.Helpers
{
    /// <summary>
    /// A wrapper class to invoke Speaker Authentication REST APIs
    /// </summary>
    public class SpeakerRecognitionApiWrapper
    {
        public const string API_BASE_URL = "https://api.projectoxford.ai/spid/v1.0";
        public const string VERIFICATION_PROFILE_BASE_URL = "https://api.projectoxford.ai/spid/v1.0/verificationProfiles";

        private readonly string _accountKey;

        public SpeakerRecognitionApiWrapper(string accountKey)
        {
            _accountKey = accountKey;
        }

        public static byte[] AudioFileToBytes(string audioFilePath)
        {
            byte[] audioBytes = null;
            using (FileStream fsSource = new FileStream(audioFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the source file into a byte array.
                audioBytes = new byte[fsSource.Length];
                int numBytesToRead = (int)fsSource.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = fsSource.Read(audioBytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }

                fsSource.Close();
            }

            return audioBytes;
        }

        private void InitializeRequest(HttpWebRequest request, string method)
        {
            request.ContentType = "application/json";
            request.ContentLength = 0;
            request.Method = method;
            request.Headers.Add("Ocp-Apim-Subscription-Key", _accountKey);

            request.KeepAlive = false;
            //request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLimit = 1;
        }

        public OperationStatus GetOperationStatus(string operationId)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(API_BASE_URL, "/operations/", operationId));
            InitializeRequest(request, "GET");

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    return JsonConvert.DeserializeObject<OperationStatus>(responseString);
                }
            }
        }

        public VerificationResult Verify(string profileId, byte[] audioBytes)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(API_BASE_URL, "/verify?verificationProfileId=", profileId));
            InitializeRequest(request, "POST");
            request.ContentType = "multipart/form-data"; // also try "application/octet-stream"
            request.ContentLength = audioBytes.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(audioBytes, 0, audioBytes.Length);
            }

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    return JsonConvert.DeserializeObject<VerificationResult>(responseString);
                }
            }
        }

        public List<string> GetVerificationPhrases()
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(API_BASE_URL, "/verificationPhrases?locale=en-US"));
            InitializeRequest(request, "GET");

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>[]>(responseString);
                    var list = new List<string>();
                    foreach (var d in responseDictionary)
                        list.Add(d.Values.First());

                    return list;
                }
            }
        }

        #region Verification Profile
        public string CreateVerificationProfile()
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(VERIFICATION_PROFILE_BASE_URL);
            InitializeRequest(request, "POST");
            var byteData = Encoding.UTF8.GetBytes("{\"locale\":\"en-US\"}");
            request.ContentLength = byteData.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(byteData, 0, byteData.Length);
            }

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                    if (responseDictionary.ContainsKey("verificationProfileId"))
                    {
                        return responseDictionary["verificationProfileId"];
                    }
                    else if (responseDictionary.ContainsKey("error"))
                    {
                        var errorMessage = responseDictionary["error"];
                        throw new WebException(errorMessage);
                    }
                    else
                        throw new WebException();
                }
            }
        }

        public void DeleteVerificationProfile(string profileId)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(VERIFICATION_PROFILE_BASE_URL, "/", profileId));
            InitializeRequest(request, "DELETE");

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    if (!string.IsNullOrEmpty(responseString))
                    {
                        var responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                        if (responseDictionary.ContainsKey("error"))
                        {
                            var errorMessage = responseDictionary["error"];
                            throw new WebException(errorMessage);
                        }
                    }
                }
            }
        }

        public VerificationEnrollmentResult CreateVerificationEnrollment(string profileId, byte[] audioBytes)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(VERIFICATION_PROFILE_BASE_URL, "/", profileId, "/enroll"));
            InitializeRequest(request, "POST");
            request.ContentType = "multipart/form-data"; // also try "application/octet-stream"
            request.ContentLength = audioBytes.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(audioBytes, 0, audioBytes.Length);
            }

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    return JsonConvert.DeserializeObject<VerificationEnrollmentResult>(responseString);
                }
            }
        }

        public void ResetVerificationEnrollments(string profileId)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(string.Concat(VERIFICATION_PROFILE_BASE_URL, "/", profileId, "/reset"));
            InitializeRequest(request, "POST");

            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();

                    if (!string.IsNullOrEmpty(responseString))
                        throw new WebException(responseString);
                }
            }
        }
        #endregion
    }
}
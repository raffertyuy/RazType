using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RazBankingDroid.Helpers;
using System.Threading.Tasks;

namespace RazBankingDroid.Tests
{
    [TestClass]
    public class SpeakerRecognitionApiWrapperTests
    {
        [TestMethod]
        public void CreateAndDeleteProfileTest()
        {
            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");
            var profileId = speaker.CreateVerificationProfile();

            if (string.IsNullOrEmpty(profileId))
                Assert.Fail();

            Debug.WriteLine(profileId);
            speaker.DeleteVerificationProfile(profileId);
        }

        [TestMethod]
        public void GetVerificationPhrasesTest()
        {
            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");
            var phrases = speaker.GetVerificationPhrases();

            phrases.ForEach(x => Debug.WriteLine(x));
        }

        [TestMethod]
        public void ResetVerificationEnrollmentsTest()
        {
            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");

            var profileId = speaker.CreateVerificationProfile();
            try
            {
                speaker.ResetVerificationEnrollments(profileId);
            }
            finally
            {
                speaker.DeleteVerificationProfile(profileId);
            }
        }

        [TestMethod]
        public void EnrollAndVerifyTest()
        {
            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");
            var profileId = speaker.CreateVerificationProfile();

            try
            {
                // Enroll
                string[] enrollWavs = {
                    "C:\\Rafferty\\GitProjects\\RazType\\RazBankingDroid\\RazBankingDroid.Tests\\Resources\\enrollment-1.wav",
                    "C:\\Rafferty\\GitProjects\\RazType\\RazBankingDroid\\RazBankingDroid.Tests\\Resources\\enrollment-2.wav",
                    "C:\\Rafferty\\GitProjects\\RazType\\RazBankingDroid\\RazBankingDroid.Tests\\Resources\\enrollment-3.wav"
                };

                foreach (var filepath in enrollWavs)
                {
                    byte[] audioBytes = null;
                    using (FileStream fsSource = new FileStream(filepath, FileMode.Open, FileAccess.Read))
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
                    }

                    var result = speaker.CreateVerificationEnrollment(profileId, audioBytes);

                    Debug.WriteLine("enrollmentStatus: {0}", result.enrollmentStatus);
                    Debug.WriteLine("enrollmentsCount: {0}", result.enrollmentsCount);
                    Debug.WriteLine("remainingEnrollments: {0}", result.remainingEnrollments);
                    Debug.WriteLine("phrase: {0}", result.phrase);
                }

                // Verify
                const string verifyWav = "C:\\Rafferty\\GitProjects\\RazType\\RazBankingDroid\\RazBankingDroid.Tests\\Resources\\verify.wav";
                byte[] verifyBytes = null;
                using (FileStream fsSource = new FileStream(verifyWav, FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    verifyBytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(verifyBytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                }

                var verificationResult = speaker.Verify(profileId, verifyBytes);

                Debug.WriteLine("result: {0}", verificationResult.result);
                Debug.WriteLine("confidence: {0}", verificationResult.confidence);
                Debug.WriteLine("phrase: {0}", verificationResult.phrase);
            }
            finally
            {
                speaker.DeleteVerificationProfile(profileId);
            }
        }
    }
}

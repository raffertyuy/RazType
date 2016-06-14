using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using RazSpeakerAuthentication.Helpers;
using System.Threading.Tasks;

namespace RazSpeakerAuthentication.Tests
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
        public void CreateVerificationEnrollmentTest()
        {
            const string testWav = "C:\\Rafferty\\GitProjects\\RazType\\RazSpeakerAuthentication\\RazSpeakerAuthentication.Tests\\audio\\enrollment-1.wav";

            byte[] audioBytes = null;
            using (FileStream fsSource = new FileStream(testWav, FileMode.Open, FileAccess.Read))
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

            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");
            var result = speaker.CreateVerificationEnrollment("47db6d9a-fc0e-4bb4-bc0c-3b3d58436f80", audioBytes);

            Debug.WriteLine("enrollmentStatus: {0}", result.enrollmentStatus);
            Debug.WriteLine("enrollmentsCount: {0}", result.enrollmentsCount);
            Debug.WriteLine("remainingEnrollments: {0}", result.remainingEnrollments);
            Debug.WriteLine("phrase: {0}", result.phrase);
        }

        [TestMethod]
        public void VerifyTest()
        {
            const string testWav = "C:\\Rafferty\\GitProjects\\RazType\\RazSpeakerAuthentication\\RazSpeakerAuthentication.Tests\\audio\\verify.wav";

            byte[] audioBytes = null;
            using (FileStream fsSource = new FileStream(testWav, FileMode.Open, FileAccess.Read))
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

            var speaker = new SpeakerRecognitionApiWrapper("cfdb5c6532ca469aa7a7f8d74ab93ca9");
            var result = speaker.Verify("47db6d9a-fc0e-4bb4-bc0c-3b3d58436f80", audioBytes);

            Debug.WriteLine("result: {0}", result.result);
            Debug.WriteLine("confidence: {0}", result.confidence);
            Debug.WriteLine("phrase: {0}", result.phrase);
        }
    }
}

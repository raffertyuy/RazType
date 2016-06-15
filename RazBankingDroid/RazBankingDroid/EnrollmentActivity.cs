using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System.Threading;
using RazBankingDroid.Helpers;
using Microsoft.Azure.Engagement.Xamarin.Activity;

namespace RazBankingDroid
{
    [Activity(Label = "Set-up Voice Verification")]
    public class EnrollmentActivity : EngagementActivity
    {
        //private AudioRecorder _audioRecorder;
        private SoundRecorderAsync _recorder;
        private SpeakerRecognitionApiWrapper _api;
        private string _profileId;

        private TextView txtVerificationPhrases;
        private EditText txtVerificationPhrase;
        private EditText txtRemainingEnrollments;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Button btnResetProfile;
        private Button btnEnrollmentToMain;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Enrollment);

            SetControlHandlers();
            EnableButtons(false);

            _recorder = new SoundRecorderAsync();

            _api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            ShowAvailableEnrollmentPhrases();
            GetOrCreateProfileId();
        }

        private void SetControlHandlers()
        {
            txtVerificationPhrases = FindViewById<TextView>(Resource.Id.txtVerificationPhrases);
            txtVerificationPhrase = FindViewById<EditText>(Resource.Id.txtVerificationPhrase);
            txtRemainingEnrollments = FindViewById<EditText>(Resource.Id.txtRemainingEnrollments);
            btnStartRecording = FindViewById<Button>(Resource.Id.btnStartRecording);
            btnStopRecording = FindViewById<Button>(Resource.Id.btnStopRecording);
            btnResetProfile = FindViewById<Button>(Resource.Id.btnResetProfile);
            btnEnrollmentToMain = FindViewById<Button>(Resource.Id.btnEnrollmentToMain);

            btnStartRecording.Click += btnStartRecording_Click;
            btnStopRecording.Click += btnStopRecording_Click;
            btnResetProfile.Click += btnResetProfile_Click;
            btnEnrollmentToMain.Click += btnEnrollmentToMain_Click;
        }

        private void btnStartRecording_Click(object sender, EventArgs e)
        {
            EnableButtons(true);
            _recorder.StartRecording();
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            _recorder.StopRecording();

            EnrollRecording();
            EnableButtons(false);
        }

        private void btnResetProfile_Click(object sender, EventArgs e)
        {
            var api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            api.ResetVerificationEnrollments(_profileId);
        }

        private void btnEnrollmentToMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        private void EnableButtons(bool isRecording)
        {
            btnStartRecording.Enabled = !isRecording;
            btnStopRecording.Enabled = isRecording;
            btnStopRecording.Enabled = !isRecording;
        }

        private void GetOrCreateProfileId()
        {
            _profileId = UserSettingsHelper.RetrieveProfileId();
            if (!string.IsNullOrEmpty(_profileId))
                return;

            _profileId = _api.CreateVerificationProfile();
            if (string.IsNullOrEmpty(_profileId))
                throw new ApplicationException("Error creating Verification Profile ID.");

            System.Diagnostics.Debug.WriteLine("Verification Profile ID: {0}", _profileId);
            UserSettingsHelper.SaveProfileId(_profileId);
        }

        private void ShowAvailableEnrollmentPhrases()
        {
            var phrases = _api.GetVerificationPhrases();
            txtVerificationPhrase.Text = string.Join("\n", phrases.ToArray());
        }

        private void EnrollRecording()
        {
            byte[] audioBytes = null;
            using (FileStream fsSource = new FileStream(Constants.WAV_FILE_PATH, FileMode.Open, FileAccess.Read))
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

            var result = _api.CreateVerificationEnrollment(_profileId, audioBytes);
            txtRemainingEnrollments.Text = result.remainingEnrollments.ToString();
            txtVerificationPhrase.Text = result.phrase;
        }
    }
}
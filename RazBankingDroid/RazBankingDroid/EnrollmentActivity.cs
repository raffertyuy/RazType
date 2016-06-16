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
using System.Threading.Tasks;

namespace RazBankingDroid
{
    [Activity(Label = "Set-up Voice Verification")]
    public class EnrollmentActivity : EngagementActivity
    {
        private LowLevelRecorder _recorder;
        private SpeakerRecognitionApiWrapper _api;
        private string _profileId;
        private bool _isRecording;
        private bool _haveRecording;

        private TextView txtVerificationPhrases;
        private TextView txtVerificationPhrase;
        private TextView txtRemainingEnrollments;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Button btnResetProfile;
        private Button btnEnrollmentToMain;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Enrollment);

            SetControlHandlers();

            _recorder = new LowLevelRecorder();
            _isRecording = false;
            _haveRecording = false;
            HandleButtonState();

            _api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            ShowAvailableEnrollmentPhrases();
            GetOrCreateProfileId();
        }

        private void SetControlHandlers()
        {
            txtVerificationPhrases = FindViewById<TextView>(Resource.Id.txtVerificationPhrases);
            txtVerificationPhrase = FindViewById<TextView>(Resource.Id.txtVerificationPhrase);
            txtRemainingEnrollments = FindViewById<TextView>(Resource.Id.txtRemainingEnrollments);
            btnStartRecording = FindViewById<Button>(Resource.Id.btnStartRecording);
            btnStopRecording = FindViewById<Button>(Resource.Id.btnStopRecording);
            btnResetProfile = FindViewById<Button>(Resource.Id.btnResetProfile);
            btnEnrollmentToMain = FindViewById<Button>(Resource.Id.btnEnrollmentToMain);

            btnStartRecording.Click += btnStartRecording_Click;
            btnStopRecording.Click += btnStopRecording_Click;
            btnResetProfile.Click += btnResetProfile_Click;
            btnEnrollmentToMain.Click += btnEnrollmentToMain_Click;
        }

        private async void btnStartRecording_Click(object sender, EventArgs e)
        {
            try
            {
                StartOperationAsync(_recorder);
                _isRecording = true;
                _haveRecording = true;
                HandleButtonState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Message: {0}", ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", ex.StackTrace);
            }
        }

        private void btnStopRecording_Click(object sender, EventArgs e)
        {
            try
            {
                StopOperation(_recorder);
                _isRecording = false;
                _recorder.RecordingStateChanged += (recording) => {
                    if (!_isRecording)
                        HandleButtonState();

                    _recorder.RecordingStateChanged = null;

                    EnrollRecording();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Message: {0}", ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", ex.StackTrace);
            }
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

        private void HandleButtonState()
        {
            btnStartRecording.Enabled = !_isRecording;
            btnStopRecording.Enabled = _isRecording;
            btnResetProfile.Enabled = !_isRecording;
        }

        async Task StartOperationAsync(INotificationReceiver nRec)
        {
            //if (useNotifications)
            //{
            //    bool haveFocus = nMan.RequestAudioResources(nRec);
            //    if (haveFocus)
            //    {
            //        status.Text = "Granted";
            //        await nRec.StartAsync();
            //    }
            //    else
            //    {
            //        status.Text = "Denied";
            //    }
            //}
            //else
            //{
                await nRec.StartAsync();
            //}
        }

        void StopOperation(INotificationReceiver nRec)
        {
            nRec.Stop();
            //if (useNotifications)
            //{
            //    nMan.ReleaseAudioResources();
            //    status.Text = "Released";
            //}
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
            txtVerificationPhrases.Text = string.Join("\n", phrases.ToArray());
        }

        private void EnrollRecording()
        {
            //FileStream fileStream = new FileStream(_recorder.WavFileName, FileMode.Open, FileAccess.Read);
            //BinaryReader binaryReader = new BinaryReader(fileStream);
            //long totalBytes = new System.IO.FileInfo(_recorder.WavFileName).Length;
            //byte[] audioBytes = binaryReader.ReadBytes((Int32)totalBytes);
            //fileStream.Close();
            //fileStream.Dispose();
            //binaryReader.Close();

            byte[] audioBytes = null;
            using (FileStream fsSource = new FileStream(_recorder.WavFileName, FileMode.Open, FileAccess.Read))
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

            VerificationEnrollmentResult result = null;
            try
            {
                result = _api.CreateVerificationEnrollment(_profileId, audioBytes);

                txtRemainingEnrollments.Text = result.remainingEnrollments.ToString();
                txtVerificationPhrase.Text = result.phrase;
            }
            catch (Exception ex)
            {
                txtVerificationPhrase.Text = "Unrecognized, please try again.";

                System.Diagnostics.Debug.WriteLine("Message: {0}", ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", ex.StackTrace);
            }
        }
    }
}
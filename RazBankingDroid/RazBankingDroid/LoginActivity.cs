using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Azure.Engagement.Xamarin.Activity;
using RazBankingDroid.Helpers;
using System.Threading.Tasks;

namespace RazBankingDroid
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity : EngagementActivity
    {
        private LowLevelRecorder _recorder;
        private SpeakerRecognitionApiWrapper _api;
        private string _profileId;
        private bool _isRecording;

        private bool _haveRecording;
        private TextView txtStatus;
        private TextView txtConfidenceLevel;
        private TextView txtLoginVerificationPhrase;
        private Button btnLoginStartRecording;
        private Button btnLoginStopRecording;
        private Button btnLogin;
        private Button btnLoginToMain;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Login);
            SetControlHandlers();

            _recorder = new LowLevelRecorder();
            _isRecording = false;
            _haveRecording = false;

            btnLogin.Enabled = false;
            HandleRecordingButtonState();

            _api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            GetProfileId();
        }

        private void SetControlHandlers()
        {
            txtStatus = FindViewById<TextView>(Resource.Id.txtStatus);
            txtConfidenceLevel = FindViewById<TextView>(Resource.Id.txtConfidenceLevel);
            txtLoginVerificationPhrase = FindViewById<TextView>(Resource.Id.txtLoginVerificationPhrase);
            btnLoginStartRecording = FindViewById<Button>(Resource.Id.btnLoginStartRecording);
            btnLoginStopRecording = FindViewById<Button>(Resource.Id.btnLoginStopRecording);
            btnLogin = FindViewById<Button>(Resource.Id.btnLogin);
            btnLoginToMain = FindViewById<Button>(Resource.Id.btnLoginToMain);

            btnLoginStartRecording.Click += btnLoginStartRecording_Click;
            btnLoginStopRecording.Click += btnLoginStopRecording_Click;
            btnLogin.Click += btnLogin_Click;
            btnLoginToMain.Click += btnLoginToMain_Click;
        }

        private async void btnLoginStartRecording_Click(object sender, EventArgs e)
        {
            try
            {
                StartOperationAsync(_recorder);
                _isRecording = true;
                _haveRecording = true;
                HandleRecordingButtonState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private void btnLoginStopRecording_Click(object sender, EventArgs e)
        {
            try
            {
                StopOperation(_recorder);
                _isRecording = false;
                _recorder.RecordingStateChanged += (recording) => {
                    if (!_isRecording)
                        HandleRecordingButtonState();

                    _recorder.RecordingStateChanged = null;

                    btnLogin.Enabled = VerifyRecording();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(AccountActivity));
            StartActivity(intent);
        }

        private void btnLoginToMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        private void HandleRecordingButtonState()
        {
            btnLoginStartRecording.Enabled = !_isRecording;
            btnLoginStopRecording.Enabled = _isRecording;
        }

        private void GetProfileId()
        {
            _profileId = UserSettingsHelper.RetrieveProfileId();
            bool validProfile = false;

            try
            {
                var profile = _api.GetVerificationProfile(_profileId);
                validProfile = _profileId == profile.verificationProfileId;
            }
            catch
            {
                validProfile = false;
                _profileId = null;
            }

            if (validProfile && !string.IsNullOrEmpty(_profileId))
                return;

            txtStatus.Text = "Profile Id not found. Please Set-up your Voice Verification.";
            btnLoginStartRecording.Enabled = false;
            btnLoginStopRecording.Enabled = false;
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

        private bool VerifyRecording()
        {
            var audioBytes = SpeakerRecognitionApiWrapper.AudioFileToBytes(_recorder.WavFileName);
            VerificationResult result = null;
            try
            {
                result = _api.Verify(_profileId, audioBytes);

                txtStatus.Text = result.result;
                txtConfidenceLevel.Text = result.confidence;
                txtLoginVerificationPhrase.Text = result.phrase;

                return result.result == "Accept";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Unrecognized, please try again.";

                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }

            return false;
        }
    }
}
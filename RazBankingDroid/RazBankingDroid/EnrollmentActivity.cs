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
        private Button btnStartStopRecording;
        private Button btnResetProfile;
        private Button btnEnrollmentToLogin;
        private Button btnEnrollmentToMain;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Enrollment);

            SetControlHandlers();

            _api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            ShowAvailableEnrollmentPhrases();
            GetOrCreateProfileId();

            _recorder = new LowLevelRecorder();
            _isRecording = false;
            HandleButtonState();
        }

        private void SetControlHandlers()
        {
            txtVerificationPhrases = FindViewById<TextView>(Resource.Id.txtVerificationPhrases);
            txtVerificationPhrase = FindViewById<TextView>(Resource.Id.txtVerificationPhrase);
            txtRemainingEnrollments = FindViewById<TextView>(Resource.Id.txtRemainingEnrollments);
            btnStartStopRecording = FindViewById<Button>(Resource.Id.btnStartStopRecording);
            btnResetProfile = FindViewById<Button>(Resource.Id.btnResetProfile);
            btnEnrollmentToLogin = FindViewById<Button>(Resource.Id.btnEnrollmentToLogin);
            btnEnrollmentToMain = FindViewById<Button>(Resource.Id.btnEnrollmentToMain);

            btnStartStopRecording.Click += btnStartStopRecording_Click;
            btnResetProfile.Click += btnResetProfile_Click;
            btnEnrollmentToLogin.Click += btnEnrollmentToLogin_Click;
            btnEnrollmentToMain.Click += btnEnrollmentToMain_Click;
        }

        private async void btnStartStopRecording_Click(object sender, EventArgs e)
        {
            if (!_isRecording) // Start Recording
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
                    System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                }
            }
            else
            {
                try
                {
                    StopOperation(_recorder);
                    _isRecording = false;
                    _recorder.RecordingStateChanged += (recording) => {
                        _recorder.RecordingStateChanged = null;

                        EnrollRecording();

                        if (!_isRecording)
                            HandleButtonState();
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                }
            }
        }

        private void btnResetProfile_Click(object sender, EventArgs e)
        {
            var api = new SpeakerRecognitionApiWrapper(Constants.SPEAKER_RECOGNITION_ACCOUNT_KEY);
            api.ResetVerificationEnrollments(_profileId);

            txtVerificationPhrase.Text = "Please start recording with one of the above phrases";
            txtRemainingEnrollments.Text = "3";

            HandleButtonState();
        }

        private void btnEnrollmentToLogin_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(LoginActivity));
            StartActivity(intent);
        }

        private void btnEnrollmentToMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }

        private void HandleButtonState()
        {
            if (_isRecording)
            {
                btnStartStopRecording.Text = "Stop Recording";
                btnStartStopRecording.SetBackgroundResource(Resource.Drawable.button_record_bg);

                btnResetProfile.SetBackgroundResource(Resource.Drawable.button_disabled_bg);
                btnResetProfile.Enabled = false;

                btnEnrollmentToLogin.SetBackgroundResource(Resource.Drawable.button_disabled_bg);
                btnResetProfile.Enabled = false;
            }
            else
            {
                btnStartStopRecording.Text = "Start Recording";
                btnStartStopRecording.SetBackgroundResource(Resource.Drawable.button_active_bg);

                if (_haveRecording)
                {
                    btnResetProfile.SetBackgroundResource(Resource.Drawable.button_active_bg);
                    btnResetProfile.Enabled = true;

                    if (txtRemainingEnrollments.Text == "0")
                    {
                        btnEnrollmentToLogin.SetBackgroundResource(Resource.Drawable.button_active_bg);
                        btnResetProfile.Enabled = true;
                    }
                    else
                    {
                        btnEnrollmentToLogin.SetBackgroundResource(Resource.Drawable.button_disabled_bg);
                        btnResetProfile.Enabled = false;
                    }
                }
                else
                {
                    btnResetProfile.SetBackgroundResource(Resource.Drawable.button_disabled_bg);
                    btnResetProfile.Enabled = false;

                    btnEnrollmentToLogin.SetBackgroundResource(Resource.Drawable.button_disabled_bg);
                    btnResetProfile.Enabled = false;
                }
            }
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
            _haveRecording = false;

            bool validProfile = false;

            try
            {
                var profile = _api.GetVerificationProfile(_profileId);
                validProfile = _profileId == profile.verificationProfileId;

                txtRemainingEnrollments.Text = profile.remainingEnrollmentsCount.ToString();
                _haveRecording = profile.remainingEnrollmentsCount != 3;
            }
            catch
            {
                validProfile = false;
                _profileId = null;
            }

            if (validProfile && !string.IsNullOrEmpty(_profileId))
                return;

            _profileId = _api.CreateVerificationProfile();
            if (string.IsNullOrEmpty(_profileId))
                throw new ApplicationException("Error creating Verification Profile ID.");

            System.Diagnostics.Debug.WriteLine("Verification Profile ID: " + _profileId);
            UserSettingsHelper.SaveProfileId(_profileId);
        }

        private void ShowAvailableEnrollmentPhrases()
        {
            var phrases = _api.GetVerificationPhrases();
            txtVerificationPhrases.Text = string.Join("\n", phrases.ToArray());
        }

        private void EnrollRecording()
        {
            var audioBytes = SpeakerRecognitionApiWrapper.AudioFileToBytes(_recorder.WavFileName);
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

                System.Diagnostics.Debug.WriteLine("Message: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }
    }
}
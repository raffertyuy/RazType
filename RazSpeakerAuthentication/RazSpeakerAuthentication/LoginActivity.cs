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

namespace RazSpeakerAuthentication
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity : EngagementActivity
    {
        private TextView txtStatus;
        private TextView txtConfidenceLevel;
        private TextView txtLoginVerificationPhrase;
        private Button btnLoginStartRecording;
        private Button btnLoginStopRecording;
        private Button btnLoginToMain;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Login);
            SetControlHandlers();
        }

        private void SetControlHandlers()
        {
            txtStatus = FindViewById<TextView>(Resource.Id.txtStatus);
            txtConfidenceLevel = FindViewById<TextView>(Resource.Id.txtConfidenceLevel);
            txtLoginVerificationPhrase = FindViewById<TextView>(Resource.Id.txtLoginVerificationPhrase);
            btnLoginStartRecording = FindViewById<Button>(Resource.Id.btnLoginStartRecording);
            btnLoginStopRecording = FindViewById<Button>(Resource.Id.btnLoginStopRecording);
            btnLoginToMain = FindViewById<Button>(Resource.Id.btnLoginToMain);

            btnLoginStartRecording.Click += btnLoginStartRecording_Click;
            btnLoginStopRecording.Click += btnLoginStopRecording_Click;
            btnLoginToMain.Click += btnLoginToMain_Click;
        }

        private void btnLoginStartRecording_Click(object sender, EventArgs e)
        {
            // authenticate

            if (true)
            {
                var intent = new Intent(this, typeof(AccountActivity));
                StartActivity(intent);
            }
        }

        private void btnLoginStopRecording_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void btnLoginToMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
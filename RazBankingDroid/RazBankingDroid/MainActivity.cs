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

using Microsoft.Azure.Engagement.Xamarin;
using Microsoft.Azure.Engagement.Xamarin.Activity;

namespace RazBankingDroid
{
    [Activity(Label = "Mobile Banking POC", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : EngagementActivity
    {
        private Button btnLogin;
        private Button btnEnroll;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            // Uncomment the following lines to enable AzME, also update the AndroidManifest.xml
            //var engagementConfiguration = new EngagementConfiguration();
            //engagementConfiguration.ConnectionString = "YOUR CONNECTION STRING HERE";
            //EngagementAgent.Init(engagementConfiguration);

            SetControlHandlers();
        }

        private void SetControlHandlers()
        {
            btnLogin = FindViewById<Button> (Resource.Id.btnLogin);
            btnEnroll = FindViewById<Button>(Resource.Id.btnEnroll);

            btnLogin.Click += btnLogin_Click;
            btnEnroll.Click += btnEnroll_Click;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(LoginActivity));
            StartActivity(intent);
        }

        private void btnEnroll_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(EnrollmentActivity));
            StartActivity(intent);
        }
    }
}
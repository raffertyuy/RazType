using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Microsoft.Azure.Engagement;
using Microsoft.Azure.Engagement.Xamarin;
using Microsoft.Azure.Engagement.Xamarin.Activity;

namespace RazDroid
{
    [Activity(Label = "RazDroid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : EngagementActivity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var engagementConfiguration = new EngagementConfiguration();
            engagementConfiguration.ConnectionString = "Endpoint=raztype.device.mobileengagement.windows.net;SdkKey=6ba280d749c34058e9a77578b9c5c261;AppId=cur000033";
            EngagementAgent.Init(engagementConfiguration);

            // Get our button from the layout resource,
            // and attach an event to it
            var btnClickMe = FindViewById<Button>(Resource.Id.btnClickMe);
            btnClickMe.Click += btnClickMe_Click;

            var btnSecond = FindViewById<Button>(Resource.Id.btnMainToSecond);
            btnSecond.Click += btnSecond_Click;

            var btnCrash = FindViewById<Button>(Resource.Id.btnCrash);
            btnCrash.Click += btnCrash_Click;

            var btnAbout = FindViewById<Button>(Resource.Id.btnAbout);
            btnAbout.Click += btnAbout_Click;
        }

        private void btnClickMe_Click(object sender, EventArgs e)
        {
            var btnClickMe = FindViewById<Button>(Resource.Id.btnClickMe);
            btnClickMe.Text = string.Format("{0} clicks!", count++);
        }

        private void btnSecond_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(SecondActivity));
            StartActivity(intent);
        }

        private void btnCrash_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(CrashActivity));
            StartActivity(intent);
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(AboutActivity));
            StartActivity(intent);
        }
    }
}


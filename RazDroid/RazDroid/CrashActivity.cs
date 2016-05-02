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
using Microsoft.Azure.Engagement.Xamarin;

namespace RazDroid
{
    [Activity(Label = "CrashActivity")]
    public class CrashActivity : EngagementActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Crash);

            // Get our UI controls from the loaded layout
            var btnCrash = FindViewById<Button>(Resource.Id.btnCrash);
            btnCrash.Click += btnCrash_Click;

            var btnMain = FindViewById<Button>(Resource.Id.btnCrashToMain);
            btnMain.Click += btnMain_Click;
        }

        private void btnCrash_Click(object sender, EventArgs e)
        {
            throw new ApplicationException("The Crash test button was clicked.");
        }

        private void btnMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
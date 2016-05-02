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

namespace RazDroid
{
    [Activity(Label = "ThirdActivity")]
    public class ThirdActivity : EngagementActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Third);

            // Get our UI controls from the loaded layout
            var btnSecond = FindViewById<Button>(Resource.Id.btnThirdToSecond);
            btnSecond.Click += btnSecond_Click;

            var btnMain = FindViewById<Button>(Resource.Id.btnThirdToMain);
            btnMain.Click += btnMain_Click;
        }

        private void btnSecond_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(SecondActivity));
            StartActivity(intent);
        }

        private void btnMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
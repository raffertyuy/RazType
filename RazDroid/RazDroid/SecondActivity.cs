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
    [Activity(Label = "SecondActivity")]
    public class SecondActivity : EngagementActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Second);

            // Get our UI controls from the loaded layout
            var btnThird = FindViewById<Button>(Resource.Id.btnSecondToThird);
            var btnMain = FindViewById<Button>(Resource.Id.btnSecondToMain);

            btnThird.Click += btnThird_Click;
            btnMain.Click += btnMain_Click;
        }

        private void btnThird_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(ThirdActivity));
            StartActivity(intent);
        }

        private void btnMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
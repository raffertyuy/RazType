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
    [Activity(Label = "AboutActivity")]
    public class AboutActivity : EngagementActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.About);

            // Get our UI controls from the loaded layout
            var linkRazType = FindViewById<Button>(Resource.Id.linkRazType);
            linkRazType.Click += LinkRazType_Click;

            var linkBlog = FindViewById<Button>(Resource.Id.linkBlog);
            linkBlog.Click += LinkBlog_Click;

            var btnMain = FindViewById<Button>(Resource.Id.btnAboutToMain);
            btnMain.Click += btnMain_Click;
        }

        private void LinkRazType_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("http://www.raztype.com");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void LinkBlog_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("http://blog.raztype.com");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void btnMain_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}
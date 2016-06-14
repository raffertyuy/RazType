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

namespace RazSpeakerAuthentication.Helpers
{
    public class ProcessingResult
    {
        public string enrollmentStatus;
        public float enrollmentSpeechTime;
        public float remainingEnrollmentSpeechTime;
        public float speechTime;
        public string identifiedProfileId;
        public string confidence;
    }
}
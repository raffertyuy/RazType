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

namespace RazBankingDroid.Helpers
{
    public class VerificationEnrollmentResult
    {
        public string enrollmentStatus;
        public int enrollmentsCount;
        public int remainingEnrollments;
        public string phrase;
    }
}
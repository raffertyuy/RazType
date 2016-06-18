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
    public class VerificationProfile
    {
        public string verificationProfileId;
        public string locale;
        public int enrollmentsCount;
        public int remainingEnrollmentsCount;
        public DateTime createdDateTime;
        public DateTime lastActionDateTime;
        public string EnrollmentStatus;
    }
}
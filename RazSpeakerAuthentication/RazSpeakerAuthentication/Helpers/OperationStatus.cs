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
    public class OperationStatus
    {
        public string status;
        public DateTime createdDateTime;
        public DateTime lastActionDateTime;
        public string message;
        public ProcessingResult processingResult;
    }
}
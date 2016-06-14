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
    public static class UserSettingsHelper
    {
        public static void SaveProfileId(string profileId)
        {

            //store
            var prefs = Application.Context.GetSharedPreferences("RazSpeakerAuthentication", FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutString("ProfileId", profileId);
            prefEditor.Commit();

        }

        // Function called from OnCreate
        public static string RetrieveProfileId()
        {
            //retreive 
            var prefs = Application.Context.GetSharedPreferences("RazSpeakerAuthentication", FileCreationMode.Private);
            return prefs.GetString("ProfileId", null);
        }
    }
}
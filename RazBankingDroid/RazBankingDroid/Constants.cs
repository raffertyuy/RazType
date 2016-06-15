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
using Android.Media;

namespace RazBankingDroid
{
    public static class Constants
    {
        public const string SPEAKER_RECOGNITION_ACCOUNT_KEY = "cfdb5c6532ca469aa7a7f8d74ab93ca9";

        public const int RECORDER_SAMPLERATE = 16000;
        public const ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        public const Android.Media.Encoding RECORDER_AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;
        public const int BUFFER_ELEMENTS_2_REC = 1024; // want to play 2048 (2K) since 2 bytes we use only 1024
        public const int BYTES_PER_ELEMENT = 2; // 2 bytes in 16bit format

        public const string WAV_FILE_PATH = "/sdcard/voice8K16bitmono.wav";
    }
}
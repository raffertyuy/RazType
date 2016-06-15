using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System.Threading.Tasks;

namespace RazBankingDroid.Helpers
{
    class LowLevelRecorder : INotificationReceiver
    {
        public Action<bool> RecordingStateChanged;

        private const int RECORDER_SAMPLERATE = 16000;
        public const ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        public const Android.Media.Encoding RECORDER_AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;

        static string filePath = "/data/data/RazBankingDroid.RazBankingDroid/files/voice8K16bitmono.wav";
        byte[] audioBuffer = null;
        AudioRecord audioRecord = null;
        bool endRecording = false;

        public Boolean IsRecording { get; set; }

        public string WavFileName { get { return filePath; } }

        async Task ReadAudioAsync()
        {
            using (var fileStream = new FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                while (true)
                {
                    if (endRecording)
                    {
                        endRecording = false;
                        break;
                    }
                    try
                    {
                        // Keep reading the buffer while there is audio input.
                        int numBytes = await audioRecord.ReadAsync(audioBuffer, 0, audioBuffer.Length);
                        await fileStream.WriteAsync(audioBuffer, 0, numBytes);
                        // Do something with the audio input.
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        break;
                    }
                }
                fileStream.Close();
            }
            audioRecord.Stop();
            audioRecord.Release();
            IsRecording = false;

            RaiseRecordingStateChangedEvent();
        }

        private void RaiseRecordingStateChangedEvent()
        {
            if (RecordingStateChanged != null)
                RecordingStateChanged(IsRecording);
        }

        protected async Task StartRecorderAsync()
        {
            endRecording = false;
            IsRecording = true;

            RaiseRecordingStateChangedEvent();

            audioBuffer = new byte[AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING)]; // new byte[100000];
            audioRecord = new AudioRecord(
                // Hardware source of recording.
                AudioSource.Mic,
                // Frequency
                RECORDER_SAMPLERATE,
                // Mono or stereo
                RECORDER_CHANNELS,
                // Audio encoding
                RECORDER_AUDIO_ENCODING,
                // Length of the audio clip.
                audioBuffer.Length
            );

            audioRecord.StartRecording();

            // Off line this so that we do not block the UI thread.
            await ReadAudioAsync();
        }

        public async Task StartAsync()
        {
            await StartRecorderAsync();
        }

        public void Stop()
        {
            endRecording = true;
            Thread.Sleep(500); // Give it time to drop out.
        }
    }
}
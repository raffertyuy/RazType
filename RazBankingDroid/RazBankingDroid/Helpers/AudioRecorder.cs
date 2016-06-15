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
using System.Threading;
using System.IO;

namespace RazBankingDroid.Helpers
{
    public class AudioRecorder
    {
        public const int RECORDER_SAMPLERATE = 16000;
        public const ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        public const Android.Media.Encoding RECORDER_AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;
        public const int BUFFER_ELEMENTS_2_REC = 1024; // want to play 2048 (2K) since 2 bytes we use only 1024
        public const int BYTES_PER_ELEMENT = 2; // 2 bytes in 16bit format


        public const string WAV_FILE_PATH = "/sdcard/voice8K16bitmono.wav";
        private AudioRecord _recorder = null;
        private Thread _recordingThread = null;

        private int _minBufferSize;
        private int _bufferSize;

        private bool IsRecording { get; set; }

        public string WavFileName { get { return WAV_FILE_PATH; } }

        public AudioRecorder()
        {
            _minBufferSize = AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING);
            _bufferSize = BUFFER_ELEMENTS_2_REC * BYTES_PER_ELEMENT;
            System.Diagnostics.Debug.WriteLine("BufferSize = {0} vs Min Size = {1}", _bufferSize, _minBufferSize);
        }

        public void StartRecording()
        {
            _recorder = new AudioRecord(AudioSource.Mic, RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING, _bufferSize);
            _recorder.StartRecording();
            IsRecording = true;

            _recordingThread = new Thread(() => WriteAudioDataToFile());
            _recordingThread.Start();
        }

        private byte[] Short2Byte(short[] sData)
        {
            int shortArrsize = sData.Length;
            byte[] bytes = new byte[shortArrsize * 2];
            for (int i = 0; i < shortArrsize; i++)
            {
                bytes[i * 2] = (byte)(sData[i] & 0x00FF);
                bytes[(i * 2) + 1] = (byte)(sData[i] >> 8);
                sData[i] = 0;
            }
            return bytes;
        }

        private void WriteAudioDataToFile()
        {
            // Write the output audio in byte

            var sData = new short[BUFFER_ELEMENTS_2_REC];

            using (FileStream os = new FileStream(WAV_FILE_PATH, FileMode.Create))
            {
                while (IsRecording)
                {
                    // gets the voice output from microphone to byte format
                    _recorder.Read(sData, 0, BUFFER_ELEMENTS_2_REC);
                    System.Diagnostics.Debug.WriteLine("Short wirting to file" + sData.ToString());

                    try
                    {
                        // // writes the data to file from buffer
                        // // stores the voice buffer
                        var bData = Short2Byte(sData);
                        os.Write(bData, 0, _bufferSize);
                    }
                    catch (IOException e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception Message: {0}", e.Message);
                        System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", e.StackTrace);
                    }
                }
            }
        }

        public void StopRecording()
        {
            // stops the recording activity
            if (_recorder == null)
                return;

            IsRecording = false;
            _recorder.Stop();
            _recorder.Release();
            _recorder.Dispose();
            _recordingThread.Join();
        }
    }
}
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
        private AudioRecord _recorder = null;
        private Thread _recordingThread = null;

        private int _minBufferSize;
        private int _bufferSize;

        private bool IsRecording { get; set; }

        public AudioRecorder()
        {
            _minBufferSize = AudioRecord.GetMinBufferSize(Constants.RECORDER_SAMPLERATE, Constants.RECORDER_CHANNELS, Constants.RECORDER_AUDIO_ENCODING);
            _bufferSize = Constants.BUFFER_ELEMENTS_2_REC * Constants.BYTES_PER_ELEMENT;
            System.Diagnostics.Debug.WriteLine("BufferSize = {0} vs Min Size = {1}", _bufferSize, _minBufferSize);
        }

        private void StartRecording()
        {
            _recorder = new AudioRecord(AudioSource.Mic, Constants.RECORDER_SAMPLERATE, Constants.RECORDER_CHANNELS, Constants.RECORDER_AUDIO_ENCODING, _bufferSize);
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

            var sData = new short[Constants.BUFFER_ELEMENTS_2_REC];

            using (FileStream os = new FileStream(Constants.WAV_FILE_PATH, FileMode.Create))
            {
                while (IsRecording)
                {
                    // gets the voice output from microphone to byte format
                    _recorder.Read(sData, 0, Constants.BUFFER_ELEMENTS_2_REC);
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

        private void StopRecording()
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
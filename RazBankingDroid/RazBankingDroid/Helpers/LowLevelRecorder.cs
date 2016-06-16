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

        private const int RECORDER_BPP = 16;
        private const int RECORDER_SAMPLERATE = 16000;
        private const ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        private const Android.Media.Encoding RECORDER_AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;

        static string filePath;
        //static string filePathConverted;
        byte[] audioBuffer = null;
        AudioRecord audioRecord = null;
        bool endRecording = false;

        public Boolean IsRecording { get; set; }

        public string WavFileName { get { return filePath; } }

        public LowLevelRecorder()
        {
            filePath = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).Path, "RazBankingDroid.wav");
            //filePathConverted = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).Path, "RazBankingDroidConverted.wav");
        }

        async Task ReadAudioAsync()
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
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

            //CopyWaveFile(filePath, filePathConverted);

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

            var bufferSize = AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING);

            audioBuffer = new byte[bufferSize];
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

        #region CopyWavFile
        //http://www.edumobile.org/android/audio-recording-in-wav-format-in-android-programming/
        private void CopyWaveFile(string inFilename, string outFilename)
        {
            byte[] data = new byte[audioBuffer.Length];

            try
            {
                byte[] audioBytes = null;
                using (FileStream fsSource = new FileStream(inFilename, FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    audioBytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(audioBytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                }

                using (FileStream fsDest = new FileStream(outFilename, FileMode.Create))
                {
                    var totalAudioLength = audioBytes.Length;
                    var totalDataLength = totalAudioLength + 36;
                    long longSampleRate = RECORDER_SAMPLERATE;
                    int channels = 1;
                    long byteRate = RECORDER_BPP * RECORDER_SAMPLERATE * channels / 8;

                    WriteWaveFileHeader(fsDest, totalAudioLength, totalDataLength, longSampleRate, 1, byteRate);
                    fsDest.Write(audioBytes, 0, audioBytes.Length);
                }
            }
            catch (FileNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Exception Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", e.StackTrace);
            }
            catch (IOException e)
            {
                System.Diagnostics.Debug.WriteLine("Exception Message: {0}", e.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: {0}", e.StackTrace);
            }
        }

        private void WriteWaveFileHeader(FileStream fsDest,
            long totalAudioLen, long totalDataLen, long longSampleRate, int channels, long byteRate)
        {

            byte[] header = new byte[44];

            header[0] = Convert.ToByte('R'); // RIFF/WAVE header
            header[1] = Convert.ToByte('I');
            header[2] = Convert.ToByte('F');
            header[3] = Convert.ToByte('F');
            header[4] = (byte)(totalDataLen & 0xff);
            header[5] = (byte)((totalDataLen >> 8) & 0xff);
            header[6] = (byte)((totalDataLen >> 16) & 0xff);
            header[7] = (byte)((totalDataLen >> 24) & 0xff);
            header[8] = Convert.ToByte('W');
            header[9] = Convert.ToByte('A');
            header[10] = Convert.ToByte('V');
            header[11] = Convert.ToByte('E');
            header[12] = Convert.ToByte('f'); // 'fmt ' chunk
            header[13] = Convert.ToByte('m');
            header[14] = Convert.ToByte('t');
            header[15] = Convert.ToByte(' ');
            header[16] = 16; // 4 bytes: size of 'fmt ' chunk
            header[17] = 0;
            header[18] = 0;
            header[19] = 0;
            header[20] = 1; // format = 1
            header[21] = 0;
            header[22] = (byte)channels;
            header[23] = 0;
            header[24] = (byte)(longSampleRate & 0xff);
            header[25] = (byte)((longSampleRate >> 8) & 0xff);
            header[26] = (byte)((longSampleRate >> 16) & 0xff);
            header[27] = (byte)((longSampleRate >> 24) & 0xff);
            header[28] = (byte)(byteRate & 0xff);
            header[29] = (byte)((byteRate >> 8) & 0xff);
            header[30] = (byte)((byteRate >> 16) & 0xff);
            header[31] = (byte)((byteRate >> 24) & 0xff);
            header[32] = (byte)(2 * 16 / 8); // block align
            header[33] = 0;
            header[34] = RECORDER_BPP; // bits per sample
            header[35] = 0;
            header[36] = Convert.ToByte('d');
            header[37] = Convert.ToByte('a');
            header[38] = Convert.ToByte('t');
            header[39] = Convert.ToByte('a');
            header[40] = (byte)(totalAudioLen & 0xff);
            header[41] = (byte)((totalAudioLen >> 8) & 0xff);
            header[42] = (byte)((totalAudioLen >> 16) & 0xff);
            header[43] = (byte)((totalAudioLen >> 24) & 0xff);

            fsDest.Write(header, 0, 44);
        }
        #endregion
    }
}
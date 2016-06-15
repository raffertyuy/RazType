using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System.IO;

namespace RazBankingDroid.Helpers
{
    public class SoundRecorder
    {
        private const int RECORDER_BPP = 16;
        private const string AUDIO_RECORDER_FILE_EXT_WAV = ".wav";
        private const string AUDIO_RECORDER_FOLDER = "RazBankingDroid";
        private const string AUDIO_RECORDER_TEMP_FILE = "record_temp.raw";
        private const int RECORDER_SAMPLERATE = 16000;
        public const ChannelIn RECORDER_CHANNELS = ChannelIn.Mono;
        public const Android.Media.Encoding RECORDER_AUDIO_ENCODING = Android.Media.Encoding.Pcm16bit;
        public const string WAV_FILENAME = "/sdcard/RazBankingDroid/voice8K16bitmono.wav";
        public const string WAV_RAW_FILENAME = "/sdcard/RazBankingDroid/voice8K16bitmonoraw.wav";

        private AudioRecord _recorder = null;
        private int _bufferSize = 0;
        private Thread _recordingThread = null;

        public bool IsRecording { get; set; }

        public string WavFileName { get { return WAV_FILENAME; } }

        public SoundRecorder()
        {
            _bufferSize = AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING);
        }

        public void StartRecording()
        {
            _recorder = new AudioRecord(AudioSource.Mic, RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING, _bufferSize);

            if (_recorder.State == State.Initialized)
                _recorder.StartRecording();

            IsRecording = true;

            _recordingThread = new Thread(() => WriteAudioDataToFile());
            _recordingThread.Start();
        }

        private void WriteAudioDataToFile()
        {
            var data = new byte[_bufferSize];

            using (FileStream os = new FileStream(WAV_RAW_FILENAME, FileMode.Create))
            {
                while (IsRecording)
                {
                    // gets the voice output from microphone to byte format
                    _recorder.Read(data, 0, _bufferSize);

                    try
                    {
                        os.Write(data, 0, _bufferSize);
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
            if (_recorder == null)
                return;

            IsRecording = false;

            if (_recorder.State == State.Initialized)
                _recorder.Stop();

            _recorder.Release();

            _recorder.Dispose();
            _recordingThread.Join();

            CopyWaveFile(WAV_RAW_FILENAME, WAV_FILENAME);
            File.Delete(WAV_RAW_FILENAME);
        }

        private void CopyWaveFile(string inFilename, string outFilename)
        {
            byte[] data = new byte[_bufferSize];

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
    }
}
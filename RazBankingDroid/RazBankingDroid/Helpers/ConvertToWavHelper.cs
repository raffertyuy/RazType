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
using System.IO;

namespace RazBankingDroid.Helpers
{
    /// <summary>
    /// Based on http://stackoverflow.com/questions/25970430/recorded-audio-using-androidrecord-api-fails-to-play
    /// </summary>
    public class ConvertToWavHelper
    {
        public void ConvertWav(byte[] audioBytes, string wavFilePath)
        {
            try
            {
                long mySubChunk1Size = 16L;
                int myBitsPerSample = 16;
                int myFormat = 1;
                long myChannels = 1L;
                long mySampleRate = 16000L;
                long myByteRate = mySampleRate * myChannels * myBitsPerSample / 8L;
                int myBlockAlign = (int)(myChannels * myBitsPerSample / 8L);

                long myDataSize = audioBytes.Length;
                long myChunk2Size = myDataSize * myChannels * myBitsPerSample / 8L;
                long myChunkSize = 36L + myChunk2Size;

                using (var fs = new FileStream(wavFilePath, FileMode.Create))
                {
                    fs.WriteByte(Convert.ToByte('R'));
                    fs.WriteByte(Convert.ToByte('I'));
                    fs.WriteByte(Convert.ToByte('F'));
                    fs.WriteByte(Convert.ToByte('F'));
                    fs.Write(IntToByteArray((int)myChunkSize), 0, 4);
                    fs.WriteByte(Convert.ToByte('W'));
                    fs.WriteByte(Convert.ToByte('A'));
                    fs.WriteByte(Convert.ToByte('V'));
                    fs.WriteByte(Convert.ToByte('E'));
                    fs.WriteByte(Convert.ToByte('f'));
                    fs.WriteByte(Convert.ToByte('m'));
                    fs.WriteByte(Convert.ToByte('t'));
                    fs.WriteByte(Convert.ToByte(' '));
                    fs.Write(IntToByteArray((int)mySubChunk1Size), 0, 4);
                    fs.Write(ShortToByteArray((short)myFormat), 0, 2);
                    fs.Write(ShortToByteArray((short)(int)myChannels), 0, 2);
                    fs.Write(IntToByteArray((int)mySampleRate), 0, 4);
                    fs.Write(IntToByteArray((int)myByteRate), 0, 4);
                    fs.Write(ShortToByteArray((short)myBlockAlign), 0, 2);
                    fs.Write(ShortToByteArray((short)myBitsPerSample), 0, 2);
                    fs.WriteByte(Convert.ToByte('d'));
                    fs.WriteByte(Convert.ToByte('a'));
                    fs.WriteByte(Convert.ToByte('t'));
                    fs.WriteByte(Convert.ToByte('a'));
                    fs.Write(IntToByteArray((int)myDataSize), 0, 4);
                    fs.Write(audioBytes, 0, audioBytes.Length);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Message: " + e.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + e.StackTrace);
            }
        }

        private static byte[] IntToByteArray(int i)
        {
            byte[] b = new byte[4];
            b[0] = (byte)(i & 0xFF);
            b[1] = (byte)(i >> 8 & 0xFF);
            b[2] = (byte)(i >> 16 & 0xFF);
            b[3] = (byte)(i >> 24 & 0xFF);
            return b;
        }

        public static byte[] ShortToByteArray(short data)
        {
            int d = (int)((uint)data >> 8 & 0xFF);
            return new byte[] { (byte)(data & 0xFF), (byte)(d) };
        }
    }
}
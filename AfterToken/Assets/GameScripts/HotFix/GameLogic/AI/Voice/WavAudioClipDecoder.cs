using System;
using System.Text;
using UnityEngine;

namespace GameLogic.AI.Voice
{
    /// <summary>运行时 WAV 解码器：支持 PCM 8/16/24/32-bit 与 IEEE float32。</summary>
    public static class WavAudioClipDecoder
    {
        public static AudioClip Decode(byte[] bytes, string clipName)
        {
            if (bytes == null || bytes.Length < 44 || ReadFourCc(bytes, 0) != "RIFF" || ReadFourCc(bytes, 8) != "WAVE")
            {
                return null;
            }

            int format = 0;
            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int dataOffset = -1;
            int dataSize = 0;
            int offset = 12;

            while (offset + 8 <= bytes.Length)
            {
                string chunk = ReadFourCc(bytes, offset);
                int size = BitConverter.ToInt32(bytes, offset + 4);
                int payload = offset + 8;
                if (size < 0 || payload + size > bytes.Length)
                {
                    return null;
                }
                if (chunk == "fmt " && size >= 16)
                {
                    format = BitConverter.ToUInt16(bytes, payload);
                    channels = BitConverter.ToUInt16(bytes, payload + 2);
                    sampleRate = BitConverter.ToInt32(bytes, payload + 4);
                    bitsPerSample = BitConverter.ToUInt16(bytes, payload + 14);
                    // WAVE_FORMAT_EXTENSIBLE：SubFormat GUID 的前 2 字节仍是 PCM(1) 或 IEEE float(3)。
                    if (format == 0xfffe && size >= 40)
                    {
                        format = BitConverter.ToUInt16(bytes, payload + 24);
                    }
                }
                else if (chunk == "data")
                {
                    dataOffset = payload;
                    dataSize = size;
                }
                offset = payload + size + (size & 1);
            }

            int bytesPerSample = bitsPerSample / 8;
            if ((format != 1 && format != 3) || channels <= 0 || sampleRate <= 0
                || bytesPerSample <= 0 || dataOffset < 0 || dataSize < channels * bytesPerSample)
            {
                return null;
            }

            int sampleCount = dataSize / bytesPerSample;
            int frameCount = sampleCount / channels;
            var samples = new float[frameCount * channels];
            for (int i = 0; i < samples.Length; i++)
            {
                int p = dataOffset + i * bytesPerSample;
                if (format == 3 && bitsPerSample == 32)
                {
                    samples[i] = Mathf.Clamp(BitConverter.ToSingle(bytes, p), -1f, 1f);
                }
                else if (bitsPerSample == 8)
                {
                    samples[i] = (bytes[p] - 128) / 128f;
                }
                else if (bitsPerSample == 16)
                {
                    samples[i] = BitConverter.ToInt16(bytes, p) / 32768f;
                }
                else if (bitsPerSample == 24)
                {
                    int value = bytes[p] | (bytes[p + 1] << 8) | (bytes[p + 2] << 16);
                    if ((value & 0x800000) != 0) value |= unchecked((int)0xff000000);
                    samples[i] = value / 8388608f;
                }
                else if (bitsPerSample == 32)
                {
                    samples[i] = BitConverter.ToInt32(bytes, p) / 2147483648f;
                }
                else
                {
                    return null;
                }
            }

            var clip = AudioClip.Create(clipName, frameCount, channels, sampleRate, false);
            return clip.SetData(samples, 0) ? clip : null;
        }

        private static string ReadFourCc(byte[] bytes, int offset) => Encoding.ASCII.GetString(bytes, offset, 4);
    }
}

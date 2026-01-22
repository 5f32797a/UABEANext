using System;

namespace UABEANext4.Logic.Audio;

public class AudioData
{
    public byte[]? PcmData { get; set; }
    public int Channels { get; set; }
    public int SampleRate { get; set; }
    public int BitsPerSample { get; set; }
    public float DurationSeconds { get; set; }

    public AudioData(byte[]? pcmData, int channels, int sampleRate, int bitsPerSample, float durationSeconds)
    {
        PcmData = pcmData;
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        DurationSeconds = durationSeconds;
    }
}

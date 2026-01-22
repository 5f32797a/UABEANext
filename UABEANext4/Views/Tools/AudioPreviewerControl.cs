using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using UABEANext4.Logic.Audio;

namespace UABEANext4.Views.Tools;

public class AudioPreviewerControl : Control
{
    public static readonly StyledProperty<AudioData?> ActiveAudioProperty =
        AvaloniaProperty.Register<AudioPreviewerControl, AudioData?>(nameof(ActiveAudio));

    public AudioData? ActiveAudio
    {
        get => GetValue(ActiveAudioProperty);
        set => SetValue(ActiveAudioProperty, value);
    }

    public static readonly StyledProperty<double> PlaybackPositionProperty =
        AvaloniaProperty.Register<AudioPreviewerControl, double>(nameof(PlaybackPosition));

    public double PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    static AudioPreviewerControl()
    {
        AffectsRender<AudioPreviewerControl>(ActiveAudioProperty, PlaybackPositionProperty);
    }

    public override void Render(DrawingContext context)
    {
        var audio = ActiveAudio;
        if (audio == null || audio.PcmData == null || audio.PcmData.Length == 0)
        {
            context.DrawText(new FormattedText("No audio data", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.Gray), new Point(10, 10));
            return;
        }

        var bounds = Bounds;
        var width = bounds.Width;
        var height = bounds.Height;
        var midY = height / 2;

        // Simple waveform rendering
        // Assuming 16-bit PCM for now
        int bytesPerSample = audio.BitsPerSample / 8;
        int sampleCount = audio.PcmData.Length / bytesPerSample;
        int step = Math.Max(1, sampleCount / (int)width);

        var pen = new Pen(Brushes.SkyBlue, 1);
        
        for (int x = 0; x < (int)width; x++)
        {
            int sampleIdx = x * step;
            if (sampleIdx >= sampleCount) break;

            try
            {
                short sample = BitConverter.ToInt16(audio.PcmData, sampleIdx * bytesPerSample);
                float normalized = sample / 32768f;
                float y = (float)(midY - normalized * midY);

                context.DrawLine(pen, new Point(x, midY), new Point(x, y));
            }
            catch
            {
                break;
            }
        }

        // Draw playback marker
        if (audio.DurationSeconds > 0)
        {
            double markerX = (PlaybackPosition / audio.DurationSeconds) * width;
            context.DrawLine(new Pen(Brushes.Red, 2), new Point(markerX, 0), new Point(markerX, height));
        }
    }
}

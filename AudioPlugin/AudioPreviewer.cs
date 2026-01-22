using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Media.Imaging;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using System;
using UABEANext4.AssetWorkspace;
using UABEANext4.Logic.Audio;
using UABEANext4.Logic.Mesh;
using UABEANext4.Plugins;
using UABEANext4.Util;

namespace AudioPlugin;

public class AudioPreviewer : IUavPluginPreviewer
{
    public string Name => "AudioClip Previewer";
    public string Description => "Previews AudioClips";

    public UavPluginPreviewerType SupportsPreview(Workspace workspace, AssetInst selection)
    {
        if (selection.TypeId == (int)AssetClassID.AudioClip)
        {
            return UavPluginPreviewerType.Audio;
        }
        return UavPluginPreviewerType.None;
    }

    public string? ExecuteText(Workspace workspace, IUavPluginFunctions funcs, AssetInst selection, out string? error)
    {
        error = "Not a text previewer";
        return null;
    }

    public Bitmap? ExecuteImage(Workspace workspace, IUavPluginFunctions funcs, AssetInst selection, out string? error)
    {
        error = "Not an image previewer";
        return null;
    }

    public MeshObj? ExecuteMesh(Workspace workspace, IUavPluginFunctions funcs, AssetInst selection, out string? error)
    {
        error = "Not a mesh previewer";
        return null;
    }

    public AudioData? ExecuteAudio(Workspace workspace, IUavPluginFunctions funcs, AssetInst selection, out string? error)
    {
        error = null;
        var baseField = workspace.GetBaseField(selection);
        if (baseField == null)
        {
            error = "Failed to read AudioClip";
            return null;
        }

        string resourceSource = baseField["m_Resource.m_Source"].AsString;
        ulong resourceOffset = baseField["m_Resource.m_Offset"].AsULong;
        ulong resourceSize = baseField["m_Resource.m_Size"].AsULong;

        if (!GetAudioBytes(selection, resourceSource, resourceOffset, resourceSize, out byte[] resourceData))
        {
            error = "Failed to read resource data";
            return null;
        }

        if (!FsbLoader.TryLoadFsbFromByteArray(resourceData, out FmodSoundBank? bank) || bank == null)
        {
            error = "Failed to load FSB bank";
            return null;
        }

        List<FmodSample> samples = bank.Samples;
        if (samples.Count == 0)
        {
            error = "No samples found in FSB";
            return null;
        }

        var sample = samples[0];
        // We need PCM data for waveform. RebuildAsStandardFileFormat might give us OGG/MP3 if that's what's inside.
        // Fmod5Sharp can usually give us PCM if we ask or if we decode it.
        // For simplicity, let's just get the raw sample data if it's PCM, or try to decode.
        
        sample.RebuildAsStandardFileFormat(out byte[]? sampleData, out string? sampleExtension);
        if (sampleData == null)
        {
            error = "Failed to rebuild sample";
            return null;
        }

        // If it's WAV, it's PCM. If it's OGG/MP3, we'd need to decode it.
        // For now, let's assume if it's WAV we can use it.
        
        float duration = sample.Metadata.SampleCount / (float)sample.Metadata.Frequency;

        // Note: sampleData for WAV includes the header. AudioPreviewerControl expects raw PCM.
        // We'll just strip the first 44 bytes if it's a WAV.
        if (sampleExtension?.ToLowerInvariant() == "wav")
        {
            if (sampleData.Length < 44)
            {
                error = "WAV data too small";
                return null;
            }

            byte[] pcm = new byte[sampleData.Length - 44];
            Array.Copy(sampleData, 44, pcm, 0, pcm.Length);
            return new AudioData(pcm, (int)sample.Metadata.Channels, (int)sample.Metadata.Frequency, 16, duration);
        }

        error = $"Preview for {sampleExtension} not yet supported in waveform.";
        return null;
    }

    private bool GetAudioBytes(AssetInst asset, string filepath, ulong offset, ulong size, out byte[] audioData)
    {
        return AudioUtils.GetAudioBytes(asset, filepath, offset, size, out audioData);
    }

    public void Cleanup() { }
}

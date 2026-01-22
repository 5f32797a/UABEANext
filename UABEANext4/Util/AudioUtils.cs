using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.IO;
using UABEANext4.AssetWorkspace;

namespace UABEANext4.Util;

public static class AudioUtils
{
    public static bool GetAudioBytes(AssetInst asset, string filepath, ulong offset, ulong size, out byte[] audioData)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            audioData = System.Array.Empty<byte>();
            return false;
        }

        if (asset.FileInstance.parentBundle != null)
        {
            string searchPath = filepath;
            if (searchPath.StartsWith("archive:/"))
                searchPath = searchPath.Substring(9);

            searchPath = Path.GetFileName(searchPath);

            AssetBundleFile bundle = asset.FileInstance.parentBundle.file;

            AssetsFileReader reader = bundle.DataReader;
            List<AssetBundleDirectoryInfo> dirInf = bundle.BlockAndDirInfo.DirectoryInfos;
            for (int i = 0; i < dirInf.Count; i++)
            {
                AssetBundleDirectoryInfo info = dirInf[i];
                if (info.Name == searchPath)
                {
                    lock (bundle.DataReader)
                    {
                        reader.Position = info.Offset + (long)offset;
                        audioData = reader.ReadBytes((int)size);
                    }
                    return true;
                }
            }
        }

        string assetsFileDirectory = Path.GetDirectoryName(asset.FileInstance.path)!;
        if (asset.FileInstance.parentBundle != null)
        {
            assetsFileDirectory = Path.GetDirectoryName(assetsFileDirectory)!;
        }

        string resourceFilePath = Path.Combine(assetsFileDirectory, filepath);

        if (File.Exists(resourceFilePath))
        {
            AssetsFileReader reader = new AssetsFileReader(resourceFilePath);
            reader.Position = (long)offset;
            audioData = reader.ReadBytes((int)size);
            return true;
        }

        string resourceFileName = Path.Combine(assetsFileDirectory, Path.GetFileName(filepath));

        if (File.Exists(resourceFileName))
        {
            AssetsFileReader reader = new AssetsFileReader(resourceFileName);
            reader.Position = (long)offset;
            audioData = reader.ReadBytes((int)size);
            return true;
        }

        audioData = System.Array.Empty<byte>();
        return false;
    }
}

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.Linq;
using UABEANext4.AssetWorkspace;

namespace UABEANext4.Util;

public class FontValidator
{
    public class GlyphInfo
    {
        public uint Unicode { get; set; }
        public string Character { get; set; }
        public int GlyphIndex { get; set; }
    }

    public class FontValidationResult
    {
        public List<GlyphInfo> FoundGlyphs { get; } = new();
        public List<uint> MissingUnicodes { get; } = new();
        public List<string> ValidationLog { get; } = new();
        public AssetInst? MaterialAsset { get; set; }
        public AssetInst? TextureAsset { get; set; }
    }

    public static FontValidationResult ValidateString(Workspace workspace, AssetInst fontAsset, string text)
    {
        var result = new FontValidationResult();
        var baseField = workspace.GetBaseField(fontAsset);
        if (baseField == null)
        {
            result.ValidationLog.Add("Error: Could not read font asset base field.");
            return result;
        }

        // 1. Check Asset Chain
        ValidateAssetChain(workspace, fontAsset, baseField, result);

        // 2. Build Glyph Map (including fallbacks)
        var glyphMap = new Dictionary<uint, GlyphInfo>();
        BuildGlyphMap(workspace, fontAsset, glyphMap, result.ValidationLog);

        // 3. Check requested text
        foreach (char c in text)
        {
            uint unicode = (uint)c;
            if (glyphMap.ContainsKey(unicode))
            {
                result.FoundGlyphs.Add(glyphMap[unicode]);
            }
            else if (!result.MissingUnicodes.Contains(unicode))
            {
                result.MissingUnicodes.Add(unicode);
            }
        }

        return result;
    }

    private static void ValidateAssetChain(Workspace workspace, AssetInst fontAsset, AssetTypeValueField baseField, FontValidationResult result)
    {
        var materialRef = baseField["m_Material"];
        if (materialRef.IsDummy) materialRef = baseField["material"]; // Older TMP or different version

        if (materialRef.IsDummy)
        {
            result.ValidationLog.Add("Warning: m_Material reference not found in FontAsset.");
        }
        else
        {
            var matInst = workspace.GetAssetInst(fontAsset.FileInstance, materialRef);
            if (matInst == null)
            {
                result.ValidationLog.Add("Error: Material reference is broken.");
            }
            else
            {
                result.MaterialAsset = matInst;
                result.ValidationLog.Add($"Info: Linked to Material: {matInst.AssetName} [PathID: {matInst.PathId}]");
                
                var matBase = workspace.GetBaseField(matInst);
                if (matBase != null)
                {
                    var texEnvs = matBase["m_SavedProperties.m_TexEnvs.Array"];
                    foreach (var texEnv in texEnvs)
                    {
                        if (texEnv["m_Texture"].IsDummy) continue;
                        var texInst = workspace.GetAssetInst(fontAsset.FileInstance, texEnv["m_Texture"]);
                        if (texInst != null)
                        {
                            result.TextureAsset = texInst;
                            result.ValidationLog.Add($"Info: Linked to Texture: {texInst.AssetName} [PathID: {texInst.PathId}]");
                            break;
                        }
                    }
                }
            }
        }
    }

    private static void BuildGlyphMap(Workspace workspace, AssetInst fontAsset, Dictionary<uint, GlyphInfo> glyphMap, List<string> log, HashSet<long>? visited = null)
    {
        visited ??= new HashSet<long>();
        if (visited.Contains(fontAsset.PathId)) return;
        visited.Add(fontAsset.PathId);

        var baseField = workspace.GetBaseField(fontAsset);
        if (baseField == null) return;

        // Parse m_CharacterTable
        var charTable = baseField["m_CharacterTable.Array"];
        if (!charTable.IsDummy)
        {
            foreach (var charEntry in charTable)
            {
                uint unicode = charEntry["m_Unicode"].AsUInt;
                int glyphIndex = charEntry["m_GlyphIndex"].AsInt;
                if (!glyphMap.ContainsKey(unicode))
                {
                    glyphMap[unicode] = new GlyphInfo { Unicode = unicode, Character = ((char)unicode).ToString(), GlyphIndex = glyphIndex };
                }
            }
        }

        // Parse Fallbacks
        var fallbacks = baseField["m_FallbackFontAssets.Array"];
        if (!fallbacks.IsDummy)
        {
            foreach (var fallbackRef in fallbacks)
            {
                var fallbackInst = workspace.GetAssetInst(fontAsset.FileInstance, fallbackRef);
                if (fallbackInst != null)
                {
                    BuildGlyphMap(workspace, fallbackInst, glyphMap, log, visited);
                }
            }
        }
    }
}

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using UABEANext4.AssetWorkspace;

namespace UABEANext4.Util;

public static class DependencyUtil
{
    public static IEnumerable<AssetPPtr> GetDependencies(AssetTypeValueField baseField)
    {
        var dependencies = new List<AssetPPtr>();
        GetDependenciesRecursive(baseField, dependencies);
        return dependencies;
    }

    private static void GetDependenciesRecursive(AssetTypeValueField field, List<AssetPPtr> dependencies)
    {
        if (field.Children == null) return;

        if (field.TemplateField.Type == "PPtr<T>" || field.Children.Count == 2)
        {
            var fileIdField = field["m_FileID"];
            var pathIdField = field["m_PathID"];

            if (!fileIdField.IsDummy && !pathIdField.IsDummy)
            {
                var fileId = fileIdField.AsInt;
                var pathId = pathIdField.AsLong;

                if (pathId != 0)
                {
                    dependencies.Add(new AssetPPtr(fileId, pathId));
                }
                return;
            }
        }

        foreach (var child in field.Children)
        {
            GetDependenciesRecursive(child, dependencies);
        }
    }

    public static IEnumerable<AssetInst> ResolveDependencies(Workspace workspace, AssetsFileInstance fileInst, IEnumerable<AssetPPtr> pptrs)
    {
        var resolved = new HashSet<AssetInst>();
        foreach (var pptr in pptrs)
        {
            var asset = workspace.GetAssetInst(fileInst, pptr.FileId, pptr.PathId);
            if (asset != null)
            {
                resolved.Add(asset);
            }
        }
        return resolved;
    }
}

using AssetsTools.NET.Extra;
using Avalonia.Platform.Storage;
using System.Text;
using UABEANext4.AssetWorkspace;
using UABEANext4.Plugins;
using UABEANext4.ViewModels.Dialogs;

namespace FontPlugin;
public class ImportFontOption : IUavPluginOption
{
    public string Name => "Import Font";
    public string Description => "Imports Fonts from ttf/otf";
    public UavPluginMode Options => UavPluginMode.Import;

    public bool SupportsSelection(Workspace workspace, UavPluginMode mode, IList<AssetInst> selection)
    {
        if (mode != UavPluginMode.Import)
        {
            return false;
        }

        var typeId = (int)AssetClassID.Font;
        return selection.All(a => a.TypeId == typeId);
    }

    public async Task<bool> Execute(Workspace workspace, IUavPluginFunctions funcs, UavPluginMode mode, IList<AssetInst> selection)
    {
        if (selection.Count > 1)
        {
            return await BatchImport(workspace, funcs, selection);
        }
        else
        {
            return await SingleImport(workspace, funcs, selection);
        }
    }

    public async Task<bool> BatchImport(Workspace workspace, IUavPluginFunctions funcs, IList<AssetInst> selection)
    {
        var dir = await funcs.ShowOpenFolderDialog(new FolderPickerOpenOptions()
        {
            Title = "Select import directory"
        });

        if (dir == null)
        {
            return false;
        }

        var extensions = new List<string>() { "ttf", "otf" };
        var batchInfosViewModel = new BatchImportViewModel(workspace, selection.ToList(), dir, extensions);
        if (batchInfosViewModel.DataGridItems.Count == 0)
        {
            await funcs.ShowMessageDialog("Error", "No matching files found in the directory. Make sure the file names are in UABEA's format.");
            return false;
        }

        var batchInfosResult = await funcs.ShowDialog(batchInfosViewModel);
        if (batchInfosResult == null)
        {
            return false;
        }

        var errorBuilder = new StringBuilder();
        foreach (ImportBatchInfo info in batchInfosResult)
        {
            var asset = info.Asset;
            var errorAssetName = $"{Path.GetFileName(asset.FileInstance.path)}/{asset.PathId}";

            try
            {
                var baseField = FontHelper.GetByteArrayFont(workspace, asset);
                if (baseField == null)
                {
                    errorBuilder.AppendLine($"[{errorAssetName}]: failed to read");
                    continue;
                }

                var filePath = info.ImportFile;
                if (filePath == null || !File.Exists(filePath))
                {
                    errorBuilder.AppendLine($"[{errorAssetName}]: failed to import because {info.ImportFile ?? "[null]"} does not exist.");
                    continue;
                }

                byte[] byteData = File.ReadAllBytes(filePath);
                baseField["m_FontData.Array"].AsByteArray = byteData;
                asset.UpdateAssetDataAndRow(workspace, baseField);
            }
            catch (Exception ex)
            {
                errorBuilder.AppendLine($"[{errorAssetName}]: {ex.Message}");
            }
        }

        if (errorBuilder.Length > 0)
        {
            string[] firstLines = errorBuilder.ToString().Split('\n').Take(20).ToArray();
            string firstLinesStr = string.Join('\n', firstLines);
            await funcs.ShowMessageDialog("Error", firstLinesStr);
        }

        return true;
    }

    public async Task<bool> SingleImport(Workspace workspace, IUavPluginFunctions funcs, IList<AssetInst> selection)
    {
        var filePaths = await funcs.ShowOpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Load font file",
            FileTypeFilter = new List<FilePickerFileType>()
            {
                new("TTF file (*.ttf)") { Patterns = ["*.ttf"] },
                new("OTF file (*.otf)") { Patterns = ["*.otf"] },
                new("All types (*.*)")  { Patterns = ["*"] },
            },
            AllowMultiple = false
        });

        if (filePaths == null || filePaths.Length == 0)
        {
            return false;
        }

        var filePath = filePaths[0];
        if (!File.Exists(filePath))
        {
            await funcs.ShowMessageDialog("Error", $"Failed to import because {filePath ?? "[null]"} does not exist.");
            return false;
        }

        try
        {
            var asset = selection[0];
            var baseField = FontHelper.GetByteArrayFont(workspace, asset);
            if (baseField == null)
            {
                await funcs.ShowMessageDialog("Error", "Failed to read Font");
                return false;
            }

            byte[] byteData = File.ReadAllBytes(filePath);
            baseField["m_FontData.Array"].AsByteArray = byteData;
            asset.UpdateAssetDataAndRow(workspace, baseField);

            return true;
        }
        catch (Exception ex)
        {
            await funcs.ShowMessageDialog("Error", $"An error occurred during import: {ex.Message}");
            return false;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UABEANext4.AssetWorkspace;
using UABEANext4.Interfaces;
using UABEANext4.Logic.Configuration;
using UABEANext4.Util;
using Avalonia.Collections;
using System.Collections.ObjectModel;

namespace UABEANext4.ViewModels.Dialogs;
public partial class BatchImportViewModel : ViewModelBase, IDialogAware<List<ImportBatchInfo>?>
{
    private string _directory;

    private bool _ignoreListEvents;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                DataGridItems.Refresh();
            }
        }
    }

    public DataGridCollectionView DataGridItems { get; set; }
    private List<ImportBatchDataGridItem> _allDataGridItems;

    [ObservableProperty]
    public List<string> _matchingFilesItems;
    [ObservableProperty]
    public object? _dataGridSelectedItem;
    [ObservableProperty]
    public int _matchingFilesSelectedIndex;

    public string Title => "Batch Import";
    public int Width => 700;
    public int Height => 350;
    public event Action<List<ImportBatchInfo>?>? RequestClose;

    [Obsolete("This constructor is for the designer only and should not be used directly.", true)]
    public BatchImportViewModel()
    {
        _directory = string.Empty;

        _allDataGridItems = new List<ImportBatchDataGridItem>();
        DataGridItems = new DataGridCollectionView(_allDataGridItems);
        MatchingFilesItems = new List<string>();
    }

    public BatchImportViewModel(Workspace workspace, List<AssetInst> selection, string directory,
        List<string> extensions)
    {
        _directory = directory;

        var anyExtension = extensions.Contains("*");

        List<string> filesInDir;
        if (!anyExtension)
            filesInDir = FileUtils.GetFilesInDirectory(directory, extensions);
        else
            filesInDir = Directory.GetFiles(directory).ToList();

        List<ImportBatchDataGridItem> gridItems = new();
        int maxNameLen = ConfigurationManager.Settings.ExportNameLength;
        
        foreach (var asset in selection)
        {
            var assetName = workspace.Namer.GetAssetName(asset, true, maxNameLen);
            assetName = AssetNamer.GetFallbackName(asset, assetName);

            var gridItem = new ImportBatchDataGridItem(
                new ImportBatchInfo(
                    asset, Path.GetFileName(asset.FileInstance.path), assetName, asset.PathId)
            );

            List<string> matchingFiles;
            if (!anyExtension)
            {
                matchingFiles = filesInDir
                    .Where(f => 
                        extensions.Any(x => f.EndsWith(gridItem.GetMatchName(x))) || 
                        extensions.Any(x => Path.GetFileName(f).Equals($"{gridItem.Description}.{x}", StringComparison.OrdinalIgnoreCase))
                    )
                    .Select(f => Path.GetFileName(f)!).ToList();
            }
            else
            {
                matchingFiles = filesInDir
                    .Where(f => 
                        PathUtils.GetFilePathWithoutExtension(f).EndsWith(gridItem.GetMatchName("*")) ||
                        Path.GetFileNameWithoutExtension(f).Equals(gridItem.Description, StringComparison.OrdinalIgnoreCase)
                    )
                    .Select(f => Path.GetFileName(f)!).ToList();
            }

            gridItem.MatchingFiles = matchingFiles;
            gridItem.SelectedIndex = matchingFiles.Count > 0 ? 0 : -1;
            if (gridItem.MatchingFiles.Count > 0)
                gridItems.Add(gridItem);
        }

        _allDataGridItems = gridItems;
        DataGridItems = new DataGridCollectionView(_allDataGridItems);
        DataGridItems.Filter = FilterDataGrid;
        MatchingFilesItems = new List<string>();
    }

    private bool FilterDataGrid(object item)
    {
        if (item is not ImportBatchDataGridItem gridItem)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return gridItem.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               gridItem.File.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               gridItem.PathId.ToString().Contains(SearchText);
    }

    public void SelectAll()
    {
        foreach (var item in _allDataGridItems)
        {
            if (item.MatchingFiles.Count > 0)
                item.SelectedIndex = 0;
        }
    }

    public void SelectNone()
    {
        foreach (var item in _allDataGridItems)
        {
            item.SelectedIndex = -1;
        }
    }

    partial void OnDataGridSelectedItemChanged(object? value)
    {
        if (value is ImportBatchDataGridItem gridItem)
        {
            MatchingFilesItems = gridItem.MatchingFiles;
            if (gridItem.SelectedIndex != -1)
            {
                //there's gotta be a better way to do this .-. oh well
                _ignoreListEvents = true;
                MatchingFilesSelectedIndex = gridItem.SelectedIndex;
                _ignoreListEvents = false;
            }
        }
    }

    partial void OnMatchingFilesSelectedIndexChanged(int value)
    {
        if (DataGridSelectedItem is ImportBatchDataGridItem gridItem && !_ignoreListEvents)
        {
            gridItem.SelectedIndex = value;
        }
    }

    public void BtnOk_Click()
    {
        List<ImportBatchInfo> importInfos = new List<ImportBatchInfo>();
        foreach (ImportBatchDataGridItem gridItem in _allDataGridItems)
        {
            if (gridItem.SelectedIndex != -1)
            {
                ImportBatchInfo importInfo = gridItem.importInfo;
                importInfo.ImportFile = Path.Combine(_directory, gridItem.MatchingFiles[gridItem.SelectedIndex]);
                importInfos.Add(importInfo);
            }
        }

        RequestClose?.Invoke(importInfos);
    }

    public void BtnCancel_Click()
    {
        RequestClose?.Invoke(null);
    }
}

public class ImportBatchInfo
{
    public readonly AssetInst Asset;
    public readonly string AssetFile;
    public readonly string AssetName;
    public readonly long PathId;
    public string? ImportFile;

    public ImportBatchInfo(AssetInst asset, string assetFile, string assetName, long pathId)
    {
        Asset = asset;
        AssetFile = assetFile;
        AssetName = assetName;
        PathId = pathId;
    }
}

public class ImportBatchDataGridItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ImportBatchInfo importInfo;

    public List<string> MatchingFiles = new();
    public int SelectedIndex;

    public string Description => importInfo.AssetName;
    public string File => importInfo.AssetFile;
    public long PathId => importInfo.PathId;

    public ImportBatchDataGridItem(ImportBatchInfo importInfo)
    {
        this.importInfo = importInfo;
    }

    public string GetMatchName(string ext)
    {
        if (ext != "*")
            return $"-{File}-{PathId}.{ext}";

        return $"-{File}-{PathId}";
    }

    public void Update(string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
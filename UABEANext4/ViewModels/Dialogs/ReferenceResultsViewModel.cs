using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UABEANext4.AssetWorkspace;
using UABEANext4.Interfaces;
using UABEANext4.Logic;

namespace UABEANext4.ViewModels.Dialogs;

public partial class ReferenceResultsViewModel : ViewModelBase, IDialogAware<object?>
{
    public string Title => "Reference Results";
    public int Width => 800;
    public int Height => 500;
    public event Action<object?>? RequestClose;


    public Workspace Workspace { get; }
    public AssetInst TargetAsset { get; }

    [ObservableProperty]
    private ObservableCollection<AssetInst> _results;

    public ReferenceResultsViewModel(Workspace workspace, AssetInst target, List<AssetInst> results)
    {
        Workspace = workspace;
        TargetAsset = target;
        Results = new ObservableCollection<AssetInst>(results);
    }

    public void VisitAsset(AssetInst asset)
    {
        WeakReferenceMessenger.Default.Send(new RequestVisitAssetMessage(asset));
    }
}

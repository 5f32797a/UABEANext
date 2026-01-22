using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using UABEANext4.AssetWorkspace;
using UABEANext4.Interfaces;
using UABEANext4.Util;

namespace UABEANext4.ViewModels.Dialogs;

public partial class LocalizationValidatorViewModel : ViewModelBase, IDialogAware<bool>
{
    public string Title => "Localization & Font Validator";
    public int Width => 500;
    public int Height => 600;
    public event Action<bool>? RequestClose;

    private readonly Workspace _workspace;
    private readonly AssetInst _fontAsset;

    [ObservableProperty]
    private string _testText = string.Empty;

    public ObservableCollection<string> Logs { get; } = new();
    public ObservableCollection<uint> MissingCharacters { get; } = new();

    [ObservableProperty]
    private string _statusText = "Ready to validate.";

    public LocalizationValidatorViewModel(Workspace workspace, AssetInst fontAsset)
    {
        _workspace = workspace;
        _fontAsset = fontAsset;
    }

    [RelayCommand]
    public void Validate()
    {
        Logs.Clear();
        MissingCharacters.Clear();

        if (string.IsNullOrEmpty(TestText))
        {
            StatusText = "Please enter some text to validate.";
            return;
        }

        StatusText = "Validating...";
        var result = FontValidator.ValidateString(_workspace, _fontAsset, TestText);

        foreach (var log in result.ValidationLog)
        {
            Logs.Add(log);
        }

        foreach (var unicode in result.MissingUnicodes)
        {
            MissingCharacters.Add(unicode);
        }

        if (result.MissingUnicodes.Count == 0)
        {
            StatusText = "Success: All characters are supported!";
        }
        else
        {
            StatusText = $"Found {result.MissingUnicodes.Count} missing characters.";
        }
    }

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(false);
    }
}

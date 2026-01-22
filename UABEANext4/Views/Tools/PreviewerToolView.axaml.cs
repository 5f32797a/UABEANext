using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using UABEANext4.ViewModels.Tools;

namespace UABEANext4.Views.Tools;
public partial class PreviewerToolView : UserControl
{
    public PreviewerToolView()
    {
        InitializeComponent();
    }
}

public class PreviewerTemplateSelector : IDataTemplate
{
    [Content]
    public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    public Control Build(object? param)
    {
        var key = param?.ToString() ?? throw new ArgumentNullException(nameof(param));
        return AvailableTemplates[key].Build(param)!;
    }

    public bool Match(object? data)
    {
        var key = data?.ToString();

        return data is PreviewerToolPreviewType
                && !string.IsNullOrEmpty(key)
                && AvailableTemplates.ContainsKey(key);
    }
}

public class BooleanToCheckerboardConverter : IValueConverter
{
    public IBrush? CheckerboardBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return CheckerboardBrush;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
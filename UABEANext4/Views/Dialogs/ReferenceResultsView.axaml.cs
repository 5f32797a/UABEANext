using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UABEANext4.Views.Dialogs;

public partial class ReferenceResultsView : Window
{
    public ReferenceResultsView()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

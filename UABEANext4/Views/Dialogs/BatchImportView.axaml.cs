using Avalonia.Controls;

namespace UABEANext4.Views.Dialogs;
public partial class BatchImportView : UserControl
{
    public BatchImportView()
    {
        InitializeComponent();
        AttachedToVisualTree += (s, e) => this.FindControl<TextBox>("searchTextBox")?.Focus();
    }
}

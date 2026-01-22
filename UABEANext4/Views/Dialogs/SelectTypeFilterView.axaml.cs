using Avalonia.Controls;

namespace UABEANext4.Views.Dialogs;
public partial class SelectTypeFilterView : UserControl
{
    public SelectTypeFilterView()
    {
        InitializeComponent();
        AttachedToVisualTree += (s, e) => this.FindControl<TextBox>("searchTextBox")?.Focus();
    }
}

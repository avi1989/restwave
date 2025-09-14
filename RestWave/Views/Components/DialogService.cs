using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RestWave.Views.Components;

public class DialogService
{
    public async Task<bool> ShowConfirmationDialogAsync(Control parent, string message, string title = "Confirm")
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return false;

        var yesButton = new Button { Content = "Yes", Margin = new Thickness(0, 0, 10, 0) };
        var noButton = new Button { Content = "No" };
        
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new TextBlock
                    {
                        Text = message, 
                        Margin = new Thickness(0, 0, 0, 20), 
                        TextWrapping = TextWrapping.Wrap,
                        [DockPanel.DockProperty] = Dock.Top
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                        [DockPanel.DockProperty] = Dock.Bottom,
                        Children = { yesButton, noButton }
                    }
                }
            }
        };

        bool result = false;
        yesButton.Click += (s, e) =>
        {
            result = true;
            dialog.Close();
        };
        noButton.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(window);
        return result;
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RealRestClient.Views
{
    public partial class ConfigWindow : Window
    {
        public string RepoPath { get; private set; } = string.Empty;

        public ConfigWindow()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var textBox = this.FindControl<TextBox>("RepoPathTextBox");
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                RepoPath = textBox.Text.Trim();
                this.Close(RepoPath);
            }
        }
    }
}


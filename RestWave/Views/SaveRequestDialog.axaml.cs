using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RestWave.Views
{
    public partial class SaveRequestDialog : Window
    {
        public string RequestName { get; private set; } = string.Empty;

        public SaveRequestDialog()
        {
            InitializeComponent();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            var textBox = this.FindControl<TextBox>("RequestNameTextBox");
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                RequestName = textBox.Text.Trim();
                this.Close(RequestName);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            this.Close(null);
        }
    }
}

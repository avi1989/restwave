using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RestWave.Services;
using RestWave.ViewModels;

namespace RestWave.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigManager _configManager;
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _configManager = new ConfigManager();
            _configManager.LoadConfiguration();

            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            Loaded += (_, _) => LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var config = _configManager.Current;

            var themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
            if (themeComboBox != null)
            {
                themeComboBox.SelectedIndex = config.Theme switch
                {
                    "Light" => 1,
                    "Dark" => 2,
                    _ => 0 // System
                };
            }
        }

        private async void OnBrowseClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select REST Files Directory",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                this._viewModel.RequestFilePath = folders[0].Path.LocalPath;
            }
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            this._viewModel.SaveConfiguration();

            Application.Current!.RequestedThemeVariant = this._viewModel.Theme switch
            {
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default // System
            };

            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using RestWave.Services;

namespace RestWave.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private ConfigManager _configManager = new();

    [ObservableProperty] private string _requestFilePath;

    [ObservableProperty] private string _theme;

    public List<string> AvailableThemes { get; } = new List<string> { "System", "Light", "Dark" };


    public SettingsViewModel()
    {
        this.RequestFilePath = this._configManager.Current.RequestsDirectoryPath!;
        this.Theme = this._configManager.Current.Theme;
    }

    public void SaveConfiguration()
    {
        this._configManager.Current.Theme = this.Theme;
        this._configManager.Current.RequestsDirectoryPath = this.RequestFilePath;
        this._configManager.Write();
    }
}
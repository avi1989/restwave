using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using RestWave.ViewModels.Responses;
using RestWave.Models;
using RestWave.Services;
using RestWave.ViewModels;

namespace RestWave.Views;

public partial class MainWindow : Window
{
    private AppViewModel ViewModel => (AppViewModel)DataContext!;

    private Config config;
    private readonly ConfigManager configManager;

    public MainWindow()
    {
        InitializeComponent();
        this.configManager = new ConfigManager();
        this.config = configManager.Current;

        this.Opened += async (_, __) =>
        {
            if (this.config.RequestsDirectoryPath == null)
            {
                var configWindow = new ConfigWindow();
                var result = await configWindow.ShowDialog<string?>(this);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    config.RequestsDirectoryPath = result;
                    this.configManager.SaveConfig(result);
                    this.ViewModel.HttpViewModel.Collections.RefreshCollections(null);
                }
                else
                {
                    // User closed config window without entering a path, close app
                    Close();
                }
            }
        };
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(this);
    }

    private void OnOpenInExplorer(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(config.RequestsDirectoryPath))
        {
            return;
        }

        try
        {
            if (!Directory.Exists(config.RequestsDirectoryPath))
            {
                Directory.CreateDirectory(config.RequestsDirectoryPath);
            }

            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = config.RequestsDirectoryPath,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = config.RequestsDirectoryPath,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = config.RequestsDirectoryPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open directory in explorer: {ex.Message}");
        }
    }

    private void OnExitClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCloneClicked(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new AppViewModel.CloneRequestCommandMessage());
    }
}
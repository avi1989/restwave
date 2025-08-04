using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RealRestClient.ViewModels;
using RealRestClient.ViewModels.Responses;
using RealRestClient.Models;
using RealRestClient.Services;

namespace RealRestClient.Views;

public partial class MainWindow : Window
{
    private AppViewModel ViewModel => (AppViewModel)DataContext!;

    private Config config;
    private readonly ConfigManager configManager;

    public MainWindow()
    {
        InitializeComponent();
        this.configManager = new ConfigManager();
        this.config = configManager.LoadConfiguration();

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
                }
                else
                {
                    // User closed config window without entering a path, close app
                    Close();
                }
            }
        };
    }


}
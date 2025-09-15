using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RestWave.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadSystemInformation();
        }

        private void LoadSystemInformation()
        {
            // Get version information
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var versionText = this.FindControl<TextBlock>("VersionText");
            if (versionText != null && version != null)
            {
                versionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }

            // Set copyright information
            var copyrightText = this.FindControl<TextBlock>("CopyrightText");
            if (copyrightText != null)
            {
                copyrightText.Text = $"Copyright © RestWave {DateTime.Now.Year}";
            }

            // Set OS information
            var osText = this.FindControl<TextBlock>("OSText");
            if (osText != null)
            {
                string osInfo = Environment.OSVersion.ToString();
                if (OperatingSystem.IsWindows())
                    osInfo = $"Windows {Environment.OSVersion.Version}";
                else if (OperatingSystem.IsLinux())
                    osInfo = "Linux";
                else if (OperatingSystem.IsMacOS())
                    osInfo = "macOS";
                
                osText.Text = osInfo;
            }

            // Set runtime information
            var runtimeText = this.FindControl<TextBlock>("RuntimeText");
            if (runtimeText != null)
            {
                runtimeText.Text = $".NET {Environment.Version}";
            }

            // Set architecture information
            var architectureText = this.FindControl<TextBlock>("ArchitectureText");
            if (architectureText != null)
            {
                architectureText.Text = RuntimeInformation.ProcessArchitecture.ToString();
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

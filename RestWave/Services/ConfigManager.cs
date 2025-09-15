using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RestWave.Models;

namespace RestWave.Services;

public class ConfigManager
{
    private readonly string configPath;
    private static Config config = new Config();
    private static bool isConfigLoaded = false;

    public ConfigManager()
    {
        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string configDir = !string.IsNullOrEmpty(xdgConfigHome)
            ? System.IO.Path.Combine(xdgConfigHome, "restwave")
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "restwave");
        this.configPath = System.IO.Path.Combine(configDir, "config");
        ConfigManager.config = this.LoadConfiguration();
    }

    private Config LoadConfiguration()
    {
        if (ConfigManager.isConfigLoaded)
        {
            return config;
        }

        if (!System.IO.File.Exists(this.configPath))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this.configPath)!);
            System.IO.File.WriteAllText(this.configPath, string.Empty);
        }

        if (System.IO.File.Exists(this.configPath))
        {
            var json = System.IO.File.ReadAllText(this.configPath).Trim();
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    ConfigManager.config = System.Text.Json.JsonSerializer.Deserialize<Config>(json) ?? new Config();
                    ConfigManager.isConfigLoaded = true;
                    return ConfigManager.config;
                }
                catch
                {
                    ConfigManager.config = new Config();
                    return ConfigManager.config;
                }
            }
        }

        ConfigManager.config = new Config();
        ConfigManager.isConfigLoaded = true;
        return ConfigManager.config;
    }

    public void Write()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.configPath)!);
        string json = System.Text.Json.JsonSerializer.Serialize(ConfigManager.config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(this.configPath, json);
    }

    public void SaveConfig(string path)
    {
        ConfigManager.config.RequestsDirectoryPath = path;
        Write();
    }

    public void SaveExpandedFolders(List<string> expandedFolders)
    {
        // store unique and sorted for stability
        ConfigManager.config.ExpandedFolders = expandedFolders.Distinct().OrderBy(x => x).ToList();
        Write();
    }

    public void SaveLastOpenedFilePath(string? filePath)
    {
        ConfigManager.config.LastOpenedFilePath = filePath;
        Write();
    }

    public void SaveTheme(string theme)
    {
        ConfigManager.config.Theme = theme;
        Write();
    }

    public Config Current => ConfigManager.config;
}

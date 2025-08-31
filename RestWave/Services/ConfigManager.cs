using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RestWave.Models;

namespace RestWave.Services;

public class ConfigManager
{
    private readonly string configPath;
    private Config config = new();

    public ConfigManager()
    {
        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        string configDir = !string.IsNullOrEmpty(xdgConfigHome)
            ? System.IO.Path.Combine(xdgConfigHome, "restwave")
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "restwave");
        this.configPath = System.IO.Path.Combine(configDir, "config");
    }

    public Config LoadConfiguration()
    {
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
                    this.config = System.Text.Json.JsonSerializer.Deserialize<Config>(json) ?? new Config();
                    return this.config;
                }
                catch
                {
                    this.config = new Config();
                    return this.config;
                }
            }
        }

        this.config = new Config();
        return this.config;
    }

    public void Write()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.configPath)!);
        string json = System.Text.Json.JsonSerializer.Serialize(this.config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(this.configPath, json);
    }

    public void SaveConfig(string path)
    {
        this.config.RequestsDirectoryPath = path;
        Write();
    }

    public void SaveExpandedFolders(List<string> expandedFolders)
    {
        // store unique and sorted for stability
        this.config.ExpandedFolders = expandedFolders.Distinct().OrderBy(x => x).ToList();
        Write();
    }

    public void SaveLastOpenedFilePath(string? filePath)
    {
        this.config.LastOpenedFilePath = filePath;
        Write();
    }

    public void SaveTheme(string theme)
    {
        this.config.Theme = theme;
        Write();
    }

    public Config Current => this.config;
}
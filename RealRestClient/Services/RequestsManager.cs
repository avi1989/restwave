using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RealRestClient.Models;
using RealRestClient.ViewModels;
using RealRestClient.ViewModels.Requests;

namespace RealRestClient.Services;

public class RequestsManager
{
    private readonly Config config;

    public RequestsManager()
    {
        ConfigManager configManager = new();
        this.config = configManager.LoadConfiguration();
    }

    public ICollection<Node> GetCollections()
    {
        if (!Directory.Exists(this.config.RequestsDirectoryPath))
        {
            Directory.CreateDirectory(this.config.RequestsDirectoryPath!);
        }
        
        var directories = System.IO.Directory.GetDirectories(this.config.RequestsDirectoryPath!);
        var collections = new List<Node>();

        foreach (var dir in directories)
        {
            var dirName = System.IO.Path.GetFileName(dir)!;
            // Add support for subdirectories - looking for JSON files instead of .http files
            var requests = Directory.GetFiles(dir, "*.json").Select(file =>
            {
                var fileName = Path.GetFileNameWithoutExtension(file)!;
                return new Node 
                { 
                    Title = fileName,
                    FilePath = file,
                    IsFolder = false,
                    CollectionName = dirName
                };
            });
            
            var collection = new Node
            {
                Title = dirName,
                FilePath = dir,
                IsFolder = true,
                CollectionName = dirName,
                SubNodes = new ObservableCollection<Node>(requests.ToList())
            };
            collections.Add(collection);
        }

        return collections;
    }

    public void SaveRequest(RequestViewModel request, string requestName)
    {
        if (!Directory.Exists(this.config.RequestsDirectoryPath))
        {
            Directory.CreateDirectory(this.config.RequestsDirectoryPath!);
        }

        // Create a default collection folder if none exists
        var defaultCollectionPath = Path.Combine(this.config.RequestsDirectoryPath!, "Default");
        if (!Directory.Exists(defaultCollectionPath))
        {
            Directory.CreateDirectory(defaultCollectionPath);
        }

        var fileName = $"{requestName}.json";
        var filePath = Path.Combine(defaultCollectionPath, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(request, options);
        File.WriteAllText(filePath, json);
    }

    public void SaveRequestToCollection(RequestViewModel request, string collectionName, string requestName)
    {
        if (!Directory.Exists(this.config.RequestsDirectoryPath))
        {
            Directory.CreateDirectory(this.config.RequestsDirectoryPath!);
        }

        var collectionPath = Path.Combine(this.config.RequestsDirectoryPath!, collectionName);
        if (!Directory.Exists(collectionPath))
        {
            Directory.CreateDirectory(collectionPath);
        }

        var fileName = $"{requestName}.json";
        var filePath = Path.Combine(collectionPath, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(request, options);
        File.WriteAllText(filePath, json);
    }

    public void OverwriteRequest(RequestViewModel request, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(request, options);
        File.WriteAllText(filePath, json);
    }

    public RequestViewModel? LoadRequest(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<RequestViewModel>(json);
        }
        catch
        {
            return null;
        }
    }
}
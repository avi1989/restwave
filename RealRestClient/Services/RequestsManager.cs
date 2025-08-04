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

    public bool MoveFile(string sourceFilePath, string targetCollectionName)
    {
        try
        {
            if (!File.Exists(sourceFilePath))
                return false;

            var fileName = Path.GetFileName(sourceFilePath);
            var targetCollectionPath = Path.Combine(this.config.RequestsDirectoryPath!, targetCollectionName);
            
            if (!Directory.Exists(targetCollectionPath))
            {
                Directory.CreateDirectory(targetCollectionPath);
            }

            var targetFilePath = Path.Combine(targetCollectionPath, fileName);
            
            // Avoid overwriting existing files
            if (File.Exists(targetFilePath))
            {
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var counter = 1;
                
                do
                {
                    fileName = $"{baseName}_{counter}{extension}";
                    targetFilePath = Path.Combine(targetCollectionPath, fileName);
                    counter++;
                }
                while (File.Exists(targetFilePath));
            }

            File.Move(sourceFilePath, targetFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RenameFile(string filePath, string newName)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var directory = Path.GetDirectoryName(filePath)!;
            var extension = Path.GetExtension(filePath);
            var newFilePath = Path.Combine(directory, $"{newName}{extension}");
            
            if (File.Exists(newFilePath))
                return false; // Don't overwrite existing files

            File.Move(filePath, newFilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RenameCollection(string collectionPath, string newName)
    {
        try
        {
            if (!Directory.Exists(collectionPath))
                return false;

            var parentDirectory = Path.GetDirectoryName(collectionPath)!;
            var newCollectionPath = Path.Combine(parentDirectory, newName);
            
            if (Directory.Exists(newCollectionPath))
                return false; // Don't overwrite existing directories

            Directory.Move(collectionPath, newCollectionPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
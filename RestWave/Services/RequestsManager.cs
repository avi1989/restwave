using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RestWave.Models;
using RestWave.ViewModels;
using RestWave.ViewModels.Requests;

namespace RestWave.Services;

public class RequestsManager
{
    private readonly Config config;

    public RequestsManager()
    {
        var configManager = new ConfigManager();
        this.config = configManager.Current;
    }

    public ICollection<Node> GetCollections()
    {
        if (this.config.RequestsDirectoryPath == null)
        {
            return new List<Node>();
        }
        if (!Directory.Exists(this.config.RequestsDirectoryPath))
        {
            Directory.CreateDirectory(this.config.RequestsDirectoryPath!);
        }

        var rootDirectories = System.IO.Directory.GetDirectories(this.config.RequestsDirectoryPath!);
        var collections = new List<Node>();

        foreach (var dir in rootDirectories)
        {
            var collection = BuildNodeHierarchy(dir, null);
            if (collection != null)
            {
                collections.Add(collection);
            }
        }

        return collections;
    }

    private Node? BuildNodeHierarchy(string directoryPath, string? parentCollection, Node? parentNode = null)
    {
        if (!Directory.Exists(directoryPath))
            return null;

        var dirName = Path.GetFileName(directoryPath)!;
        var collectionName = parentCollection ?? dirName;

        var folderNode = new Node
        {
            Title = dirName,
            FilePath = directoryPath,
            IsFolder = true,
            CollectionName = collectionName,
            Parent = parentNode,
            SubNodes = new ObservableCollection<Node>()
        };

        // Get all JSON files (requests) in this directory
        var requests = Directory.GetFiles(directoryPath, "*.json").Select(file =>
        {
            var fileName = Path.GetFileNameWithoutExtension(file)!;
            return new Node
            {
                Title = fileName,
                FilePath = file,
                IsFolder = false,
                CollectionName = collectionName,
                Parent = folderNode
            };
        });

        // Get all subdirectories and build their hierarchies recursively
        var subFolders = Directory.GetDirectories(directoryPath).Select(subDir =>
            BuildNodeHierarchy(subDir, collectionName, folderNode)).Where(node => node != null);

        var allSubNodes = new List<Node>();
        allSubNodes.AddRange(requests);
        allSubNodes.AddRange(subFolders!);

        folderNode.SubNodes = new ObservableCollection<Node>(allSubNodes);

        return folderNode;
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

    public string CreateCollection(string collectionName)
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

        return collectionPath;
    }

    public string CreateNestedFolder(string parentFolderPath, string folderName)
    {
        if (!Directory.Exists(parentFolderPath))
        {
            throw new DirectoryNotFoundException($"Parent folder does not exist: {parentFolderPath}");
        }

        var nestedFolderPath = Path.Combine(parentFolderPath, folderName);
        if (!Directory.Exists(nestedFolderPath))
        {
            Directory.CreateDirectory(nestedFolderPath);
        }

        return nestedFolderPath;
    }

    public void SaveRequestToCollection(RequestViewModel request, string collectionName, string requestName)
    {
        var collectionPath = CreateCollection(collectionName);

        var fileName = $"{requestName}.json";
        var filePath = Path.Combine(collectionPath, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(request, options);
        File.WriteAllText(filePath, json);
    }

    public void SaveRequestToFolder(RequestViewModel request, string folderPath, string requestName)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {folderPath}");
        }

        var fileName = $"{requestName}.json";
        var filePath = Path.Combine(folderPath, fileName);

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

    public bool MoveFolder(string sourceFolderPath, string targetFolderPath)
    {
        try
        {
            if (!Directory.Exists(sourceFolderPath) || !Directory.Exists(targetFolderPath))
                return false;

            var folderName = Path.GetFileName(sourceFolderPath);
            var targetPath = Path.Combine(targetFolderPath, folderName);

            // Avoid overwriting existing folders
            if (Directory.Exists(targetPath))
            {
                var counter = 1;
                var baseName = folderName;

                do
                {
                    folderName = $"{baseName}_{counter}";
                    targetPath = Path.Combine(targetFolderPath, folderName);
                    counter++;
                }
                while (Directory.Exists(targetPath));
            }

            Directory.Move(sourceFolderPath, targetPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool MoveFileToFolder(string sourceFilePath, string targetFolderPath)
    {
        try
        {
            if (!File.Exists(sourceFilePath) || !Directory.Exists(targetFolderPath))
                return false;

            var fileName = Path.GetFileName(sourceFilePath);
            var targetFilePath = Path.Combine(targetFolderPath, fileName);

            // Avoid overwriting existing files
            if (File.Exists(targetFilePath))
            {
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var counter = 1;

                do
                {
                    fileName = $"{baseName}_{counter}{extension}";
                    targetFilePath = Path.Combine(targetFolderPath, fileName);
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
}
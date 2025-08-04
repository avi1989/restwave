using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RealRestClient.Models;
using RealRestClient.ViewModels;

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
            // Add support for subdirectories
            var requests = Directory.GetFiles(dir, "*.http").Select(x => Path.GetFileName(x)!);
            var collection = new Node
            {
                Title = dirName,
                SubNodes = new ObservableCollection<Node>(requests.Select(x => new Node { Title = x }).ToList())
            };
            collections.Add(collection);
        }

        return collections;
    }
}
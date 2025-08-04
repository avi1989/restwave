using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels;

public partial class Node : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Node>? _subNodes;

    [ObservableProperty] private string title;
    
    [ObservableProperty] private string? _filePath;
    
    [ObservableProperty] private bool _isFolder;
    
    [ObservableProperty] private string? _collectionName;
}
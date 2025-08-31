using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RestWave.ViewModels;

public partial class Node : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Node>? _subNodes;

    [ObservableProperty] private string title = string.Empty;

    [ObservableProperty] private string? _filePath;

    [ObservableProperty] private bool _isFolder;

    [ObservableProperty] private string? _collectionName;

    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private string _editingText = string.Empty;

    [ObservableProperty] private bool _isExpanded;

    [ObservableProperty] private Node? _parent;

    public void StartEditing()
    {
        EditingText = Title;
        IsEditing = true;
    }

    public void StopEditing(bool save = false)
    {
        if (save && !string.IsNullOrWhiteSpace(EditingText))
        {
            Title = EditingText.Trim();
        }
        IsEditing = false;
    }

    public string GetFullPath()
    {
        var pathParts = new List<string>();
        var current = this;
        while (current != null)
        {
            pathParts.Add(current.Title);
            current = current.Parent;
        }
        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    public bool IsDescendantOf(Node potentialAncestor)
    {
        var current = this.Parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current.Parent;
        }
        return false;
    }

    public Node? FindRoot()
    {
        var current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }
}
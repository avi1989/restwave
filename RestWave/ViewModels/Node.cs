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
}
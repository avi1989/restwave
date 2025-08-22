using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using RestWave.Views;
using RestWave.Services;

namespace RestWave.ViewModels;

public partial class CollectionsViewModel : ViewModelBase
{
    public CollectionsViewModel()
    {
        this.Collections = new ObservableCollection<Node>();
    }

    [ObservableProperty] private ObservableCollection<Node> _collections;

    [ObservableProperty] private Node? _selectedNode;

    public string? SelectedRequestName => this.SelectedNode?.IsFolder == false ? this.SelectedNode?.Title : "";

    partial void OnSelectedNodeChanged(Node? value)
    {
        OnPropertyChanged(nameof(SelectedRequestName));
    }

    public void RefreshCollections(TreeView? treeView)
    {
        // Capture expanded state before refreshing
        var expandedState = new Dictionary<string, bool>();
        if (treeView != null)
        {
            CaptureTreeViewExpandedState(treeView, expandedState);
        }

        this.Collections.Clear();
        RequestsManager requestsManager = new();
        var collections = requestsManager.GetCollections();
        foreach (var collection in collections)
        {
            this.Collections.Add(collection);
        }

        // Restore expanded state after a short delay to allow UI to update
        if (treeView != null && expandedState.Count > 0)
        {
            Dispatcher.UIThread.Post(() => { RestoreTreeViewExpandedState(treeView, expandedState); },
                DispatcherPriority.Background);
        }
    }

    private void CaptureTreeViewExpandedState(TreeView treeView, Dictionary<string, bool> expandedState)
    {
        var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>().ToList();
        foreach (var item in treeViewItems)
        {
            if (item.DataContext is Node node && node.IsFolder)
            {
                var path = GetNodePath(node);
                expandedState[path] = item.IsExpanded;
            }
        }
    }

    private void RestoreTreeViewExpandedState(TreeView treeView, Dictionary<string, bool> expandedState)
    {
        var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>().ToList();
        foreach (var item in treeViewItems)
        {
            if (item.DataContext is Node node && node.IsFolder)
            {
                var path = GetNodePath(node);
                if (expandedState.TryGetValue(path, out bool wasExpanded))
                {
                    item.IsExpanded = wasExpanded;
                }
            }
        }
    }

    private string GetNodePath(Node node)
    {
        // Create a unique path for the node based on its hierarchy
        var path = node.Title;
        if (!string.IsNullOrEmpty(node.CollectionName))
        {
            path = $"{node.CollectionName}/{node.Title}";
        }

        return path;
    }
}
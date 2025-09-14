using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RestWave.ViewModels;

namespace RestWave.Views.Components;

public class TreeViewService
{
    private const string TreeViewControlName = "CollectionsTreeView";
    private const string EditingTextBoxName = "EditingTextBox";

    public TreeView? GetTreeView(Control parent)
    {
        return parent.FindControl<TreeView>(TreeViewControlName);
    }

    public void FocusEditingTextBox(TreeView treeView, Node node)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var allTextBoxes = treeView.FindDescendantsOfType<TextBox>();
            foreach (var textBox in allTextBoxes)
            {
                if (textBox.Name == EditingTextBoxName && textBox.IsVisible && textBox.DataContext == node)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                    break;
                }
            }
        }, DispatcherPriority.Background);
    }

    public void ExpandNodeInTreeView(TreeView treeView, Node node)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var treeViewItems = treeView.FindDescendantsOfType<TreeViewItem>();
            foreach (var item in treeViewItems)
            {
                if ((item.DataContext as Node)?.FilePath == node.FilePath)
                {
                    item.IsExpanded = true;
                    break;
                }
            }
        }, DispatcherPriority.Background);
    }
}
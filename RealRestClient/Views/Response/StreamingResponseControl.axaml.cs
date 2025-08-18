using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using RealRestClient.ViewModels.Responses;

namespace RealRestClient.Views.Response;

public partial class StreamingResponseControl : UserControl
{
    private ScrollViewer? _scrollViewer;
    private ListBox? _listBox;

    public StreamingResponseControl()
    {
        InitializeComponent();
    }

    public ResponseViewModel ViewModel => (ResponseViewModel)DataContext!;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is ResponseViewModel vm)
        {
            vm.StreamLines.CollectionChanged += StreamLines_CollectionChanged;
        }

        _listBox = this.FindControl<ListBox>("VirtualizedContent");
        if (_listBox != null)
        {
            _listBox.TemplateApplied += ListBox_TemplateApplied;
        }
    }

    private void ListBox_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
    }

    private void StreamLines_CollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_scrollViewer != null)
        {
            Dispatcher.UIThread.Post(() => _scrollViewer.ScrollToEnd());
        }
    }

    private void SelectedGroupChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] == null)
            return;
        
        this.ViewModel.ChangeSelectedGroup(e.AddedItems[0].ToString());
    }
}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RealRestClient.ViewModels.Responses;

public partial class ResponseViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusCode = string.Empty;
    
    private ObservableCollection<string> _streamLines;
    private bool _isStreaming;
    
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _body;

    public ResponseViewModel()
    {
        _streamLines = new ObservableCollection<string>();
    }
    
    public ObservableCollection<string> StreamLines => _streamLines;
    
    
    public void AppendStreamLine(string line)
    {
        _streamLines.Add(line);
    }

    public void ClearStreamLines()
    {
        _streamLines.Clear();
    }
    
    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            _isStreaming = value;
            OnPropertyChanged();
        }
    }

    
    private CancellationTokenSource? _cancellationTokenSource;

    public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

    [ObservableProperty] private ObservableCollection<KeyValuePair<string, string>> _headers = new();
    
    public void StartOperation()
    {
        // Create a new cancellation token source for the operation
        _cancellationTokenSource?.Cancel(); // Cancel any existing operation
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        
        IsLoading = true;
    }

    public void CancelOperation()
    {
        _cancellationTokenSource?.Cancel();
        IsLoading = false;
    }

    public void CompleteOperation()
    {
        IsLoading = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

}
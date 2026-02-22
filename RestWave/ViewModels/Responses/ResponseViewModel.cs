using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using RestWave.Extensions;

namespace RestWave.ViewModels.Responses;

public partial class ResponseViewModel : ViewModelBase
{
    private const int LargeResponseThreshold = 1 * 1024 * 1024; // 1 MB
    private const int TruncationThreshold = 5 * 1024 * 1024; // 5 MB
    private const int TruncationDisplaySize = 1 * 1024 * 1024; // 1 MB

    [ObservableProperty] private string _statusCode = string.Empty;

    private ObservableCollection<string> _streamLines = new();
    private bool _isStreaming;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _body = string.Empty;

    [ObservableProperty] private bool _isLargeResponse;

    [ObservableProperty] private bool _isTruncated;

    [ObservableProperty] private string _truncationMessage = string.Empty;

    public TextDocument BodyDocument { get; } = new();

    [ObservableProperty] private ObservableCollection<string> _groupedKeys = new();

    [ObservableProperty] private string _selectedGroup = "";

    [ObservableProperty] private string _selectedGroupDocument = "";

    [ObservableProperty] private Dictionary<string, string> _groupedResponses = new Dictionary<string, string>();

    public ObservableCollection<string> StreamLines => _streamLines;

    partial void OnBodyChanged(string value)
    {
        var text = value ?? string.Empty;
        var byteLength = System.Text.Encoding.UTF8.GetByteCount(text);

        var isLarge = byteLength > LargeResponseThreshold;
        var isTruncated = byteLength > TruncationThreshold;
        string truncationMessage;

        if (isTruncated)
        {
            var sizeMb = byteLength / (1024.0 * 1024.0);
            truncationMessage = $"Response too large to display fully ({sizeMb:F1} MB). Showing first 1 MB.";
            text = text.Substring(0, FindCharIndexForByteLimit(text, TruncationDisplaySize)) + "\n... [truncated]";
        }
        else
        {
            truncationMessage = string.Empty;
        }

        // All observable property changes and BodyDocument updates must happen on the
        // UI thread — PropertyChanged handlers in the view directly modify UI controls.
        void Apply()
        {
            IsLargeResponse = isLarge;
            IsTruncated = isTruncated;
            TruncationMessage = truncationMessage;
            BodyDocument.Text = text;
            OnPropertyChanged(nameof(ShouldShowCopyButton));
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Apply();
        }
        else
        {
            Dispatcher.UIThread.Post(Apply);
        }
    }

    public void ChangeSelectedGroup(string group)
    {
        SelectedGroup = group;

        if (GroupedResponses.TryGetValue(group, out var value))
        {
            SelectedGroupDocument = value;
        }
        else
        {
            SelectedGroupDocument = string.Empty;
        }
    }

    public void AppendStreamLine(string line)
    {
        _streamLines.Add(line);

        if (line.StartsWith("event: "))
        {
            return;
        }

        if (line.StartsWith("data: "))
            line = line.Substring(6).Trim();
        else if (line.StartsWith("retry: "))
            line = line.Substring(7).Trim();

        if (!JsonValidator.TryValidateJson(line, out var json))
        {
            return;
        }

        if (json == null)
        {
            return;
        }

        switch (json.RootElement.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in json.RootElement.EnumerateObject())
                {
                    if (GroupedResponses.TryGetValue(property.Name, out var value))
                    {
                        if (property.Value.ToString().Trim().StartsWith("{") &&
                            JsonValidator.TryValidateJson(property.Value.ToString(), out var jsonResult))
                        {
                            if (jsonResult == null)
                            {
                                continue;
                            }

                            var prettyJson = JsonSerializer.Serialize(jsonResult.RootElement, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                            GroupedResponses[property.Name] += "\n\n";
                            GroupedResponses[property.Name] += "```";
                            GroupedResponses[property.Name] += prettyJson;
                            GroupedResponses[property.Name] += "\n";
                            GroupedResponses[property.Name] += "```";
                            GroupedResponses[property.Name] += "\n\n";
                        }
                        else
                        {
                            GroupedResponses[property.Name] += property.Value.ToString();
                        }
                    }
                    else
                    {
                        var document = property.Value.ToString();
                        GroupedResponses.Add(property.Name, document);
                        GroupedKeys.Add(property.Name);
                    }

                    if (SelectedGroup == property.Name)
                    {
                        SelectedGroupDocument = GroupedResponses[property.Name];
                    }
                }

                Console.WriteLine(GroupedResponses["data"]);

                break;

            default:
                break;
        }
    }

    private static int FindCharIndexForByteLimit(string text, int maxBytes)
    {
        int byteCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            byteCount += System.Text.Encoding.UTF8.GetByteCount(text, i, 1);
            if (byteCount > maxBytes)
                return i;
        }
        return text.Length;
    }

    public void ClearStreamLines()
    {
        this.IsStreaming = false;
        this.StreamLines.Clear();
        this.SelectedGroupDocument = string.Empty;

        foreach (var group in this.GroupedResponses)
        {
            this.GroupedResponses[group.Key] = string.Empty;
        }
    }

    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            _isStreaming = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShouldShowCopyButton));
        }
    }

    public bool ShouldShowCopyButton => !string.IsNullOrEmpty(this.Body) && !this.IsStreaming;


    private CancellationTokenSource? _cancellationTokenSource;

    public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

    [ObservableProperty] private ObservableCollection<KeyValuePair<string, string>> _headers = new();

    public void StartOperation()
    {
        // Create a new cancellation token source for the operation
        _cancellationTokenSource?.Cancel(); // Cancel any existing operation
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        ClearStreamLines();

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
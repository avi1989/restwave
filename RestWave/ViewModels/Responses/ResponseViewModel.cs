using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using RestWave.Extensions;

namespace RestWave.ViewModels.Responses;

public partial class ResponseViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusCode = string.Empty;

    private ObservableCollection<string> _streamLines = new();
    private bool _isStreaming;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _body = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _groupedKeys = new();

    [ObservableProperty] private string _selectedGroup = "";

    [ObservableProperty] private string _selectedGroupDocument = "";

    [ObservableProperty] private Dictionary<string, string> _groupedResponses = new Dictionary<string, string>();

    public ObservableCollection<string> StreamLines => _streamLines;

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
                        if (property.Value.ToString().Trim().StartsWith("{") && JsonValidator.TryValidateJson(property.Value.ToString(), out var jsonResult))
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
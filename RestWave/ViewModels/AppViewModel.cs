using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RestWave.Services;
using RestWave.ViewModels.Requests;

namespace RestWave.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    [ObservableProperty] private HttpViewModel httpViewModel = new();
    
    [RelayCommand]
    public void CreateNewRequestCommand()
    {
        var requestName = $"Request {DateTime.Now:HHmmss}";
        WeakReferenceMessenger.Default.Send(new CreateRequestCommandMessage(requestName));
    }
    
    public record StartRenamingMessage(string CollectionName);

    public record CreateRequestCommandMessage(string RequestName, RequestViewModel? RequestBody = null);
    
    public record CloneRequestCommandMessage();

}
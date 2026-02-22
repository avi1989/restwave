using System;
using System.ComponentModel;
using Avalonia.Controls;

using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using RestWave.ViewModels.Responses;

namespace RestWave.Views.Response;

public partial class SimpleResponseControl : UserControl
{
    private TextEditor? _responseEditor;
    private ResponseViewModel? _currentViewModel;

    public SimpleResponseControl()
    {
        InitializeComponent();
        _responseEditor = this.FindControl<TextEditor>("ResponseEditor");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _currentViewModel = DataContext as ResponseViewModel;

        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ApplyEditorSettings(_currentViewModel.IsLargeResponse);
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _currentViewModel = null;
        }
        base.OnUnloaded(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResponseViewModel.IsLargeResponse) && _currentViewModel != null)
        {
            ApplyEditorSettings(_currentViewModel.IsLargeResponse);
        }
    }

    private void ApplyEditorSettings(bool isLargeResponse)
    {
        if (_responseEditor == null) return;

        if (isLargeResponse)
        {
            _responseEditor.SyntaxHighlighting = null;
            _responseEditor.WordWrap = false;
        }
        else
        {
            _responseEditor.SyntaxHighlighting =
                HighlightingManager.Instance.GetDefinition("JavaScript");
            _responseEditor.WordWrap = true;
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RestWave.Models;
using RestWave.ViewModels;
using RestWave.ViewModels.Requests;

namespace RestWave.Views
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
            DataContext = new HistoryViewModel();
        }

        public HistoryWindow(HistoryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public event EventHandler<RequestViewModel>? RequestReplay;

        private void ReplayButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RequestHistoryItem historyItem)
            {
                var viewModel = (HistoryViewModel)DataContext!;
                var request = viewModel.ReplayRequest(historyItem);
                
                if (request != null)
                {
                    RequestReplay?.Invoke(this, request);
                    Close();
                }
            }
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            if (DataContext is HistoryViewModel viewModel)
            {
                await viewModel.LoadHistoryCommand.ExecuteAsync(null);
            }
        }
    }
}

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RestWave.Models;
using RestWave.Services;

namespace RestWave.Views
{
	public partial class HistoryWindow : Window
	{
		private readonly RequestHistoryService requestHistoryService;

		public HistoryWindow()
		{
			InitializeComponent();
			requestHistoryService = new RequestHistoryService();
			LoadAll();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void LoadAll()
		{
			var list = this.FindControl<ListBox>("HistoryList");
			IReadOnlyList<RequestHistoryEntry> entries = requestHistoryService.GetAll();
			list.Items = entries;
		}
	}
}


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RestWave.Models;

namespace RestWave.Services
{
	public class RequestHistoryService
	{
		private readonly string historyFilePath;
		private readonly object fileLock = new object();
		private List<RequestHistoryEntry> entries = new List<RequestHistoryEntry>();

		public RequestHistoryService()
		{
			this.historyFilePath = BuildHistoryFilePath();
			Load();
		}

		private static string BuildHistoryFilePath()
		{
			string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			string configDir = !string.IsNullOrEmpty(xdgConfigHome)
				? Path.Combine(xdgConfigHome, "restwave")
				: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "restwave");
			Directory.CreateDirectory(configDir);
			return Path.Combine(configDir, "history.json");
		}

		private void Load()
		{
			try
			{
				if (File.Exists(this.historyFilePath))
				{
					var json = File.ReadAllText(this.historyFilePath).Trim();
					if (!string.IsNullOrEmpty(json))
					{
						entries = JsonSerializer.Deserialize<List<RequestHistoryEntry>>(json) ?? new List<RequestHistoryEntry>();
					}
				}
			}
			catch
			{
				entries = new List<RequestHistoryEntry>();
			}
		}

		private void Persist()
		{
			lock (fileLock)
			{
				var options = new JsonSerializerOptions
				{
					WriteIndented = true
				};
				var json = JsonSerializer.Serialize(entries, options);
				File.WriteAllText(this.historyFilePath, json);
			}
		}

		public void Add(RequestHistoryEntry entry)
		{
			entries.Add(entry);
			// cap size to avoid unbounded growth (e.g., last 1000)
			if (entries.Count > 1000)
			{
				entries = entries.OrderByDescending(e => e.Timestamp).Take(1000).ToList();
			}
			Persist();
		}

		public IReadOnlyList<RequestHistoryEntry> GetAll()
		{
			return entries.OrderByDescending(e => e.Timestamp).ToList();
		}

		public IReadOnlyList<RequestHistoryEntry> GetByKey(string key)
		{
			return entries.Where(e => e.Key == key).OrderByDescending(e => e.Timestamp).ToList();
		}

		public void Clear()
		{
			entries.Clear();
			Persist();
		}
	}
}


using System;
using System.Collections.Generic;

namespace RestWave.Models
{
	public class RequestHistoryEntry
	{
		public DateTime Timestamp { get; set; }

		public string Key { get; set; } = string.Empty; // Identity for grouping (file path or method+url)

		public string Method { get; set; } = string.Empty;

		public string Url { get; set; } = string.Empty;

		public List<KeyValuePair<string, string>> RequestHeaders { get; set; } = new List<KeyValuePair<string, string>>();

		public string RequestBody { get; set; } = string.Empty;

		public string StatusCode { get; set; } = string.Empty;

		public List<KeyValuePair<string, string>> ResponseHeaders { get; set; } = new List<KeyValuePair<string, string>>();

		public string ResponseBody { get; set; } = string.Empty;

		public long DurationMs { get; set; }
	}
}


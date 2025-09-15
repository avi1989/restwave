using System;
using System.Collections.Generic;

namespace RestWave.Models
{
    public class RequestHistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public long ResponseTime { get; set; } // in milliseconds
        public long ResponseSize { get; set; } // in bytes
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, string> ResponseHeaders { get; set; } = new();
        public string ResponseBody { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;
        public string RequestName { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using RestWave.Models;
using RestWave.ViewModels.Requests;

namespace RestWave.Services
{
    public class HistoryManager
    {
        private readonly Config _config;
        private readonly string _databasePath;
        private readonly string _connectionString;

        public HistoryManager()
        {
            var configManager = new ConfigManager();
            _config = configManager.Current;
            
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RestWave");
            Directory.CreateDirectory(appDataPath);
            
            _databasePath = Path.Combine(appDataPath, "history.db");
            _connectionString = $"Data Source={_databasePath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS RequestHistory (
                    Id TEXT PRIMARY KEY,
                    Timestamp TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    Method TEXT NOT NULL,
                    Headers TEXT,
                    Body TEXT,
                    StatusCode TEXT,
                    ResponseTime INTEGER,
                    ResponseSize INTEGER,
                    IsSuccess INTEGER,
                    ErrorMessage TEXT,
                    ResponseHeaders TEXT,
                    ResponseBody TEXT,
                    CollectionName TEXT,
                    RequestName TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_timestamp ON RequestHistory(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_url ON RequestHistory(Url);
                CREATE INDEX IF NOT EXISTS idx_method ON RequestHistory(Method);
                CREATE INDEX IF NOT EXISTS idx_status ON RequestHistory(StatusCode);
            ";
            createTableCommand.ExecuteNonQuery();
        }

        public async Task<string> SaveRequestHistoryAsync(RequestViewModel request, string statusCode, long responseTime, 
            long responseSize, bool isSuccess, string errorMessage = "", 
            Dictionary<string, string>? responseHeaders = null, string responseBody = "", 
            string collectionName = "", string requestName = "")
        {
            if (!_config.EnableHistory)
                return string.Empty;

            var historyItem = new RequestHistoryItem
            {
                Url = request.Url,
                Method = request.Method,
                Headers = ConvertHeadersToDict(request.HeadersInput),
                Body = _config.SaveSensitiveData ? request.JsonBodyInput.JsonText : FilterSensitiveData(request.JsonBodyInput.JsonText),
                StatusCode = statusCode,
                ResponseTime = responseTime,
                ResponseSize = responseSize,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                ResponseHeaders = responseHeaders ?? new Dictionary<string, string>(),
                ResponseBody = _config.SaveResponseBodies ? responseBody : string.Empty,
                CollectionName = collectionName,
                RequestName = requestName
            };

            await SaveHistoryItemAsync(historyItem);
            await CleanupOldHistoryAsync();
            
            return historyItem.Id;
        }

        private async Task SaveHistoryItemAsync(RequestHistoryItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO RequestHistory 
                (Id, Timestamp, Url, Method, Headers, Body, StatusCode, ResponseTime, ResponseSize, 
                 IsSuccess, ErrorMessage, ResponseHeaders, ResponseBody, CollectionName, RequestName)
                VALUES 
                (@Id, @Timestamp, @Url, @Method, @Headers, @Body, @StatusCode, @ResponseTime, @ResponseSize,
                 @IsSuccess, @ErrorMessage, @ResponseHeaders, @ResponseBody, @CollectionName, @RequestName)";

            command.Parameters.AddWithValue("@Id", item.Id);
            command.Parameters.AddWithValue("@Timestamp", item.Timestamp.ToString("O"));
            command.Parameters.AddWithValue("@Url", item.Url);
            command.Parameters.AddWithValue("@Method", item.Method);
            command.Parameters.AddWithValue("@Headers", JsonSerializer.Serialize(item.Headers));
            command.Parameters.AddWithValue("@Body", item.Body);
            command.Parameters.AddWithValue("@StatusCode", item.StatusCode);
            command.Parameters.AddWithValue("@ResponseTime", item.ResponseTime);
            command.Parameters.AddWithValue("@ResponseSize", item.ResponseSize);
            command.Parameters.AddWithValue("@IsSuccess", item.IsSuccess ? 1 : 0);
            command.Parameters.AddWithValue("@ErrorMessage", item.ErrorMessage);
            command.Parameters.AddWithValue("@ResponseHeaders", JsonSerializer.Serialize(item.ResponseHeaders));
            command.Parameters.AddWithValue("@ResponseBody", item.ResponseBody);
            command.Parameters.AddWithValue("@CollectionName", item.CollectionName);
            command.Parameters.AddWithValue("@RequestName", item.RequestName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<RequestHistoryItem>> GetHistoryAsync(int limit = 100, int offset = 0,
            string? urlFilter = null, string? methodFilter = null, string? statusFilter = null,
            string? collectionNameFilter = null, string? requestNameFilter = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrEmpty(urlFilter))
                whereClause += " AND Url LIKE @UrlFilter";
            if (!string.IsNullOrEmpty(methodFilter))
                whereClause += " AND Method = @MethodFilter";
            if (!string.IsNullOrEmpty(statusFilter))
                whereClause += " AND StatusCode LIKE @StatusFilter";
            if (!string.IsNullOrEmpty(collectionNameFilter))
                whereClause += " AND CollectionName = @CollectionNameFilter";
            if (!string.IsNullOrEmpty(requestNameFilter))
                whereClause += " AND RequestName = @RequestNameFilter";

            command.CommandText = $@"
                SELECT * FROM RequestHistory 
                {whereClause}
                ORDER BY Timestamp DESC 
                LIMIT @Limit OFFSET @Offset";

            command.Parameters.AddWithValue("@Limit", limit);
            command.Parameters.AddWithValue("@Offset", offset);

            if (!string.IsNullOrEmpty(urlFilter))
                command.Parameters.AddWithValue("@UrlFilter", $"%{urlFilter}%");
            if (!string.IsNullOrEmpty(methodFilter))
                command.Parameters.AddWithValue("@MethodFilter", methodFilter);
            if (!string.IsNullOrEmpty(statusFilter))
                command.Parameters.AddWithValue("@StatusFilter", $"{statusFilter}%");
            if (!string.IsNullOrEmpty(collectionNameFilter))
                command.Parameters.AddWithValue("@CollectionNameFilter", collectionNameFilter);
            if (!string.IsNullOrEmpty(requestNameFilter))
                command.Parameters.AddWithValue("@RequestNameFilter", requestNameFilter);

            var history = new List<RequestHistoryItem>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = new RequestHistoryItem
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    Timestamp = DateTime.Parse(reader["Timestamp"].ToString() ?? DateTime.UtcNow.ToString("O")),
                    Url = reader["Url"].ToString() ?? string.Empty,
                    Method = reader["Method"].ToString() ?? "GET",
                    Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(reader["Headers"].ToString() ?? "{}") ?? new(),
                    Body = reader["Body"].ToString() ?? string.Empty,
                    StatusCode = reader["StatusCode"].ToString() ?? string.Empty,
                    ResponseTime = Convert.ToInt64(reader["ResponseTime"]),
                    ResponseSize = Convert.ToInt64(reader["ResponseSize"]),
                    IsSuccess = Convert.ToInt32(reader["IsSuccess"]) == 1,
                    ErrorMessage = reader["ErrorMessage"].ToString() ?? string.Empty,
                    ResponseHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(reader["ResponseHeaders"].ToString() ?? "{}") ?? new(),
                    ResponseBody = reader["ResponseBody"].ToString() ?? string.Empty,
                    CollectionName = reader["CollectionName"].ToString() ?? string.Empty,
                    RequestName = reader["RequestName"].ToString() ?? string.Empty
                };
                history.Add(item);
            }

            return history;
        }

        public async Task<List<RequestHistoryItem>> GetRecentRequestsAsync(int limit = 20)
        {
            return await GetHistoryAsync(limit, 0);
        }

        public async Task<RequestHistoryItem?> GetHistoryItemAsync(string id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM RequestHistory WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new RequestHistoryItem
                {
                    Id = reader["Id"].ToString() ?? string.Empty,
                    Timestamp = DateTime.Parse(reader["Timestamp"].ToString() ?? DateTime.UtcNow.ToString("O")),
                    Url = reader["Url"].ToString() ?? string.Empty,
                    Method = reader["Method"].ToString() ?? "GET",
                    Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(reader["Headers"].ToString() ?? "{}") ?? new(),
                    Body = reader["Body"].ToString() ?? string.Empty,
                    StatusCode = reader["StatusCode"].ToString() ?? string.Empty,
                    ResponseTime = Convert.ToInt64(reader["ResponseTime"]),
                    ResponseSize = Convert.ToInt64(reader["ResponseSize"]),
                    IsSuccess = Convert.ToInt32(reader["IsSuccess"]) == 1,
                    ErrorMessage = reader["ErrorMessage"].ToString() ?? string.Empty,
                    ResponseHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(reader["ResponseHeaders"].ToString() ?? "{}") ?? new(),
                    ResponseBody = reader["ResponseBody"].ToString() ?? string.Empty,
                    CollectionName = reader["CollectionName"].ToString() ?? string.Empty,
                    RequestName = reader["RequestName"].ToString() ?? string.Empty
                };
            }

            return null;
        }

        public async Task DeleteHistoryItemAsync(string id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RequestHistory WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task ClearHistoryAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RequestHistory";
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateRequestNameAsync(string oldCollectionName, string oldRequestName, string newRequestName)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RequestHistory
                SET RequestName = @NewRequestName
                WHERE CollectionName = @CollectionName AND RequestName = @OldRequestName";

            command.Parameters.AddWithValue("@NewRequestName", newRequestName);
            command.Parameters.AddWithValue("@CollectionName", oldCollectionName);
            command.Parameters.AddWithValue("@OldRequestName", oldRequestName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateCollectionNameAsync(string oldCollectionName, string newCollectionName)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RequestHistory
                SET CollectionName = @NewCollectionName
                WHERE CollectionName = @OldCollectionName";

            command.Parameters.AddWithValue("@NewCollectionName", newCollectionName);
            command.Parameters.AddWithValue("@OldCollectionName", oldCollectionName);

            await command.ExecuteNonQueryAsync();
        }

        public async Task MoveRequestToCollectionAsync(string oldCollectionName, string requestName, string newCollectionName)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE RequestHistory
                SET CollectionName = @NewCollectionName
                WHERE CollectionName = @OldCollectionName AND RequestName = @RequestName";

            command.Parameters.AddWithValue("@NewCollectionName", newCollectionName);
            command.Parameters.AddWithValue("@OldCollectionName", oldCollectionName);
            command.Parameters.AddWithValue("@RequestName", requestName);

            await command.ExecuteNonQueryAsync();
        }

        private async Task CleanupOldHistoryAsync()
        {
            if (_config.MaxHistoryItems <= 0 && _config.HistoryRetentionDays <= 0)
                return;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Clean up by count
            if (_config.MaxHistoryItems > 0)
            {
                var countCommand = connection.CreateCommand();
                countCommand.CommandText = @"
                    DELETE FROM RequestHistory 
                    WHERE Id NOT IN (
                        SELECT Id FROM RequestHistory 
                        ORDER BY Timestamp DESC 
                        LIMIT @MaxItems
                    )";
                countCommand.Parameters.AddWithValue("@MaxItems", _config.MaxHistoryItems);
                await countCommand.ExecuteNonQueryAsync();
            }

            // Clean up by age
            if (_config.HistoryRetentionDays > 0)
            {
                var ageCommand = connection.CreateCommand();
                ageCommand.CommandText = "DELETE FROM RequestHistory WHERE Timestamp < @CutoffDate";
                var cutoffDate = DateTime.UtcNow.AddDays(-_config.HistoryRetentionDays);
                ageCommand.Parameters.AddWithValue("@CutoffDate", cutoffDate.ToString("O"));
                await ageCommand.ExecuteNonQueryAsync();
            }
        }

        private Dictionary<string, string> ConvertHeadersToDict(HeadersInputViewModel headersInput)
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in headersInput.Headers)
            {
                if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value))
                {
                    headers[header.Key] = header.Value;
                }
            }
            return headers;
        }

        private string FilterSensitiveData(string body)
        {
            // Basic sensitive data filtering - can be enhanced
            if (string.IsNullOrEmpty(body))
                return body;

            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                var filtered = FilterJsonElement(jsonDoc.RootElement);
                return JsonSerializer.Serialize(filtered, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                // If not valid JSON, return as-is or apply basic string filtering
                return body;
            }
        }

        private object FilterJsonElement(JsonElement element)
        {
            var sensitiveKeys = new[] { "password", "token", "secret", "key", "auth", "authorization" };

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (sensitiveKeys.Any(key => prop.Name.ToLower().Contains(key)))
                        {
                            obj[prop.Name] = "[FILTERED]";
                        }
                        else
                        {
                            obj[prop.Name] = FilterJsonElement(prop.Value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(FilterJsonElement).ToArray();

                default:
                    return element.GetRawText().Trim('"');
            }
        }
    }
}

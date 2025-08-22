namespace RestWave.Extensions;

using System.Text.Json;

public static class JsonValidator
{
    public static bool TryValidateJson(string json, out JsonDocument? jsonDocument)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            jsonDocument = null;
            return false;
        }

        try
        {
            var response = JsonDocument.Parse(json);
            jsonDocument = response;
            return true;
        }
        catch (JsonException)
        {
            jsonDocument = null;
            return false;
        }
    }

    public static bool IsValidJson(string json)
    {
        return TryValidateJson(json, out _);
    }
}
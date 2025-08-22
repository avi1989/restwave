using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RestWave.ViewModels.Requests;

public partial class JsonBodyInputViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _jsonText = "{\n  \"key\": \"value\"\n}";

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _hasValidationError;
    
    partial void OnJsonTextChanged(string value)
    {
        ValidateJson(value);
    }

    
    public void ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            ValidationError = "JSON cannot be empty";
            HasValidationError = true;
            return;
        }

        try
        {
            // Try to parse the JSON to validate it
            using (JsonDocument.Parse(json))
            {
                ValidationError = string.Empty;
                HasValidationError = false;
            }
        }
        catch (JsonException ex)
        {
            ValidationError = $"Invalid JSON: {ex.Message}";
            HasValidationError = true;
        }
    }
}
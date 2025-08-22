using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RestWave.Views.Components;

public class FolderIconConverter : IValueConverter
{
    public static readonly FolderIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFolder)
        {
            return isFolder ? "📁" : "📄";
        }
        return "📄";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

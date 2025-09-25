using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RestWave.Views
{
    public class MethodColorConverter : IValueConverter
    {
        public static readonly MethodColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string method)
            {
                return method.ToUpper() switch
                {
                    "GET" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                    "POST" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),   // Amber
                    "PUT" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
                    "DELETE" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                    "PATCH" => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // Purple
                    "HEAD" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Blue Grey
                    "OPTIONS" => new SolidColorBrush(Color.FromRgb(121, 85, 72)), // Brown
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))       // Grey
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusCodeColorConverter : IValueConverter
    {
        public static readonly StatusCodeColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string statusCode && !string.IsNullOrEmpty(statusCode))
            {
                if (statusCode.StartsWith("2"))
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));    // Green
                if (statusCode.StartsWith("3"))
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7));    // Amber
                if (statusCode.StartsWith("4"))
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));    // Orange
                if (statusCode.StartsWith("5"))
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));    // Red
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));          // Grey
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimestampConverter : IValueConverter
    {
        public static readonly TimestampConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime timestamp)
            {
                // Ensure both timestamps are in the same timezone for comparison
                var now = DateTime.UtcNow;
                var timestampUtc = timestamp.Kind == DateTimeKind.Utc ? timestamp : timestamp.ToUniversalTime();
                var diff = now - timestampUtc;

                if (diff.TotalMinutes < 1)
                    return "Just now";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours}h ago";
                if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays}d ago";
                
                return timestampUtc.ToLocalTime().ToString("MMM dd, yyyy");
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponseTimeConverter : IValueConverter
    {
        public static readonly ResponseTimeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long milliseconds)
            {
                if (milliseconds < 1000)
                    return $"{milliseconds}ms";
                
                return $"{milliseconds / 1000.0:F1}s";
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ResponseSizeConverter : IValueConverter
    {
        public static readonly ResponseSizeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                if (bytes < 1024)
                    return $"{bytes} B";
                if (bytes < 1024 * 1024)
                    return $"{bytes / 1024.0:F1} KB";
                
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

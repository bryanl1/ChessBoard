using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
    /// <summary>
    /// Converts an integer player id into black or white
    /// 1 -> "White"
    /// 2 -> "Black"
    /// </summary>
    public class PlayerIntToStringConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            try {
                if (value == null) return "Error";
                int v = value is int i ? i : System.Convert.ToInt32(value);
                return v switch {
                    0 => "None",
                    1 => "White",
                    2 => "Black",
                    _ => $"Player {v}",
                };
            } catch {
                return "Unknown";
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}

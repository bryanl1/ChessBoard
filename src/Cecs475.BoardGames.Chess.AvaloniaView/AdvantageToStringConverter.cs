using Avalonia.Data.Converters;
using Cecs475.BoardGames.Chess.Model;
using System;
using System.Globalization;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
    /// <summary>
    /// Converts a GameAdvantage to string 
    /// If Advantage is 0 -> "Tie game".
    /// </summary>
    public class AdvantageToStringConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is not GameAdvantage v) {
                throw new ArgumentException("ERROR in GameAdvantage", nameof(value));
            }

            if (v.Advantage == 0 || v.Player == 0)
                return "Tie game";

            string playerName = v.Player switch {
                1 => "White",
                2 => "Black",
                _ => $"Player {v.Player}",
            };

            // show + sign for positive advantage
            var adv = v.Advantage >= 0 ? $"+{v.Advantage}" : v.Advantage.ToString();
            return $"{playerName} has a {adv} advantage";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}

using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
	/// <summary>
	/// Converts multiple values from a ChessSquare 
    /// Background Light or Dark depending on row and column
    /// Square selected by click is red,
    /// Square hovered over after a select is light green
    /// And square hovered, without selection, but a possible move is light green
	/// </summary>
	internal class ChessSquareBackgroundMultiConverter : IMultiValueConverter {
		private static readonly IBrush LightBrush = Brushes.White;
		private static readonly IBrush DarkBrush = Brushes.DarkGray;
		private static readonly IBrush CheckBrush = Brushes.Yellow;
		private static readonly IBrush SelectedBrush = Brushes.Red;
		private static readonly IBrush HighlightBrush = Brushes.LightGreen;

		public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
			if (values == null || values.Count < 2)
				return LightBrush;

			try {
                //AI generated getting all the available objects for all possible
				var rowObj = values[0];
				var colObj = values[1];
				object? selObj = values.Count > 2 ? values[2] : null;
				object? hovObj = values.Count > 3 ? values[3] : null;
				object? hlObj = values.Count > 4 ? values[4] : null;
				object? checkObj = values.Count > 5 ? values[5] : null;
				int rowInt = rowObj is int r ? r : System.Convert.ToInt32(rowObj);
				int colInt = colObj is int c ? c : System.Convert.ToInt32(colObj);
                //downcasts
				bool isSelected = selObj is bool sb && sb;
				bool isHovered = hovObj is bool hb && hb;
				bool isHighlighted = hlObj is bool hb2 && hb2;
				bool isKingInCheck = checkObj is bool cb && cb;

				if (isKingInCheck)
					return CheckBrush;
				if (isSelected)
					return SelectedBrush;
				if (isHighlighted)
					return HighlightBrush;

				return ((rowInt + colInt) & 1) == 0 ? LightBrush : DarkBrush;
			} catch {
				return LightBrush;
			}
		}

		public object? ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
			throw new NotSupportedException();
		}
	}
}

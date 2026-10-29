using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using Cecs475.BoardGames.Chess.Model;
using System.Diagnostics;

namespace Cecs475.BoardGames.Chess.AvaloniaView
{
    internal class PieceImageConverter : IValueConverter {
        private static readonly Dictionary<string, Bitmap> s_cache = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            Debug.WriteLine("HELLO HELLO piece is ");
            // Accept either a ChessSquare or a ChessPiece directly.
            ChessPiece? piece = null;
            if (value is null) return null;

            // If the value is an object that has a Piece property (the ChessSquare), extract it via reflection.
            try {
                var valType = value.GetType();
                var prop = valType.GetProperty("Piece");
                if (prop != null) {
                    var p = prop.GetValue(value);
                    if (p is ChessPiece cp) piece = cp;
                }
            } catch {
                // 
            }

            if (value is ChessPiece d) piece = d;
            //direct.PieceType.ToString();
            if (piece == null) return null;

            if (!(piece is ChessPiece pnn)) return null;
            var pType = pnn.PieceType;
            if (pType == ChessPieceType.Empty) return null;

            string role = pType.ToString().ToLowerInvariant();//lowercase function..
            string color = pnn.Player == 1 ? "white" : "black";

            var candidates = new List<string> {
                $"{color}-{role}.png"
            };

            foreach (var file in candidates) {
                var assetPath = $"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{file}";
                if (s_cache.TryGetValue(assetPath, out var bmp)) return bmp;
                try {
                    using var stream = AssetLoader.Open(new Uri(assetPath));
                    bmp = new Bitmap(stream);
                    s_cache[assetPath] = bmp;
                    return bmp;
                } catch {
                    // try next candidate
                }
            }

            // Fallback added if there is an issue?
            try {
                var fallback = "avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/black-knight.png";
                if (s_cache.TryGetValue(fallback, out var fb)) return fb;
                using var stream = AssetLoader.Open(new Uri(fallback));
                var fbmp = new Bitmap(stream);
                s_cache[fallback] = fbmp;
                return fbmp;
            } catch {
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
    }

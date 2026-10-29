using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Cecs475.BoardGames.Chess.Model;

namespace Cecs475.BoardGames.Chess.AvaloniaView;

//Promotion window, loads the images, does the promotion.
public partial class PromotionWindow : Window
{
    private readonly ChessViewModel mViewModel;
    private readonly BoardPosition mStart;
    private readonly BoardPosition mEnd;

    public PromotionWindow(ChessViewModel vm, BoardPosition start, BoardPosition end)
    {
        InitializeComponent();
        mViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        mStart = start;
        mEnd = end;
        //		private static readonly IBrush LightBrush = Brushes.White;
        //		private static readonly IBrush DarkBrush = Brushes.DarkGray;
        var player = mViewModel.CurrentPlayer;
        var foreground = player == 1 ? Brushes.White : Brushes.Black;
        var bg = player == 1 ? Brushes.Black : Brushes.White;

        string color = player == 1 ? "white" : "black";
        string basePath = "avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/";
        try
        {
            var imgKnight = this.FindControl<Image>("imgKnight");
            var imgBishop = this.FindControl<Image>("imgBishop");
            var imgRook = this.FindControl<Image>("imgRook");
            var imgQueen = this.FindControl<Image>("imgQueen");

            if (imgKnight != null)
            {
                using var s = AssetLoader.Open(new Uri(basePath + $"{color}-knight.png"));
                imgKnight.Source = new Bitmap(s);
            }
            if (imgBishop != null)
            {
                using var s = AssetLoader.Open(new Uri(basePath + $"{color}-bishop.png"));
                imgBishop.Source = new Bitmap(s);
            }
            if (imgRook != null)
            {
                using var s = AssetLoader.Open(new Uri(basePath + $"{color}-rook.png"));
                imgRook.Source = new Bitmap(s);
            }
            if (imgQueen != null)
            {
                using var s = AssetLoader.Open(new Uri(basePath + $"{color}-queen.png"));
                imgQueen.Source = new Bitmap(s);
            }
            // Set border backgrounds to player's color so images are shown with correct contrast.
            var bKnight = this.FindControl<Border>("btnKnight");
            var bBishop = this.FindControl<Border>("btnBishop");
            var bRook = this.FindControl<Border>("btnRook");
            var bQueen = this.FindControl<Border>("btnQueen");
            if (bKnight != null) bKnight.Background = bg;
            if (bBishop != null) bBishop.Background = bg;
            if (bRook != null) bRook.Background = bg;
            if (bQueen != null) bQueen.Background = bg;
        }
        catch
        { //do nothing
        }
    }

    private void OnPiecePointerEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border b)
        {
            b.Background = Brushes.LightGray;
        }
    }

    private void OnPiecePointerExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border b)
        {
            var player = mViewModel.CurrentPlayer;
            var bg = player == 1 ? Brushes.Black : Brushes.White;
            b.Background = bg;
        }
    }

    private void OnPromoteKnight(object? sender, PointerPressedEventArgs e)
    {
        ApplyPromotion(ChessPieceType.Knight);
    }

    private void OnPromoteBishop(object? sender, PointerPressedEventArgs e)
    {
        ApplyPromotion(ChessPieceType.Bishop);
    }

    private void OnPromoteRook(object? sender, PointerPressedEventArgs e)
    {
        ApplyPromotion(ChessPieceType.Rook);
    }

    private void OnPromoteQueen(object? sender, PointerPressedEventArgs e)
    {
        ApplyPromotion(ChessPieceType.Queen);
    }

    private void ApplyPromotion(ChessPieceType type)
    {
        var move = new PawnPromotionChessMove(mStart, mEnd, type);
        mViewModel.ApplyMove(move);
        Close(); //control the window
    }
}
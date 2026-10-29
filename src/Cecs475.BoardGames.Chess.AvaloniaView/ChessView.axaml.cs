using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cecs475.BoardGames.Chess.Model;
using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Cecs475.BoardGames.Chess.AvaloniaView;

public partial class ChessView : UserControl {
	public ChessView() {
		InitializeComponent();
	}

	public Control ViewControl => this;

	public ChessViewModel ChessViewModel => (ChessViewModel)this.FindResource("vm")!;

	private void Panel_PointerEntered(object? sender, PointerEventArgs e) {
		if (sender is not Control c) { throw new ArgumentException(nameof(sender)); }
		var square = (ChessViewModel.ChessSquare)c.DataContext!;
		square.IsHovered = true;

		var vm = (ChessViewModel)this.FindResource("vm")!;
		var selected = vm.Squares.FirstOrDefault(s => s.IsSelected);
		if (selected != null) {
			// highlight if any possible move has this start and end
			var start = new BoardPosition(selected.Row, selected.Col);
			var end = new BoardPosition(square.Row, square.Col);
			bool isLegal = vm.PossibleMoves.Any(m => m.StartPosition.Equals(start) && m.EndPosition.Equals(end));
			square.IsHighlighted = isLegal;
		} else {
			var thisPos = new BoardPosition(square.Row, square.Col);
			square.IsHighlighted = vm.PossibleMoves.Any(m => m.StartPosition.Equals(thisPos));
		}
	}

	private void Panel_PointerExited(object? sender, PointerEventArgs e) {
		if (sender is not Control c) { throw new ArgumentException(nameof(sender)); }
		var square = (ChessViewModel.ChessSquare)c.DataContext!;
		square.IsHovered = false;
		square.IsHighlighted = false;
	}

	private async void Panel_PointerReleased(object? sender, PointerReleasedEventArgs e) {
		if (sender is not Control c) { throw new ArgumentException(nameof(sender)); }
		var square = (ChessViewModel.ChessSquare)c.DataContext!;
		var vm = ChessViewModel;
		var selected = vm.Squares.FirstOrDefault(s => s.IsSelected);
		if (selected != null) {
			//reclick
			if (selected == square) {
				selected.IsSelected = false;
				return;
			}

			if (selected != square) {
			var start = new BoardPosition(selected.Row, selected.Col);
			var end = new BoardPosition(square.Row, square.Col);
			var candidates = vm.PossibleMoves.Where(m => m.StartPosition.Equals(start) && m.EndPosition.Equals(end)).ToList();
			if (candidates.Count == 1) {
				vm.ApplyMove(candidates[0]);
				return;
			}
			else if (candidates.Count > 1) {
				// Pawn promotion
				//AI functionality! double check
				var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
				var dlg = new PromotionWindow(vm, start, end);
				if (owner != null) {
					await dlg.ShowDialog((Window)owner);
				} else {
					await dlg.ShowDialog((Window?)null!);
				}
				return;
			}
				// cancel 
				selected.IsSelected = false;
				return;
			}
		}
		// No selectable
		var startPos = new BoardPosition(square.Row, square.Col);
		bool hasOwnMove = vm.PossibleMoves.Any(m => m.StartPosition.Equals(startPos));
		if (hasOwnMove) {
			foreach (var s in vm.Squares) s.IsSelected = false;
			square.IsSelected = true;
		} else {
			foreach (var s in vm.Squares) s.IsSelected = false;
		}
	}

	private void UndoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
		var vm = ChessViewModel;
		vm.UndoMove();
	}
}
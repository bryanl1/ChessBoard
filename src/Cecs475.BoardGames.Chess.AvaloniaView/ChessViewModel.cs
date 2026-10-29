using Cecs475.BoardGames;
using Cecs475.BoardGames.Chess.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
	
	/// <summary>
	/// Represents the state of a View of a Chess Board
	/// Exposes the necessary properties to bind to the View
	/// 
	/// </summary>
	public class ChessViewModel : INotifyPropertyChanged {

		private ChessBoard mBoard;
		private ObservableCollection<ChessSquare> mSquares;

		//Initialize the Board,Squares
		public ChessViewModel() {
			mBoard = new ChessBoard();
			mSquares = new ObservableCollection<ChessSquare>(
				BoardPosition.GetRectangularPositions(8, 8)
				.Select((p, idx) => new ChessSquare(
					mBoard.GetPieceAtPosition(p),
					idx
				))
			);
			}
			// expose squares
			public ObservableCollection<ChessSquare> Squares {
				get { return mSquares; }
			}
			public IEnumerable<ChessMove> PossibleMoves => mBoard.GetPossibleMoves();

		public int CurrentPlayer
		{
			get => mBoard.CurrentPlayer;
		}
		
		//True if there is at least one move in the board's history
		public bool CanUndo {
			get => mBoard.MoveHistory.Any();
		}

		public GameAdvantage BoardAdvantage {
			get => mBoard.CurrentAdvantage;
		}

		public void UndoMove() {
			if (!CanUndo) return;
			mBoard.UndoLastMove();
			RebindState();
		}

		//Applies the move, from start to end.
		public void ApplyMove(BoardPosition start, BoardPosition end) {
			ChessMove? toApply = null;
			foreach (var mv in mBoard.GetPossibleMoves()) {
				if (mv.StartPosition.Equals(start) && mv.EndPosition.Equals(end)) {
					toApply = mv;
					break;
				}
			}

			if (toApply != null) {
				mBoard.ApplyMove(toApply);
				RebindState();
				if (mBoard.IsFinished) {
					GameFinished?.Invoke(this, new EventArgs());
				}
			}
		}

		public void ApplyMove(ChessMove move)
		{
			mBoard.ApplyMove(move);
			RebindState();
			if (mBoard.IsFinished)
			{
				GameFinished?.Invoke(this, new EventArgs());
			}
		}

		// Update possible moves ChessBoard.GetPossibleMoves returns ChessMove objects; expose them directly as in PossibleMoves
		//ai func.
		//Refresh everthing!!
		private void RebindState() {
			var positions = BoardPosition.GetRectangularPositions(8, 8).ToList();
			for (int i = 0; i < positions.Count; ++i) {
				var pos = positions[i];
				mSquares[i].Piece = mBoard.GetPieceAtPosition(pos);
				mSquares[i].IsSelected = false;
				mSquares[i].IsHovered = false;
				mSquares[i].IsHighlighted = false;
				// mark king when in check
				var sqPiece = mBoard.GetPieceAtPosition(pos);
				mSquares[i].IsKingInCheck = (mBoard.IsCheck && sqPiece.PieceType == ChessPieceType.King && sqPiece.Player == mBoard.CurrentPlayer);
			}

			OnPropertyChanged(nameof(BoardAdvantage));
			OnPropertyChanged(nameof(CurrentPlayer));
			OnPropertyChanged(nameof(CanUndo));
		}

		// Chess Square, exposes
        public class ChessSquare : INotifyPropertyChanged
        {
			private int mPlayer;
			private ChessPiece? mPiece;
			public ChessPiece? Piece {
				get => mPiece;
				set {
					if (!Equals(value, mPiece)) {
						mPiece = value;
						OnPropertyChanged();
					}
				}
			}

			public int Row { get; }
			public int Col { get; }

			private bool mIsSelected;
			public bool IsSelected {
				get => mIsSelected;
				set {
					if (value != mIsSelected) {
						mIsSelected = value;
						OnPropertyChanged();
					}
				}
			}

			private bool mIsHovered;
			public bool IsHovered {
				get => mIsHovered;
				set {
					if (value != mIsHovered) {
						mIsHovered = value;
						OnPropertyChanged();
					}
				}
			}

			private bool mIsHighlighted;
			public bool IsHighlighted {
				get => mIsHighlighted;
				set {
					if (value != mIsHighlighted) {
						mIsHighlighted = value;
						OnPropertyChanged();
					}
				}
			}

			private bool mIsKingInCheck;
			public bool IsKingInCheck {
				get => mIsKingInCheck;
				set {
					if (value != mIsKingInCheck) {
						mIsKingInCheck = value;
						OnPropertyChanged();
					}
				}
			}

			public ChessSquare(ChessPiece? piece, int index)
			{
				Piece = piece;
				Row = index / 8;
				Col = index % 8;
			}

            public event PropertyChangedEventHandler? PropertyChanged;

            // Call this helper method whenever a property changes the compiler will automatically detect the name of where  calling  from.
            private void OnPropertyChanged([CallerMemberName] string? name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        // Invoke this event after applying a move if the game is now finished.
        public event EventHandler? GameFinished;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Call this helper method whenever a property changes. The parameter is optional;
		// if you leave it off, the compiler will automatically detect the name of the property
		// based on where you are calling this method from.
		private void OnPropertyChanged([CallerMemberName]string? name = null) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

	}
}

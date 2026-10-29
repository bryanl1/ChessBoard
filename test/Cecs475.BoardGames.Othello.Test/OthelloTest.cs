using System;
using System.Collections.Generic;
using System.Text;
using Cecs475.BoardGames.Model;
using Cecs475.BoardGames.Othello.Model;
using Cecs475.BoardGames.Othello.ConsoleView;
using Cecs475.BoardGames.Test;

namespace Cecs475.BoardGames.Othello.Test {
	public class OthelloTest : BoardGameTest<OthelloBoard, OthelloMove, OthelloConsoleView> {

		public static OthelloMove Move(BoardPosition pos) {
			return new OthelloMove(pos);
		}

		public static OthelloMove Move(int row, int col) {
			return Move(Pos(row, col));
		}
	}
}

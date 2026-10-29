using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Cecs475.BoardGames.Model;
using Cecs475.BoardGames.Othello.Model;
using Xunit;

namespace Cecs475.BoardGames.Othello.Test {
	public class NewBoardTests : OthelloTest {
		[Fact]
		public void StartingPositions() {
			OthelloBoard b = new OthelloBoard();
			b.GetPlayerAtPosition(Pos(3, 3)).Should().Be(2);
			b.GetPlayerAtPosition(Pos(3, 4)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 3)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 4)).Should().Be(2);

			foreach (var pos in BoardPosition.GetRectangularPositions(8, 8)) {
				if (!((pos.Row == 3 || pos.Row == 4) && (pos.Col == 3 || pos.Col == 4))) {
					b.GetPlayerAtPosition(pos).Should().Be(0);
				}
			}
		}

		[Fact]
		public void StartingMoves() {
			OthelloBoard b = new OthelloBoard();
			var possMoves = b.GetPossibleMoves();
			possMoves.Should().HaveCount(4).And.BeEquivalentTo(
				[Move(2, 3), Move(3, 2), Move(4, 5), Move(5, 4)]
			);
		}
	}
}

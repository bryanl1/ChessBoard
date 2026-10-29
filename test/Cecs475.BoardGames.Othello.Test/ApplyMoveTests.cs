using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Cecs475.BoardGames.Othello.Model;
using Xunit;

namespace Cecs475.BoardGames.Othello.Test {
	public class ApplyMoveTests : OthelloTest {
		[Fact]
		public void OneDirectionOneFlip() {
			OthelloBoard b = new OthelloBoard();
			Apply(b, Move(3, 2));
			b.GetPlayerAtPosition(Pos(3, 2)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(3, 3)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(3, 4)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 3)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 4)).Should().Be(2);
		}

		[Fact]
		public void OneDirectionManyFlips() {
			OthelloBoard b = CreateBoardFromMoves(
				// These moves get packaged into an OthelloMove params array by the compiler. 
				Move(3, 2),
				Move(4, 2),
				Move(5, 2)
			);
			b.GetPlayerAtPosition(Pos(4, 2)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 3)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 4)).Should().Be(2);

			Apply(b, Move(4, 1));
			b.GetPlayerAtPosition(Pos(4, 1)).Should().Be(2);
			b.GetPlayerAtPosition(Pos(4, 2)).Should().Be(2);
			b.GetPlayerAtPosition(Pos(4, 3)).Should().Be(2);
			b.GetPlayerAtPosition(Pos(4, 4)).Should().Be(2);
		}

		[Fact]
		public void ManyDirectionsOneFlip() {
			OthelloBoard b = CreateBoardFromMoves(
				// These moves get packaged into an OthelloMove params array by the compiler. 
				Move(3, 2),
				Move(4, 2)
			);
			Apply(b, Move(5, 2));

			b.CurrentPlayer.Should().Be(2);
			b.GetPlayerAtPosition(Pos(5, 2)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 2)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(3, 2)).Should().Be(1);
			b.GetPlayerAtPosition(Pos(4, 3)).Should().Be(1);
		}
	}
}
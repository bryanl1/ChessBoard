using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Cecs475.BoardGames.Othello.Model;
using Xunit;

namespace Cecs475.BoardGames.Othello.Test {
	public class AdvantageTests : OthelloTest {
		[Fact]
		public void SingleFlipAdvantage() {
			OthelloBoard b = new OthelloBoard();
			Apply(b, Move(3, 2));
			b.CurrentAdvantage.Should().Be(Advantage(1, 3));
		}

		[Fact]
		public void UndoSingleFlip() {
			OthelloBoard b = CreateBoardFromMoves(Move(3, 2));
			b.CurrentAdvantage.Should().Be(Advantage(1, 3));
			b.UndoLastMove();
			b.CurrentAdvantage.Should().Be(Advantage(0, 0));
		}
	}
}

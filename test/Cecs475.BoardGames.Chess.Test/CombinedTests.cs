using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Chess.Test;
using Cecs475.BoardGames.Chess;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System;
using Xunit.Abstractions;
using Xunit;

public class CombinedTests : ChessTest
{
    [Fact]
    public void NewChessBoard()
    {
        //Validate the status of board when the game starts
        ChessBoard b = new ChessBoard();
        //player check
        b.GetPieceAtPosition(Pos("f8")).Player.Should().Be(2, "Player 2 should be at f8");
        b.GetPieceAtPosition(Pos("c2")).Player.Should().Be(1, "Player 1 should be at c2");
        b.GetPieceAtPosition(Pos("g5")).Player.Should().Be(0, "No piece should be at g5");
        //Piece check
        b.GetPieceAtPosition(Pos("d8")).PieceType.Should().Be(ChessPieceType.Queen, "Black Queen at d8");
        b.GetPieceAtPosition(Pos("f2")).PieceType.Should().Be(ChessPieceType.Pawn, "White Pawn at f2");
        b.GetPieceAtPosition(Pos("g8")).PieceType.Should().Be(ChessPieceType.Knight, "Black Knight at g8");
        //Other properties
        b.IsCheck.Should().BeFalse("No check at a start");
        b.IsDraw.Should().BeFalse("No draw at a start");
        b.IsFinished.Should().BeFalse("Not finished as game just started");
    }

    [Fact]
    //(031466030 mod 5) + 1 = 1 so test getpossible() move on a white and black rooks
    public void RookMoves()
    {
        //Validate the possible moves for black
        //and white rooks based on varying circumstances
        ChessBoard b = CreateBoardFromMoves("h2h4", "a7a5", "g2g3", "b7b6", "f1g2", "c7c6", "g1f3", "e7e5", "h4h5", "c6c5");
        var possMoves = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(possMoves, Pos("h1"));
        rookMoves.Should().HaveCount(5).And.BeEquivalentTo([Move("h1h4"), Move("h1h3"), Move("h1h2"), Move("h1f1"), Move("h1g1")], "Expect at L shaped possible moves for king side white rook from f1 to h4");
        //Making sure rook can attack but not jump over an enemy piece
        Apply(b, [Move("b2b3"), Move("a5a4"), Move("b3a4")]);
        possMoves = b.GetPossibleMoves();
        rookMoves = GetMovesAtPosition(possMoves, Pos("a8"));
        rookMoves.Should().Contain(Move("a8a4")).And.NotContain(Move("a8a3"), "Black Rook at a8 can attack an enemy piece but not jump over it");
    }

    [Fact]
    public void UndoCheckmate()
    {
        //Validate Checkmate's effects on the game
        //and validate undoing checkmate
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d6", "f1c4", "e7e6", "d1h5", "e6e5");
        b.IsCheckmate.Should().BeFalse("No checkmate at the moment");
        Apply(b, Move("h5f7"));
        //Validate checkmate flag is set to true when checkmate occurs
        b.IsCheckmate.Should().BeTrue("White queen moves f7, leaving the black king with no way to" + "get out of checkmate");
        b.IsFinished.Should().BeTrue("Checkmate marks the end of the game");
        //Validate previous state of board and flags are restored when undoing checkmate
        b.UndoLastMove();
        b.IsCheck.Should().BeFalse("Undoing the move remove the check status for black king");
        b.IsCheckmate.Should().BeFalse("Since Black king is out of check, Checkmate should be false");
        b.IsFinished.Should().BeFalse("IsFinished should be restored to false after undoing checkmate");
        b.GetPieceAtPosition(Pos("h5")).Should().Be(new ChessPiece(ChessPieceType.Queen, 1), "White Queen returned to h5");
        b.GetPieceAtPosition(Pos("f7")).Should().Be(new ChessPiece(ChessPieceType.Pawn, 2), "Black Pawn restored at f7");
    }

    [Fact]
    public void WhiteCastling()
    {
        //Validate Castling can be performed according to their rules
        ChessBoard b = CreateBoardFromMoves("g2g4", "b7b6", "f1g2", "c8a6", "g1f3", "c7c5");
        //Validating castling including effects on rook's new position
        var possMoves = b.GetPossibleMoves();
        var castlingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        castlingMoves.Should().BeEquivalentTo([Move("e1f1"), Move("e1g1")], "white king can move to f1 or to g1");
        Apply(b, Move("e1g1"));
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "white Rook moves to f1 automatically after white king goes to g1");
        //Undoing castling
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "White king moves back to e1 after undoing white castling");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "White rook moves also back to h1 after undoing white castling");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Empty, "Should be no piece at f1 after undoing");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.Empty, "Should be no piece at g1 after undoing");
        //When enemy can move to square between white king and white rook
        Apply(b, [Move("e2e3")]);
        possMoves = b.GetPossibleMoves();
        castlingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        castlingMoves.Should().BeEmpty("Cannot do white castling because enemy bishop can attack white king at f1 or e2");
    }

    [Fact]
    public void CheckKing()
    {
        //Validate effects on possible moves when check occurs
        //and that they all enable recovery from check status
        ChessBoard b = CreateBoardFromMoves("e2e4", "b7b6", "e1e2", "c8a6");
        //Sets white king to be in a position of check due to black bishop
        //Validating check flag and effects of possible moves after king put in check
        b.IsCheck.Should().BeTrue("Black bishop in a6 can attack white king at e2");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(5, "White king is in check. Player white can only do moves that will get" + "king out of check and there are only five possible moves on this board");
        foreach (var checkEscapeMove in possMoves)
        {
            //every possible moves should get white out of check
            Apply(b, checkEscapeMove);
            b.IsCheck.Should().BeFalse("Possible moves for White should get white king out of check");
            b.UndoLastMove();
            b.IsCheck.Should().BeTrue("Undoing escape move should return white king in check state");
        }
    }

    [Fact]
    public void BlackEnPassant()
    {
        //Validate en passant can be performed and that they can be undone
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "h2h3", "c5c4", "d2d4");
        var possMoves = b.GetPossibleMoves();
        var pawnMoves = GetMovesAtPosition(possMoves, Pos("c4"));
        //Validate en passant is possible
        pawnMoves.Should().Contain(Move("c4d3"), "White pawn did a one-time two square move, landing" + "next to a black pawn on the same rank and so Black can do an en passant for its turn");
        Apply(b, Move("c4d3"));
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "Black performed an en passant, capturing a white pawn at d4 and getting 1 point");
        b.GetPieceAtPosition(Pos("d4")).Should().Be(new ChessPiece(ChessPieceType.Empty, 0), "White pawn at d4 captured through en passant" + "and so there will be no piece at d4 after");
        //Validating undoing en passant
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("c4")).Should().Be(new ChessPiece(ChessPieceType.Pawn, 2), "Black Pawn returned to c4 after undoing");
        b.GetPieceAtPosition(Pos("d4")).Should().Be(new ChessPiece(ChessPieceType.Pawn, 1), "White Pawn restored to d4 after undoing en passant");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "Undoing the black en passant, restores the advantage to neutral");
        //When Black doesn't do en passant the turn after
        //White pawn lands next to a black pawn via two square move.
        Apply(b, [Move("f7f6"), Move("e4e5")]);
        possMoves = b.GetPossibleMoves();
        pawnMoves = GetMovesAtPosition(possMoves, Pos("c4"));
        pawnMoves.Should().NotContain(Move("c4d3"), "After missing the turn window for en passant, Black lost the oppurtunity do an en passant");
    }

    // check that number of some pieces per player is correct
    [Fact]
    public void StartingPieceCounts()
    {
        ChessBoard b = new ChessBoard();
        b.GetPositionsOfPiece(ChessPieceType.Pawn, player: 1).Should().HaveCount(8, "player 1 should have 8 pawns");
        b.GetPositionsOfPiece(ChessPieceType.Pawn, player: 2).Should().HaveCount(8, "player 2 should have 8 pawns");
        b.GetPositionsOfPiece(ChessPieceType.Rook, player: 1).Should().HaveCount(2, "player 1 should have 2 rooks");
        b.GetPositionsOfPiece(ChessPieceType.Rook, player: 2).Should().HaveCount(2, "player 2 should have 2 rooks");
        b.GetPositionsOfPiece(ChessPieceType.Bishop, player: 1).Should().HaveCount(2, "player 1 should have 2 bishops");
        b.GetPositionsOfPiece(ChessPieceType.Bishop, player: 2).Should().HaveCount(2, "player 2 should have 2 bishops");
        GetAllPiecesForPlayer(b, 1).Should().HaveCount(16, "player 1 should have 16 pieces");
        GetAllPiecesForPlayer(b, 2).Should().HaveCount(16, "player 2 should have 16 pieces");
    }

    // check that number of pieces has gone down after a capture and some other conditions
    [Fact]
    public void PieceCounts()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "b7b5");
        GetAllPiecesForPlayer(b, 1).Should().HaveCount(16, "player 1 should have 16 pieces");
        GetAllPiecesForPlayer(b, 2).Should().HaveCount(16, "player 2 should have 16 pieces");
        Apply(b, "a4b5");
        GetAllPiecesForPlayer(b, 1).Should().HaveCount(16, "player 1 should have 16 pieces");
        GetAllPiecesForPlayer(b, 2).Should().HaveCount(15, "player 2 should have 15 pieces now");
        b.GetPositionsOfPiece(ChessPieceType.Pawn, player: 2).Should().HaveCount(7, "player 2 should have 7 pawns");
        b.GetPlayerAtPosition(Pos("b5")).Should().Be(1, "player 1 attacked this position");
        b.CurrentPlayer.Should().Be(2, "player 1 just moved");
        b.UndoLastMove();
        GetAllPiecesForPlayer(b, 1).Should().HaveCount(16, "player 1 should have 16 pieces");
        GetAllPiecesForPlayer(b, 2).Should().HaveCount(16, "player 2 should have 16 pieces again");
        b.GetPositionsOfPiece(ChessPieceType.Pawn, player: 2).Should().HaveCount(8, "player 2 should have 8 pawns again");
        b.GetPlayerAtPosition(Pos("b5")).Should().Be(2, "player 2 came back to life");
        b.CurrentPlayer.Should().Be(1, "player 1's move was undone");
    }

    // checks conditions during check
    // checks that player 2 bishop has only 1 move and it removes check condition, but same piece has 5 moves when not in check
    [Fact]
    public void MovesWhenInCheck()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d6");
        b.ApplyMove(Move("f1b5"));
        b.IsCheck.Should().Be(true, "king is in check");
        var moves = b.GetPossibleMoves();
        moves.Should().HaveCount(5, "only a few moves are possible during check");
        GetMovesAtPosition(moves, Pos("c8")).Should().HaveCount(1).And.Contain(Move("c8d7"), "player 2 white bishop should only have 1 possible move during check");
        b.UndoLastMove();
        b.ApplyMove(Move("f1e2"));
        b.IsCheck.Should().Be(false, "king no longer in check");
        moves = b.GetPossibleMoves();
        GetMovesAtPosition(moves, Pos("c8")).Should().HaveCount(5).And.Contain(Move("c8h3"), "player 2 white bishop should have 5 possible moves");
    }

    // checks bishop possible moves when blocked (cant jump over pawn)
    // my id % 5 + 1 = 3
    [Fact]
    public void BishopPossibleMoves()
    {
        ChessBoard b = CreateBoardFromMoves("e2e3", "d7d5", "f1c4", "d8d6");
        var moves = b.GetPossibleMoves();
        GetMovesAtPosition(moves, Pos("c4")).Should().HaveCount(7).And.NotContain(Move("c4e6"), "bishop shouldn't be able to jump past pawn");
    }

    // castles player 2 kingside, checks if pieces are in the correct positions
    // also moves rook forward and back and checks that castling is no longer a possible move
    [Fact]
    public void Castling()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "g8f6", "d2d4", "e7e6", "c2c3", "f8e7", "f2f3");
        var castle = new ChessMove(Pos("e8"), Pos("g8"), ChessMoveType.CastleKingSide);
        b.ApplyMove(castle);
        b.GetPieceAtPosition(Pos("f8")).PieceType.Should().Be(ChessPieceType.Rook, "rook should be at f8 after castling");
        b.GetPieceAtPosition(Pos("g8")).PieceType.Should().Be(ChessPieceType.King, "king should be at g8 after castling");
        b.UndoLastMove();
        Apply(b, ["h7h5", "h2h4", "h8h6", "h1h3", "h6h8", "a2a3"]);
        GetMovesAtPosition(b.GetPossibleMoves(), Pos("e8")).Should().HaveCount(1).And.Contain(Move("e8f8"), "king should not be able to castle after rook moves");
    }

    // puts white king in check with no possible moves
    // checks that game finishes when checkmate condiiton is reached
    [Fact]
    public void Checkmate()
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e5", "g2g4", "d8h4");
        b.IsCheck.Should().BeTrue("white king is in check");
        b.GetPossibleMoves().Should().HaveCount(0, "player 1 has no moves left");
        b.IsFinished.Should().BeTrue("the game should be over because white is checkmated");
    }

    // 1) Initial board test: White has 20 legal opening moves
    [Fact]
    public void StartBoardWhiteHas20LegalMoves()
    {
        ChessBoard b = new ChessBoard();
        var moves = b.GetPossibleMoves().ToList();
        moves.Count.Should().Be(20, "the starting position should offer 20 legal moves for White");
        b.CurrentPlayer.Should().Be(1, "White always moves first");
    }

    // 2) Tricky move : White king-side castling
    [Fact]
    public void CastleWhiteKingSide()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "e2e4", // W: clear e2
 "a7a6", // B: making meaningless waiting moves(does not attack f1/g1) 
 "g1f3", // W: clear g1
 "a6a5", // B: 
 "f1e2", // W: clear f1
 "b8c6"); // B: still does not attack f1/g1
        // castling 0-0 should be possible
        var castle = Move("e1g1");
        var legal = b.GetPossibleMoves().Any(m => m.Equals(castle));
        legal.Should().BeTrue("Path is clear, neither square nor the king is under attack, king/rook");
        Apply(b, "e1g1");
        b.GetPlayerAtPosition(Pos("g1")).Should().Be(1, "after castling, the White king must be");
        b.GetPlayerAtPosition(Pos("f1")).Should().Be(1, "after castling, the White rook must be");
        b.GetPlayerAtPosition(Pos("h1")).Should().Be(0, "after castling, h1 should be empty");
        b.CurrentPlayer.Should().Be(2, "after White castles, it should be Black's turn");
    }

    //3) Undo #1: Simple move and undo
    [Fact]
    public void UndoSimpleMove()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "e2e4");
        b.CurrentPlayer.Should().Be(2, "after White plays, it should be Black's turn");
        b.GetPlayerAtPosition(Pos("e4")).Should().Be(1, "White pawn should be on e4");
        b.GetPlayerAtPosition(Pos("e2")).Should().Be(0, "e2 should be empty after pawn moved");
        var preHistory = b.MoveHistory.Count;
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(1, "after undo, it should be White's turn");
        b.GetPlayerAtPosition(Pos("e2")).Should().Be(1, "White pawn returns to e2 after undo");
        b.GetPlayerAtPosition(Pos("e4")).Should().Be(0, "e4 should be empty after undo");
        b.MoveHistory.Count.Should().BeLessThan(preHistory, "undo should reduce the move history count");
    }

    //4) Undo #2: Capture and undo
    [Fact]
    public void UndoCaptureByBlack()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "e2e4", // W
 "b8c6", // B
 "e4e5", // W
 "c6e5"); // B knight captures
        b.GetPlayerAtPosition(Pos("e5")).Should().Be(2, "Black knight should be on e5 after capture");
        var preHistory = b.MoveHistory.Count;
        b.UndoLastMove();
        b.GetPlayerAtPosition(Pos("c6")).Should().Be(2, "Black knight returns to c6 after undo");
        b.GetPlayerAtPosition(Pos("e5")).Should().Be(1, "White pawn is restored on e5 after undo");
        b.CurrentPlayer.Should().Be(2, "after undoing Black's capture, it should still be Black");
        b.MoveHistory.Count.Should().BeLessThan(preHistory, "undo should reduce the move history count");
    }

    // 5) Get possible moves: Verify that when the rook on a1 is blocked by its own pawn structure
    [Fact]
    public void RookAtA1MinimalMoves()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "a2a3", "h7h6"); // Clear a2 and block with a white pawn on a3 → rook at a1 can only go to a2
        var moves = b.GetPossibleMoves().ToList();
        // Filter only the moves starting from a1
        var fromA1 = GetMovesAtPosition(moves, Pos("a1")).ToList();
        // There should be exactly one move: a1 → a2
        fromA1.Count.Should().Be(1, "with rank blocked and a3 occupied by a white pawn, rook at a1 should only have a1→a2");
        fromA1.Should().Contain(m => m.EndPosition.Equals(Pos("a2")), "rook should be able to step from a1 to a2");
    }

    // 6) Check: verify Black's legal responses when in check
    [Fact]
    public void BlackInCheckResolveCheck()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "e2e4", // W
 "e7e5", // B
 "d1h5", // W
 "b8c6", // B
 "h5f7"); // W
        b.IsCheck.Should().BeTrue("after white queen takes the pawn on f7, Black's king should be in check");
        var moves = b.GetPossibleMoves().ToList();
        moves.Any(m => m.Equals(Move("a7a6"))).Should().BeFalse("When in check, Black caanot play irrelevant moves like a7a6");
        moves.Any(m => m.Equals(Move("e8f7"))).Should().BeTrue("kxf7 should be a legal move to resolve the check");
    }

    //test for initial board
    [Fact]
    public void InitialBoard()
    {
        //creates board
        ChessBoard b = new ChessBoard();
        //check if player 1 and 2 are on the right sides and the middle of the board is empty
        b.GetPlayerAtPosition(Pos("a1")).Should().Be(1, "Player 1 is on lower left of board");
        b.GetPlayerAtPosition(Pos("a8")).Should().Be(2, "Player 2 is on upper left of board");
        b.GetPlayerAtPosition(Pos("a4")).Should().Be(0, "The middle of the board has no pieces");
        //checks if queens are in the right place.
        b.GetPieceAtPosition(Pos("d1")).PieceType.Should().Be(ChessPieceType.Queen, "White queen is at position d1");
        b.GetPieceAtPosition(Pos("d8")).PieceType.Should().Be(ChessPieceType.Queen, "Black queen is at position d8");
        //checks if white starts
        b.CurrentPlayer.Should().Be(1, "Player 1 starts");
    }

    //test case for castling
    [Fact]
    public void Castling__From_bkhoang1272()
    {
        //creates board
        ChessBoard b = CreateBoardFromMoves("d2d3", "g8f6", "e2e4", "e7e5", "f1e2", "f8c5", "g1f3");
        //gets possible moves for king at e8
        var possMoves = b.GetPossibleMoves();
        var castlingExpected = GetMovesAtPosition(possMoves, Pos("e8"));
        castlingExpected.Should().HaveCount(3, "King can move to f8 or do castling").And.Contain(Move("e8f8")).And.Contain(Move("e8e7")).And.Contain(Move(Pos("e8"), Pos("g8"), ChessMoveType.CastleKingSide));
        //performs castling and confirms new positions of the pieces
        Apply(b, Move(Pos("e8"), Pos("g8"), ChessMoveType.CastleKingSide));
        var king = b.GetPieceAtPosition(Pos("g8"));
        king.Player.Should().Be(2, "king performed castling");
        king.PieceType.Should().Be(ChessPieceType.King);
        var rook = b.GetPieceAtPosition(Pos("f8"));
        rook.Player.Should().Be(2, "rook performed castling");
        rook.PieceType.Should().Be(ChessPieceType.Rook);
    }

    //checks the movement logic of the rook
    [Fact]
    public void ValidRookMove()
    {
        ChessBoard b = CreateBoardFromMoves("h2h4", "a7a5", "h1h3", "b7b5", "h3b3", "a5a4", "e2e3", "b5b4");
        var possMoves = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(possMoves, Pos("b3"));
        rookMoves.Should().HaveCount(4, "Rook can move to 3 places or capture pawn").And.Contain(Move("b3a3")).And.Contain(Move("b3b4")).And.Contain(Move("b3d3")).And.Contain(Move("b3c3"));
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
        //rook captures pawn
        Apply(b, Move("b3b4"));
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(1, "Player 1 captured player 2's pawn");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "Black lost pawn of 1 value");
        //undos last move and checks advantage and piece positions
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("b3")).PieceType.Should().Be(ChessPieceType.Rook, "piece should be rook");
        b.GetPieceAtPosition(Pos("b4")).PieceType.Should().Be(ChessPieceType.Pawn, "piece should be pawn");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "advantage hasn't changed after undoing pawn capture");
    }

    //checks if pawns can do diagonal capture
    [Fact]
    public void PawnDiagonalCapture()
    {
        ChessBoard b = CreateBoardFromMoves("b2b4", "b7b5", "c2c3", "c7c5");
        var possMoves = b.GetPossibleMoves();
        var pawnMoves = GetMovesAtPosition(possMoves, Pos("b4"));
        pawnMoves.Should().HaveCount(1, "Pawn can capture diagonally").And.Contain(Move("b4c5"));
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
        //pawn captures pawn
        Apply(b, Move("b4c5"));
        b.GetPieceAtPosition(Pos("c5")).Player.Should().Be(1, "Player 1 captured player 2's pawn");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "Black lost pawn of 1 value");
    }

    //checks movement logic of queen
    [Fact]
    public void ValidQueenMove()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "d7d5", "a4a5", "d8d7", "a1a4");
        var possMoves = b.GetPossibleMoves();
        var queenMoves = GetMovesAtPosition(possMoves, Pos("d7"));
        queenMoves.Should().HaveCount(9, "Queen can move 8 places or capture rook").And.Contain(Move("d7d8")).And.Contain(Move("d7d6")).And.Contain(Move("d7c6")).And.Contain(Move("d7b5")).And.Contain(Move("d7a4")).And.Contain(Move("d7e6")).And.Contain(Move("d7f5")).And.Contain(Move("d7g4")).And.Contain(Move("d7h3"));
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
        //queen captures rook
        Apply(b, Move("d7a4"));
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(2, "Player 2 captured player 1's rook");
        b.CurrentAdvantage.Should().Be(Advantage(2, 5), "White lost rook");
        //undos the last move and checks advantage and positions of the pieces
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("d7")).PieceType.Should().Be(ChessPieceType.Queen, "piece should be queen");
        b.GetPieceAtPosition(Pos("a4")).PieceType.Should().Be(ChessPieceType.Rook, "piece should be rook");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "advantage hasn't changed after undoing pawn capture");
    }

    //places the king in check and shows moves to cancel it
    [Fact]
    public void ValidCheck()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "f1c4", "g8f6", "d2d3", "f8b4");
        var possMoves = b.GetPossibleMoves();
        possMoves.Count().Should().Be(7, "There should be 7 possible moves to cancel checking of white king");
        possMoves.Should().BeEquivalentTo([Move("e1f1"), Move("e1e2"), Move("d1d2"), Move("c2c3"), Move("c1d2"), Move("b1c3"), Move("b1d2")], "the king can't move into check");
    }

    /// <summary>
    /// On a new board, White should start with 20 legal moves and no game end conditions.
    /// </summary>
    [Fact]
    public void OpeningMoveCount_WhiteStartsWith20Moves()
    {
        // Arrange: create a new board
        ChessBoard b = new ChessBoard();
        // Act: get all legal moves from the start position
        var moves = b.GetPossibleMoves();
        // Assert: White has 20 possible moves (16 pawn pushes + 4 knight moves)
        moves.Count().Should().Be(20, "White should have 20 legal moves at the start (16 pawn moves + 4 knight moves)");
        // White is always the starting player
        b.CurrentPlayer.Should().Be(1, "White moves first in chess");
        // No end-game flags should be active on a new board
        b.IsCheck.Should().BeFalse("no check at the start");
        b.IsCheckmate.Should().BeFalse("no checkmate at the start");
        b.IsStalemate.Should().BeFalse("no stalemate at the start");
        b.IsDraw.Should().BeFalse("no draw at the start");
        b.IsFinished.Should().BeFalse("game should not be finished at the start");
    }

    /// <summary>
    /// White performs en passant and undo restores the board and turn.
    /// </summary>
    [Fact]
    public void EnPassant_ExecuteAndUndo()
    {
        // Arrange: build a position where white can perform en passant
        ChessBoard b = CreateBoardFromMoves("e2e4", "a7a6", "e4e5", "d7d5");
        // Act: perform en passant (white pawn from e5 takes black pawn on d5)
        Apply(b, Move(Pos("e5"), Pos("d6"), ChessMoveType.EnPassant));
        // Assert: board after en passant
        b.GetPieceAtPosition(Pos("d6")).PieceType.Should().Be(ChessPieceType.Pawn, "white pawn should land on d6 after en passant");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(1, "the pawn on d6 should belong to white");
        b.GetPieceAtPosition(Pos("d5")).Player.Should().Be(0, "black pawn must be removed from d5 after en passant");
        b.CurrentPlayer.Should().Be(2, "turn should flip to black after white's capture");
        // Act: undo the move
        b.UndoLastMove();
        // Assert: board restored correctly
        b.GetPieceAtPosition(Pos("e5")).Player.Should().Be(1, "white pawn should be restored to e5 after undo");
        b.GetPieceAtPosition(Pos("d5")).Player.Should().Be(2, "black pawn should be restored to d5 after undo");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(0, "d6 should be empty after undo");
        b.CurrentPlayer.Should().Be(1, "undo should restore the turn to white");
    }

    /// <summary>
    /// Black castles kingside. undo should fully restore king/rook and turn.
    /// </summary>
    [Fact]
    public void BlackCastlingWithUndo()
    {
        // Arrange: clear g8 and f8 for black in a legal order (no fancy tactics)
        ChessBoard b = CreateBoardFromMoves("g2g3", "g8f6", // free g8 by moving the knight
 "b2b3", "g7g6", // clear g7 so the f8 bishop can move out
 "c2c3", "f8g7", // slide the bishop off f8
 "d2d3", "e8g8" // perform black kingside castling
        );
        // Assert: after castling, king goes to g8 and rook from h8 goes to f8
        b.GetPieceAtPosition(Pos("g8")).PieceType.Should().Be(ChessPieceType.King, "black king should be on g8 after castling");
        b.GetPieceAtPosition(Pos("g8")).Player.Should().Be(2, "the king on g8 should belong to black");
        b.GetPieceAtPosition(Pos("f8")).PieceType.Should().Be(ChessPieceType.Rook, "black rook should be on f8 after castling");
        b.GetPieceAtPosition(Pos("f8")).Player.Should().Be(2, "the rook on f8 should belong to black");
        b.IsCheck.Should().BeFalse("castling cannot move through or into check");
        // Act: undo the castle
        b.UndoLastMove();
        // Assert: king and rook return to starting squares; turn restored
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "undo should restore the king to e8");
        b.GetPieceAtPosition(Pos("h8")).PieceType.Should().Be(ChessPieceType.Rook, "undo should restore the rook to h8");
        b.CurrentPlayer.Should().Be(2, "after undoing black's move, it should be black to move again");
    }

    /// <summary>
    /// Black queen on d5 should report exactly the legal ray moves (no extras).
    /// This stresses GetPossibleMoves for the queen.
    /// </summary>
    [Fact]
    public void QueenMovesOnOpenBoard()
    {
        // Arrange: free the center and park the black queen on d5.
        // End with a quiet white move so it's Black to move.
        ChessBoard b = CreateBoardFromMoves("d2d4", "d7d5", "c2c4", "c7c6", "b1c3", "d5c4", "e2e3", "d8d5", "h2h3" // ensures it's Black to move (queen's turn)
        );
        var qMoves = GetMovesAtPosition(b.GetPossibleMoves(), Pos("d5"));
        // Expected destinations (15 total) with c6/f7/c4 blocking certain rays.
        var expected = new[]
        {
            Move("d5e5"),
            Move("d5f5"),
            Move("d5g5"),
            Move("d5h5"),
            Move("d5c5"),
            Move("d5b5"),
            Move("d5a5"),
            Move("d5d6"),
            Move("d5d7"),
            Move("d5d8"),
            Move("d5d4"), // capture on d4
            Move("d5e6"), // NE until f7 (blocked)
            Move("d5e4"),
            Move("d5f3"),
            Move("d5g2") // SE, g2 is a capture
        };
        qMoves.Should().HaveCount(15, "from d5 the queen has 15 legal targets with c6/f7 blocking diagonals and c4 occupied by own pawn").And.BeEquivalentTo(expected, "queen should list exactly the ray moves allowed by current blockers");
        // Explicit negatives
        qMoves.Should().NotContain(Move("d5c6"), "own pawn on c6 blocks the NW diagonal");
        qMoves.Should().NotContain(Move("d5f7"), "own pawn on f7 blocks the NE diagonal");
        qMoves.Should().NotContain(Move("d5c4"), "own pawn on c4 blocks the SW diagonal");
    }

    /// <summary>
    /// White is in check from Black's queen on e5; only moves that resolve the check are legal.
    /// </summary>
    [Fact]
    public void WhiteKingInCheck_OnlyLegalResponses()
    {
        // Arrange: create a clean e-file check on the white king (e1).
        // After the last move it's White to move and in check from Qe5.
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d5", "e4d5", "d8d5", "b1c3", "d5e5" // black queen gives check on e5
        );
        // Assert: White (the side to move) is in check.
        b.IsCheck.Should().BeTrue("white should be in check from the black queen on e5 along the e-file");
        // Act: get White's legal moves.
        var legal = b.GetPossibleMoves();
        // These blocks should be present (each interposes on the e-file).
        legal.Should().Contain(Move("d1e2"), "white can block the e-file check by moving the queen to e2");
        legal.Should().Contain(Move("f1e2"), "white can block the e-file check by moving the bishop to e2");
        legal.Should().Contain(Move("c3e2"), "white can block the e-file check by moving the knight from c3 to e2");
        // These should NOT be present (do not resolve the check / illegal).
        legal.Should().NotContain(Move("a2a3"), "irrelevant pawn push does not resolve the check and must be illegal");
        legal.Should().NotContain(Move("e1e2"), "the king cannot move to e2 because that square is attacked by the checking queen");
    }

    /// <summary>
    /// After a black pawn captures to b3, verify its legal forward/diagonal moves
    /// (and that it has no backwards/sideways moves).
    /// </summary>
    [Fact]
    public void BlackPawnOnB3_HasOnlyForwardAndDiagonalCaptures()
    {
        // Arrange: set up a4xb3 (black pawn captures from a4 to b3).
        // Then make a quiet White move so it's Black to move (to query the pawn's moves).
        ChessBoard b = CreateBoardFromMoves("a2a3", "a7a5", "h2h3", "a5a4", "b2b3", "a4b3", "g2g3" // hand turn to Black
        );
        // piece locations after the capture
        b.GetPieceAtPosition(Pos("b3")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn should be on b3 after capturing from a4");
        b.GetPieceAtPosition(Pos("b3")).Player.Should().Be(2, "the pawn on b3 should belong to Black");
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(0, "a4 should be empty after the capture");
        // Act: get Black's legal moves that start from b3
        var moves = GetMovesAtPosition(b.GetPossibleMoves(), Pos("b3"));
        // Assert: forward one step to b2 is allowed (empty square)
        moves.Should().Contain(Move("b3b2"), "a black pawn on b3 can advance one square forward to b2 if it is empty");
        // Assert: capture to c2 is allowed (white pawn still sits on c2)
        moves.Should().Contain(Move("b3c2"), "a black pawn on b3 can capture diagonally forward-right if an enemy piece is on c2");
        // Assert: no backward or sideways movement
        moves.Should().NotContain(Move("b3b4"), "pawns cannot move backwards");
        moves.Should().NotContain(Move("b3a3"), "pawns cannot move sideways");
        // Assert: no diagonal capture to a2 because that square is empty now (a2→a3 earlier)
        moves.Should().NotContain(Move("b3a2"), "a diagonal capture is only legal if an enemy piece is on the target square");
        // Assert: no two-square advance from mid-board
        moves.Should().NotContain(Move("b3b1"), "a pawn can only move two squares from its original rank");
    }

    //At most 1 test can be about the initial starting board state. (We don't need 100 test cases that count how many pawns are on the starting board.)
    [Fact]
    public void BlackUnitsPositions()
    {
        // tests if the location of the black player's units are correct
        var board = new ChessBoard();
        //get the positions of some pieces
        var blackRooks = board.GetPositionsOfPiece(ChessPieceType.Rook, 2);
        var blackQueen = board.GetPositionsOfPiece(ChessPieceType.Queen, 2);
        var blackKing = board.GetPositionsOfPiece(ChessPieceType.King, 2);
        //check if they are in the correct postions
        blackRooks.Should().Contain(Pos("a8"), "Black rook should be at a8");
        blackRooks.Should().Contain(Pos("h8"), "Black rook should be at h8");
        blackQueen.Should().Contain(Pos("d8"), "Black queen should be at d8");
        blackKing.Should().Contain(Pos("e8"), "Black king should be at e8");
    }

    //At least 1 test must involve a tricky move, like castling, pawn promotion, or en passant. (It's easy to mess these up.)
    [Fact]
    public void EnPassant_Test()
    // tests if a white pawn can perform an en passant
    {
        //setup pawn moves so that two are adjacent
        var board = CreateBoardFromMoves("f2f4", "e7e5", "f4f5", "g7g5");
        //check if the pawn can do a en passant capture on the other pawn
        var moves = board.GetPossibleMoves();
        var whitePawnMoves = GetMovesAtPosition(moves, Pos("f5"));
        //if it can capture then the en passant move can succeed
        whitePawnMoves.Should().Contain(Move("f5g6"), "White should be able to capture black pawn via en passant and move to g6");
        //if the pawn successfully moves from column f to g then it en passants
        Apply(board, "f5g6");
        var whitePawns = board.GetPositionsOfPiece(ChessPieceType.Pawn, 1);
        whitePawns.Should().Contain(Pos("g6"), "white pawn should be at g6");
    }

    [Fact]
    public void CastleTest()
    //the black side performs a castle
    { //in this setup the king moves from e8 to g8(two tiles away) this can only happen in a castle
        var board = CreateBoardFromMoves("h2h3", "e7e5", "a2a3", "f8d6", "b2b4", "g8h6", "f2f3", "e8g8");
        var blackRooks = board.GetPositionsOfPiece(ChessPieceType.Rook, 2);
        var blackKings = board.GetPositionsOfPiece(ChessPieceType.King, 2);
        // i never moved the rook from h8, so if it moved, then its because it castled when i moved the king
        blackRooks.Should().NotContain(Pos("h8"), "Black rook should not be at h8");
        blackRooks.Should().Contain(Pos("f8"), "Black rook should be at f8");
        blackKings.Should().Contain(Pos("g8"), "Black king should be at g8");
    }

    //At least 2 tests must involve UndoLastMove(). (Check many aspects of the board after the move is undone.)
    [Fact]
    public void UndoTest1()
    {
        //undoing the en passant from the previous en passant test
        var board = CreateBoardFromMoves("f2f4", "e7e5", "f4f5", "g7g5", "f5g6");
        board.UndoLastMove();
        //if the en passant occurs then white pawn is at g6 and black pawn at g5 is captured
        // check if the black g5 pawn is there after the undo
        var blackPawns = board.GetPositionsOfPiece(ChessPieceType.Pawn, 2);
        blackPawns.Should().Contain(Pos("g5"), "Black pawn should be at g5");
        // check if the white pawn is back at f5 
        var whitePawns = board.GetPositionsOfPiece(ChessPieceType.Pawn, 1);
        whitePawns.Should().Contain(Pos("f5"), "white pawn should be at f5");
        //check if the white pawn can do the en passant again
        var moves = board.GetPossibleMoves();
        var whitePawnMoves = GetMovesAtPosition(moves, Pos("f5"));
        whitePawnMoves.Should().Contain(Move("f5g6"), "White should be able to capture black pawn via en passant on g6");
    }

    [Fact]
    public void UndoTest2()
    { // just a bunch of randoms moves, but at c3d5 a white knight captures black pawn, and at e6d5 a pawn captures that knight
        var board = CreateBoardFromMoves("e2e4", "e7e6", "d2d4", "d7d5", "b1c3", "c7c5", "c3d5", "e6d5");
        board.UndoLastMove();
        //undo the black pawn capturing the white knight
        //check to see if the white knight is back
        var whiteKnights = board.GetPositionsOfPiece(ChessPieceType.Knight, 1);
        whiteKnights.Should().Contain(Pos("d5"), "white knight should be at d5");
        //check to see if the black pawn moved back to e6 after the undo
        var blackPawns = board.GetPositionsOfPiece(ChessPieceType.Pawn, 2);
        blackPawns.Should().Contain(Pos("e6"), "black pawn should be at e6");
    }

    //One test must involve validating GetPossibleMoves() to see that a particular piece has correctly reported all its possible moves.
    //(26664324 mod 5) + 1 = 5, so test kings
    // this test also duel fufills the condtion for a check test
    [Fact]
    public void CheckAndKingPossibleMoves()
    { // "e2e4", "e7e6", "d2d4", "d7d5","h2h3" "c2c4" pawns move forward, "d8d6" and "d6b4" black queen moves
        // this moveset uses a black queen to put the white king in check
        var board = CreateBoardFromMoves("e2e4", "e7e6", "d2d4", "d7d5", "h2h3", "d8d6", "c2c4", "d6b4");
        //confirm king is in check
        bool check = board.IsCheck;
        check.Should().BeTrue();
        var moves = board.GetPossibleMoves();
        moves.Count().Should().Be(5, "There should be 5 possible moves to cancel the checking of white's king");
        var whiteKingMoves = GetMovesAtPosition(moves, Pos("e1"));
        // the black queen blocks d2 from the king, so the only move the king can make is e2
        whiteKingMoves.Should().Contain(Move("e1e2"), "White should be able to move e2");
        whiteKingMoves.Should().NotContain(Move("e1d2"), "White shouldn't be able to move d2");
        // the white player's queen,knight and bishop can also block the queens path
        var whiteKnightMoves = GetMovesAtPosition(moves, Pos("b1"));
        whiteKnightMoves.Should().Contain(Move("b1d2"), "White knight should be able to move d2");
        whiteKnightMoves.Should().Contain(Move("b1c3"), "White knight should be able to move c3");
        var whiteBishopMoves = GetMovesAtPosition(moves, Pos("c1"));
        whiteBishopMoves.Should().Contain(Move("c1d2"), "White bishop should be able to move d2");
        var whiteQueenMoves = GetMovesAtPosition(moves, Pos("d1"));
        whiteQueenMoves.Should().Contain(Move("d1d2"), "White queen should be able to move d2");
    }

    [Fact]
    // Summary:
    // Verifies that the starting board has White to move and that all the White pawns are correctly placed on rank 2.
    public void StartingBoard_WhiteToMove_AndWhitePawnsOnRank2()
    {
        ChessBoard b = new ChessBoard();
        b.CurrentPlayer.Should().Be(1, "the game should start with Player 1 (White) to move");
        foreach (var file in new[]
        {
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h"
        }

        )
        {
            var pos = Pos(file + "2");
            var piece = b.GetPieceAtPosition(pos);
            piece.PieceType.Should().Be(ChessPieceType.Pawn, $"expected a pawn at {file}2");
            piece.Player.Should().Be(1, $"pawn at {file}2 should belong to White (Player 1)");
        }
    }

    [Fact]
    // Summary:
    // Verifies that the black rook on a8 has no legal moves at the start of the game because its blocked by its own pawn at a7 and knight at b8.
    public void RookFromA8_HasNoLegalMoves_WhenBlockedByPawnAndKnight()
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "h2h3");
        var moves = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(moves, Pos("a8"));
        rookMoves.Should().BeEmpty("black rook at a8 cannot move because it is blocked by the pawn at a7 and knight at b8");
    }

    [Fact]
    // Summary:
    // Verify that undoing Blacks pawn advance from e7 to e5 restores the board to the original state it used to be in.
    public void UndoLastMove_RestoresBoardAfterBlackPawnAdvance()
    {
        ChessBoard b = CreateBoardFromMoves("a2a3", "e7e5");
        b.GetPieceAtPosition(Pos("e5")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn should be on e5 after moving");
        b.GetPieceAtPosition(Pos("e7")).Player.Should().Be(0, "e7 should be empty after pawn moved");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e7")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn should return to e7 after undo");
        b.GetPieceAtPosition(Pos("e5")).Player.Should().Be(0, "e5 should be empty after undo");
        b.CurrentPlayer.Should().Be(2, "after undoing Black's move it is Black to move again");
    }

    [Fact]
    // Summary:
    // Verify that undoing a White pawn capture (e4xd5) restores both pawns to their original positions.
    public void UndoLastMove_RestoresBoardAfterCapture()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d5", "e4d5");
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Pawn, "white pawn should be on d5 after capturing");
        b.GetPieceAtPosition(Pos("d5")).Player.Should().Be(1, "the pawn on d5 should belong to White after capture");
        b.GetPieceAtPosition(Pos("e4")).Player.Should().Be(0, "e4 should be empty after the pawn moved");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn should return to d5 after undo");
        b.GetPieceAtPosition(Pos("d5")).Player.Should().Be(2, "the pawn at d5 should belong to Black after undo");
        b.GetPieceAtPosition(Pos("e4")).PieceType.Should().Be(ChessPieceType.Pawn, "white pawn should return to e4 after undo");
        b.GetPieceAtPosition(Pos("e4")).Player.Should().Be(1, "the pawn at e4 should belong to White after undo");
    }

    [Fact]
    // Summary:
    // Verify that White can legally castle kingside when the path is clear and the king and rook move correctly.
    public void WhiteCanCastleKingside()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "a7a6", "g1f3", "a6a5", "f1e2", "a5a4");
        var moves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(moves, Pos("e1"));
        kingMoves.Should().Contain(Move("e1g1"), "white king should be able to castle kingside when path is clear and it's White's turn");
        Apply(b, "e1g1");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "king should end up on g1 after castling");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "rook should move from h1 to f1 after castling");
    }

    [Fact]
    // Summary:
    // Verify that when the Black king is in check from the White queen, only moves that can fix the check are legal.
    public void BlackKing_InCheck_FromWhiteQueen_MovesMustResolveCheck()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d1h5", "b8c6", "h5f7");
        b.IsCheck.Should().BeTrue("black king should be in check from the white queen on f7");
        var moves = b.GetPossibleMoves();
        moves.Should().NotContain(Move("a7a6"), "when in check, Black cannot ignore the check and push a pawn");
        moves.Should().Contain(Move("e8f7"), "Black king should be able to capture the white queen on f7 to resolve the check");
    }

    [Fact]
    public void CheckNewBoard()
    {
        ChessBoard b = new ChessBoard();
        b.GetPieceAtPosition(Pos("a8")).PieceType.Should().Be(ChessPieceType.Rook, "The a8 piece should be a rook.");
        b.GetPlayerAtPosition(Pos("b7")).Should().Be(2, "This is a black piece.");
    }

    [Fact]
    public void CheckPawnPromotion()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "g7g5", "b7a8rook", "g5g4");
        var possMoves = b.GetPossibleMoves();
        b.GetPieceAtPosition(Pos("a8")).PieceType.Should().Be(ChessPieceType.Rook, "This pawn should be a rook now.");
        GetMovesAtPosition(possMoves, Pos("a8")).Should().BeEquivalentTo([Move("a8a7"), Move("a8b8")], "This pawn should be a rook now.");
    }

    [Fact]
    public void CheckRookMoves()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", "a1a3", "b7b5", "a3b3", "b5b4");
        var possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("b3")).Should().BeEquivalentTo([Move("b3a3"), Move("b3c3"), Move("b3d3"), Move("b3e3"), Move("b3f3"), Move("b3g3"), Move("b3h3"), Move("b3b4")], "The Rook should be able to move to these spots.");
    }

    //White, Undo
    [Fact]
    public void UndoRookMoves()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", "a1a3", "h5h4", "a3b3", "h4h3", "b3b6");
        var possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("b7")).Count().Should().Be(0, "The Rook Blocks the pawn so it cannot move.");
        GetMovesAtPosition(possMoves, Pos("a7")).Should().BeEquivalentTo([Move("a7a5"), Move("a7a6"), Move("a7b6")], "The pawn can take the rook or do other moves.");
        b.UndoLastMove();
        b.ApplyMove(Move("f2f4"));
        possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("b7")).Count().Should().Be(2, "The Rook no longer blocsk the pawn, so it can move.");
        GetMovesAtPosition(possMoves, Pos("a7")).Should().BeEquivalentTo([Move("a7a5"), Move("a7a6")], "The pawn can no longer take the rook.");
    }

    //Black, Undo
    [Fact]
    public void UndoKnightMove()
    {
        ChessBoard b = CreateBoardFromMoves("b2b3", "b8a6", "c2c3", "a6b4");
        var possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("b3")).Count().Should().Be(0, "The b2 pawn is blocked by the black knight, it has no moves.");
        GetMovesAtPosition(possMoves, Pos("c3")).Should().BeEquivalentTo([Move("c3c4"), Move("c3b4")], "The c3 pawn can either take the knight or move forward.");
        b.UndoLastMove();
        //This pawn move is just to change turns so that we can check white's moves
        b.ApplyMove(Move("g7g5"));
        possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("b3")).Should().BeEquivalentTo([Move("b3b4")], "The pawn should be able to move again now that the knight isn't there.");
        GetMovesAtPosition(possMoves, Pos("c3")).Should().BeEquivalentTo([Move("c3c4")], "The pawn can no longer move diagonally because the knight isn't there.");
    }

    //Black, Check
    [Fact]
    public void FoolsMate()
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e5", "g2g4", "d8h4");
        var possible = b.GetPossibleMoves();
        possible.Count().Should().Be(0, "There are no moves to save white as white's King has been checkmated.");
    }

    [Fact]
    public void TestCheckInitialisationBoard_Black()
    {
        // Purpose: verify initial black back rank and pawns
        ChessBoard b = new ChessBoard();
        // Check Pawns
        for (int i = 0; i < 8; i++)
        {
            b.GetPieceAtPosition(Pos(1, i)).PieceType.Should().Be(ChessPieceType.Pawn, "All pieces in row 1 should be pawns");
        }

        // Check Rooks
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Rook, "Piece at (0,0) should be a rook");
        b.GetPieceAtPosition(Pos(0, 7)).PieceType.Should().Be(ChessPieceType.Rook, "Piece at (0,7) should be a rook");
        // Check Knights
        b.GetPieceAtPosition(Pos(0, 1)).PieceType.Should().Be(ChessPieceType.Knight, "Piece at (0,1) should be a knight");
        b.GetPieceAtPosition(Pos(0, 6)).PieceType.Should().Be(ChessPieceType.Knight, "Piece at (0,6) should be a knight");
        // Check Bishops
        b.GetPieceAtPosition(Pos(0, 2)).PieceType.Should().Be(ChessPieceType.Bishop, "Piece at (0,2) should be a bishop");
        b.GetPieceAtPosition(Pos(0, 5)).PieceType.Should().Be(ChessPieceType.Bishop, "Piece at (0,5) should be a bishop");
        // Check Queen
        b.GetPieceAtPosition(Pos(0, 3)).PieceType.Should().Be(ChessPieceType.Queen, "Piece at (0,3) should be a queen");
        // Check King
        b.GetPieceAtPosition(Pos(0, 4)).PieceType.Should().Be(ChessPieceType.King, "Piece at (0,4) should be a king");
    }

    [Fact]
    public void TestPawnPromotion_White()
    {
        // Purpose: white pawn promotes to a queen
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", // W
 "a4a5", "h5h4", // B
 "a5a6", "h4h3", // W
 "a6b7", "g7g5" // B
        );
        Apply(b, "b7c8queen"); // W
        b.GetPieceAtPosition(Pos("c8")).PieceType.Should().Be(ChessPieceType.Queen, "Pawn should promote to Queen by default");
    }

    [Fact]
    public void TestUndoPromotionTest_White()
    {
        // Purpose: undo a white promotion and restore captured piece
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", // W
 "a4a5", "h5h4", // B
 "a5a6", "h4h3", // W
 "a6b7", "g7g5", // B
 "b7c8queen" // W
        );
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("b7")).PieceType.Should().Be(ChessPieceType.Pawn, "After undo, piece at b7 should be a pawn");
        b.GetPieceAtPosition(Pos("c8")).PieceType.Should().Be(ChessPieceType.Bishop, "After undo, piece at c8 should be a Bishop");
    }

    [Fact]
    public void TestQueenPossibleMovesQueenBlocked_Black()
    {
        // Purpose: black queen on d8 has zero legal moves (blocked by own pieces)
        var b = CreateBoardFromMoves("a2a3", // W
 "h7h6", // B 
 "a3a4" // W 
        );
        var blackMoves = b.GetPossibleMoves(); // B
        var queenMoves = GetMovesAtPosition(blackMoves, Pos("d8")).ToList();
        queenMoves.Should().HaveCount(0, "Black queen on d8 is blocked by its own pieces at the start (c7, d7, e7 and minor pieces)");
        queenMoves.Should().NotContain(Move("d8d7"), "Queen cannot move onto its own pawn on d7");
        queenMoves.Should().NotContain(Move("d8e7"), "Queen cannot move onto its own pawn on e7");
        queenMoves.Should().NotContain(Move("d8c7"), "Queen cannot move onto its own pawn on c7");
    }

    [Fact]
    public void TestCheckmate_NoMoreMoves_Black()
    {
        // Purpose: black is checkmated and has no legal moves
        var b = CreateBoardFromMoves("e2e4", // W
 "e7e5", // B
 "d1h5", // W
 "g8f6", // B
 "f1c4", // W
 "b8c6", // B
 "h5f7" // W
        );
        b.GetPossibleMoves().Should().BeEmpty("After 1.e4 e5 2.Qh5 Nf6 3.Bc4 Nc6 4.Qxf7#, Black is checkmated and has no legal moves");
    }

    [Fact]
    public void TestWhiteKingsideCastling_White()
    {
        // Purpose: verify white kingside castling and its undo
        var b = CreateBoardFromMoves("e2e4", "a7a6", // W B  
 "f1e2", "a6a5", // W B  
 "g1f3", "a5a4" // W B  
        );
        var whiteMoves = b.GetPossibleMoves();
        whiteMoves.Should().Contain(Move("e1g1"), "Kingside castling should be generated as a legal move when conditions are met");
        // Apply castling
        Apply(b, "e1g1"); // W
        // Post-conditions: king and rook relocated; original squares empty
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "After castling kingside, the white king should be on g1");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "After castling kingside, the white rook should be on f1");
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.Empty, "The original square e1 should be empty after castling");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Empty, "The original square h1 should be empty after castling");
        // Undo castling and verify restoration
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "After undo, the white king should return to e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "After undo, the white rook should return to h1");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Empty, "After undo, f1 should be empty again");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.Empty, "After undo, g1 should be empty again");
    }

    /// <summary>
    /// Kingside Castling for White.
    /// </summary>
    [Fact]
    public void KingsideCastlingTest()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "a7a6", "f1c4", "b7b5", "c4b3", "e7e5", "d2d3", "g8f6", "g1f3", "f8c5");
        var possMoves = b.GetPossibleMoves();
        var castlingExpected = GetMovesAtPosition(possMoves, Pos("e1"));
        castlingExpected.Should().HaveCount(4, "there should be 4 possible moves for White's king").And.Contain([Move("e1e2"), Move("e1d2"), Move("e1f1")]).And.Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        // Apply castling
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        var king = b.GetPieceAtPosition(Pos("g1"));
        var rook = b.GetPieceAtPosition(Pos("f1"));
        king.Player.Should().Be(1, "a white piece performed castling move");
        rook.Player.Should().Be(1, "a white piece performed castling move");
        king.PieceType.Should().Be(ChessPieceType.King, "a king performed castling move");
        rook.PieceType.Should().Be(ChessPieceType.Rook, "a rook performed castling move");
        // Undo the move and check the board state
        b.UndoLastMove();
        king = b.GetPieceAtPosition(Pos("e1"));
        rook = b.GetPieceAtPosition(Pos("h1"));
        king.Player.Should().Be(1, "a white piece was returned to e1");
        rook.Player.Should().Be(1, "a white piece was returned to h1");
        var empty1 = b.GetPieceAtPosition(Pos("g1"));
        empty1.Player.Should().Be(0, "there is no piece at g1");
        var empty2 = b.GetPieceAtPosition(Pos("f1"));
        empty2.Player.Should().Be(0, "there is no piece at f1");
    }

    /// <summary>
    /// Black's queen should report correct set of possible moves.
    /// </summary>
    [Fact]
    public void QueenPossibleMovesTest()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d2d4", "d7d5", "f1c4", "d8h4");
        Apply(b, "g2g3");
        var possMoves = b.GetPossibleMoves();
        var queenPossibleMoves = GetMovesAtPosition(possMoves, Pos("h4"));
        queenPossibleMoves.Count().Should().Be(12, "there should be 12 possible moves for Black's queen in this board");
        queenPossibleMoves.Should().BeEquivalentTo([Move("h4g4"), Move("h4f4"), Move("h4e4"), Move("h4g5"), Move("h4f6"), Move("h4e7"), Move("h4d8"), Move("h4h5"), Move("h4h6"), Move("h4h3"), Move("h4h2"), Move("h4g3")], "Black's queen cannot jump over other black pieces");
    }

    /// <summary>
    /// Placing White's king in check.
    /// </summary>
    [Fact]
    public void CheckKingTest()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d2d4", "d7d5", "f1c4", "d8h4", "h2h3");
        // placing White's king in check by capturing pawn
        Apply(b, "h4f2");
        // checking possible moves for the king
        var possMoves = b.GetPossibleMoves();
        var kingPossibleMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        kingPossibleMoves.Count().Should().Be(1, "the is only one way to get out of check");
        kingPossibleMoves.Should().BeEquivalentTo([Move("e1f2")], "White's king must capture Black's queen to get out of check");
        Apply(b, "e1f2");
        // checking board state after capture
        var queen = b.GetPieceAtPosition(Pos("f2"));
        queen.Player.Should().Be(1, "a white piece was moved to f2");
        queen.PieceType.Should().Be(ChessPieceType.King, "a king was moved to f2");
        b.CurrentAdvantage.Should().Be(Advantage(1, 8), "White lost one pawn of value 1 but captured a queen of value 9");
    }

    /// <summary>
    /// White's knights cannot capture another white piece.
    /// </summary>
    [Fact]
    public void KnightMove_Capture()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e6", "d2d4", "d7d5", "g1f3", "g8f6");
        var possMoves = b.GetPossibleMoves();
        var knightMoves = GetMovesAtPosition(possMoves, Pos("f3"));
        knightMoves.Should().HaveCount(5).And.NotContain(Move("f3d4"), "White's knight cannot jump over or capture another white piece");
        Apply(b, "e4d5"); // pawn captured black pawn
        Apply(b, "e6e5");
        Apply(b, "f3e5"); // knight captures black pawn
        // check board state after knight captures pawn
        b.CurrentAdvantage.Should().Be(Advantage(1, 2), "White captured two black pawns");
        var knight = b.GetPieceAtPosition(Pos("e5"));
        knight.Player.Should().Be(1, "a white piece captured black pawn at e5");
        knight.PieceType.Should().Be(ChessPieceType.Knight, "a knight captured a black pawn");
        var empty = b.GetPieceAtPosition(Pos("f3"));
        empty.Player.Should().Be(0, "the knight moved from f3 to e5");
        // undo move and check board state before knight captures pawn
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "only one black pawn was captured");
        knight = b.GetPieceAtPosition(Pos("f3"));
        knight.Player.Should().Be(1, "a white piece was returned to previous position at f3");
        knight.PieceType.Should().Be(ChessPieceType.Knight, "a knight was returned to f3");
        var captured = b.GetPieceAtPosition(Pos("e5"));
        captured.Player.Should().Be(2, "the captured black piece was returned to previous position at e5");
        captured.PieceType.Should().Be(ChessPieceType.Pawn, "a pawn was returned to e5");
    }

    /// <summary>
    /// Black's rooks cannot capture another black piece.
    /// </summary>
    [Fact]
    public void RookMove_Capture()
    {
        ChessBoard b = CreateBoardFromMoves("a2a3", "a7a5", "b2b4", "a8a6", "h2h3");
        var possMoves = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(possMoves, Pos("a6"));
        rookMoves.Should().HaveCount(9).And.NotContain([Move("a6a5"), Move("a6a4"), Move("a6a3")], "black rook cannot jump over or capture another black piece");
        Apply(b, "g7g5");
        Apply(b, "b4a5"); // white pawn captures black pawn
        Apply(b, "a6a5"); // black rook captures white pawn
        // check board state
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "both players have captured one pawn");
        var rook = b.GetPieceAtPosition(Pos("a5"));
        rook.Player.Should().Be(2, "a black piece was moved to a5");
        rook.PieceType.Should().Be(ChessPieceType.Rook, "a rook was moved to a5");
        var empty = b.GetPieceAtPosition(Pos("a6"));
        empty.Player.Should().Be(0, "a black rook was moved from a6 to a5 to capture pawn");
    }

    /// <summary>
    /// Black is put into checkmate.
    /// </summary>
    [Fact]
    public void ScholarsMate_Test()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d1h5", "b8c6", "f1c4", "g8f6", "h5f7");
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e8"));
        kingMoves.Should().HaveCount(0, "Black is put into checkmate");
        var king = b.GetPieceAtPosition(Pos("e8"));
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "White's queen captured a pawn");
    }

    /// <summary>
    /// [Initial Starting Board State]
    /// Testing white's rooks position in the initial board. 
    /// </summary>
    [Fact]
    public void InitialBoardState()
    {
        ChessBoard b = new ChessBoard();
        // Test white's rooks color
        b.GetPieceAtPosition(Pos(7, 7)).Player.Should().Be(1, "The bottom left of the board should start as white");
        b.GetPieceAtPosition(Pos(7, 0)).Player.Should().Be(1, "The bottom right of the board should start as white");
        // Test white's rooks piece type
        b.GetPieceAtPosition(Pos(7, 7)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (0,7)");
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (0,0)");
        // Test other properties
        b.CurrentPlayer.Should().Be(1, "Player 1 starts the game");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
    }

    /// <summary>
    /// [Tricky Move]
    /// Testing a queen side castle for white.
    /// Also includes UndoLastMove() and tests various aspects of the board.
    /// </summary>
    [Fact]
    public void CastleLeft()
    {
        ChessBoard b = CreateBoardFromMoves("d2d4", // White pawn forward
 "h7h6", // Black pawn (rightmost) forward
 "b1c3", // White knight out
 "h6h5", // Black pawn again
 "c1e3", // White bishop out
 "f7f6", // Black pawn forward
 "d1d2", // White queen out
 "h5h4" // Black pawn again
        );
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        // This is really cool its like functional programming patterns similar to js
        kingMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleQueenSide);
        b.UndoLastMove(); // Undo black's pawn
        b.UndoLastMove(); // Undo white's queen
        // Update the state of possible moves and the king's moves
        possMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        // After undoing white's queen the castle should not be possible because
        // the queen is between the rook and king
        kingMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleQueenSide, "After undoing the last two moves, the queen is in the way of the rook and king, so a queen side castle should not be possible.");
        b.CurrentPlayer.Should().Be(1, "After the 2 moves have been undone, it should be white's turn");
    }

    /// <summary>
    /// [ID Number Piece - GetPossibleMoves()]
    /// Stress testing black's knight taking pieces and having few
    /// and many possible moves.
    /// </summary>
    [Fact]
    public void KnightStress()
    {
        ChessBoard b = new ChessBoard();
        var possMoves = b.GetPossibleMoves();
        var blackKnightMoves = GetMovesAtPosition(possMoves, Pos("b8"));
        blackKnightMoves.Should().BeEmpty("It's not black's turn yet so black's knight should not be able to move.");
        b.ApplyMove(Move("a2a4")); // white
        possMoves = b.GetPossibleMoves();
        blackKnightMoves = GetMovesAtPosition(possMoves, Pos("b8"));
        blackKnightMoves.Should().HaveCount(2, "At the start of the game on black's first turn, the b8 knight can only move to a6 or c6.");
        // Push the rightmost white pawn forward to be eligible for capture by black's rightmost knight
        b.ApplyMove(Move("b7b6")); // black 
        b.ApplyMove(Move("h2h4")); // white 
        b.ApplyMove(Move("b6b5")); // black 
        b.ApplyMove(Move("h4h5")); // white  
        b.ApplyMove(Move("b5b4")); // black 
        b.ApplyMove(Move("h5h6")); // white  
        b.CurrentPlayer.Should().Be(2, "We have alternated between black and white and now it is black's turn");
        b.ApplyMove(Move("g8h6")); // capture white's pawn
        b.CurrentAdvantage.Player.Should().Be(2, "Black's knight just captured white's pawn");
        b.ApplyMove(Move("c2c3")); // push white pawn forward
        b.ApplyMove(Move("h6f5")); // move black knight
        b.ApplyMove(Move("c3c4")); // push white pawn forward
        possMoves = b.GetPossibleMoves();
        blackKnightMoves = GetMovesAtPosition(possMoves, Pos("f5"));
        blackKnightMoves.Should().HaveCount(6, "The black knight can move anywhere except north towards its own pieces.");
        b.UndoLastMove();
        possMoves = b.GetPossibleMoves();
        blackKnightMoves = GetMovesAtPosition(possMoves, Pos("f5"));
        blackKnightMoves.Should().HaveCount(0, "It should not be black's turn if the last move was undone.");
    }

    /// <summary>
    /// [Placing Black's King In Check]
    /// Testing valid moves after placing the king in check, this tests
    /// if the chess engine properly counts blocking the check and moving
    /// the king out of the way as valid moves.
    /// </summary>
    [Fact]
    public void KingCheck()
    {
        // Simple check opening with white's queen
        ChessBoard b = CreateBoardFromMoves("d2d4", "f7f6", "e2e4", "d7d5", "d1h5");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(2, "Black's king can either move out of the queen's path or place a pawn to block it.");
    }

    /// <summary>
    /// [Checkmate White]
    /// This uses a fool's mate to checkmate white.
    /// </summary>
    [Fact]
    public void CheckMate()
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e5", "g2g4", "d8h4");
        var possMoves = b.GetPossibleMoves();
        Console.WriteLine(possMoves.Count());
        b.IsFinished.Should().BeTrue("This is a classic fools mate.");
    }

    /// <summary>
    /// [Test Undoing Moves And Black's Revival]
    /// This captures a black pawn then brings it back to life with UndoLastMove()
    /// </summary>
    [Fact]
    public void MoveHistory()
    {
        ChessBoard b = CreateBoardFromMoves("d2d4", "e7e5", "d4e5");
        b.CurrentAdvantage.Player.Should().Be(1, "White's pawn just captured black's pawn.");
        b.GetPieceAtPosition(Pos("e5")).Player.Should().Be(1, "White occupies the captured piece.");
        b.GetPieceAtPosition(Pos("e5")).PieceType.Should().Be(ChessPieceType.Pawn, "White just captured with a pawn.");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e5")).Player.Should().Be(2, "After undoing a move, black's pawn should come back to life.");
        b.GetPieceAtPosition(Pos("e5")).PieceType.Should().Be(ChessPieceType.Pawn, "Black's pawn was the one that got captured, so after undoing a move, black's pawn should be back.");
    }

    /// <summary>
    /// Simple facts about "new" boards regarding queens and pawns
    /// </summary>
    [Fact]
    public void NewChessBoard__From_heleeon()
    {
        ChessBoard b = new ChessBoard();
        // Test queen piece locations.
        b.GetPieceAtPosition(Pos(7, 3)).PieceType.Should().Be(ChessPieceType.Queen, "White's queen at position (7,3)");
        b.GetPieceAtPosition(Pos(0, 3)).PieceType.Should().Be(ChessPieceType.Queen, "Black's queen at position (0,3)");
        // Test pawn piece locations and count 
        int whitePawnCount = 0;
        foreach (var pos in GetPositionsInRank(2))
        {
            b.GetPieceAtPosition(pos).Player.Should().Be(1, "Player 1 should be in second from bottom row of board");
            b.GetPieceAtPosition(pos).PieceType.Should().Be(ChessPieceType.Pawn, "White's pawns should be lined up across board at rank 2");
            whitePawnCount++;
        }

        whitePawnCount.Should().Be(8, "White starts with 8 pawns");
        int blackPawnCount = 0;
        foreach (var pos in GetPositionsInRank(7))
        {
            b.GetPieceAtPosition(pos).Player.Should().Be(2, "Player 2 should be in second from top row of board");
            b.GetPieceAtPosition(pos).PieceType.Should().Be(ChessPieceType.Pawn, "Black's pawns should be lined up across board at rank 7");
            blackPawnCount++;
        }

        blackPawnCount.Should().Be(8, "Black starts with 8 pawns");
    }

    /// <summary>
    /// Castling cannot happen when pieces between king and rook. 
    /// </summary>
    [Fact]
    public void Castling_BlockedByPieces()
    {
        ChessBoard b = CreateBoardFromMoves("g1f3", "b8c6", "e2e3", "d7d5");
        var possMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possMoves, Pos("e1")).Should().BeEquivalentTo([Move("e1e2")], "king at e1 cannot castle to g1");
    }

    /// <summary>
    /// Queenside castling is allowed when no pieces between king and left rook.
    /// Kingside castling is allowed when no pieces between king and right rook. 
    /// </summary>
    [Fact]
    public void Castling_Allowed()
    {
        ChessBoard b = CreateBoardFromMoves("g1h3", "g8h6", "b1a3", "b8a6", "d2d4", "d7d5", "c1e3", "c8e6", "d1d2", "d8d7", "g2g3", "g7g6", "f1g2", "f8g7");
        var possMoves = b.GetPossibleMoves();
        var CastlingExpected = GetMovesAtPosition(possMoves, Pos("e1"));
        CastlingExpected.Should().HaveCount(4, "King can go left one, right one, queenside castle, or kingside castle.").And.Contain(Move("e1d1")).And.Contain(Move("e1f1")).And.Contain(Move(Pos("e1"), Pos("c1"), ChessMoveType.CastleQueenSide)).And.Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        // Apply the kingside castle
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        var king = b.GetPieceAtPosition(Pos("g1"));
        king.PieceType.Should().Be(ChessPieceType.King, "king performed a castle move");
        var rook = b.GetPieceAtPosition(Pos("f1"));
        rook.PieceType.Should().Be(ChessPieceType.Rook, "rook moved because of king's castle");
        // Undo the move and check the board state
        b.UndoLastMove();
        king = b.GetPieceAtPosition(Pos("e1"));
        king.PieceType.Should().Be(ChessPieceType.King, "king in original position before castle");
        rook = b.GetPieceAtPosition(Pos("h1"));
        rook.PieceType.Should().Be(ChessPieceType.Rook, "rook in original position before castle");
    }

    /// <summary>
    /// A Knight moves in an "L" shape and can land on an enemy or empty square
    /// (031230431 mod 5) + 1 = 2 
    /// </summary>
    [Fact]
    public void KnightPossibleMoves()
    {
        ChessBoard b = CreateBoardFromMoves("f2f4", "g8h6", "f4f5");
        var possMoves = b.GetPossibleMoves();
        var KnightMovesExpected = GetMovesAtPosition(possMoves, Pos("h6"));
        KnightMovesExpected.Should().HaveCount(3, "Knight can jump to g8, jump to g4, or attack enemy pawn at f5").And.Contain(Move("h6g8")).And.Contain(Move("h6g4")).And.Contain(Move("h6f5"));
    }

    /// <summary>
    /// Bishop can attack enemy pieces diagonally
    /// </summary>
    [Fact]
    public void BishopAttacksKnight()
    {
        ChessBoard b = CreateBoardFromMoves("b1c3", "b7b6", "c3e4", "c8b7", "f2f3");
        var possMoves = b.GetPossibleMoves();
        var BishopMovesExpected = GetMovesAtPosition(possMoves, Pos("b7"));
        BishopMovesExpected.Should().Contain(Move("b7e4"), "Bishop can attack enemy knight");
        // Apply the bishop attacking the knight
        Apply(b, Move(Pos("b7"), Pos("e4")));
        var bishop = b.GetPieceAtPosition(Pos("e4"));
        bishop.PieceType.Should().Be(ChessPieceType.Bishop, "bishop took over knight's position");
        bishop.Player.Should().Be(2, "black's bishop captured white's knight");
        b.CurrentAdvantage.Should().Be(Advantage(2, 3), "black captured a knight");
        // Undo the move and check the board state
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no piece captured");
        bishop = b.GetPieceAtPosition(Pos("b7"));
        bishop.Player.Should().Be(2, "bishop in original position before attack");
        bishop.PieceType.Should().Be(ChessPieceType.Bishop, "bishop in original position before attack");
        var knight = b.GetPieceAtPosition(Pos("e4"));
        knight.Player.Should().Be(1, "knight in original position before attack");
        knight.PieceType.Should().Be(ChessPieceType.Knight, "knight in original position before attack");
    }

    /// <summary>
    /// King in check cannot move into check 
    /// </summary>
    [Fact]
    public void ConfirmCheckAndValidMoves()
    {
        ChessBoard b = CreateBoardFromMoves("d2d3", "e7e6", "b1a3", "f8b4");
        var possMoves = b.GetPossibleMoves();
        possMoves.Count().Should().Be(3, "There should be 3 possible moves to cancel the checking of white's king");
        possMoves.Should().BeEquivalentTo([Move("c2c3"), Move("c1d2"), Move("d1d2")], "the king cannot move into check");
    }

    [Fact]
    public void white_enpassant_then_undo()
    {
        var b = CreateBoardFromMoves("e2e4", "a7a6", "e4e5", "f7f5"); // quiet a-pawn moves to avoid random interferences
        var moves = b.GetPossibleMoves();
        var e5Moves = GetMovesAtPosition(moves, Pos("e5"));
        e5Moves.Should().Contain(m => m.MoveType == ChessMoveType.EnPassant, "EP should be available right after a two-step adjacent pawn move");
        // actually do EP (e5 -> f6)
        Apply(b, Move(Pos("e5"), Pos("f6"), ChessMoveType.EnPassant));
        var onF6 = b.GetPieceAtPosition(Pos("f6"));
        onF6.PieceType.Should().Be(ChessPieceType.Pawn, "white pawn should land on f6 after EP");
        onF6.Player.Should().Be(1, "the pawn belongs to white");
        b.GetPieceAtPosition(Pos("f5")).Player.Should().Be(0, "the black pawn on f5 should be captured/removed");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "white should be up a pawn after the capture");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e5")).PieceType.Should().Be(ChessPieceType.Pawn, "undo puts white pawn back on e5");
        b.GetPieceAtPosition(Pos("f5")).PieceType.Should().Be(ChessPieceType.Pawn, "undo resurrects the black pawn that got EP’d");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "material advantage should reset after undo");
    }

    [Fact]
    public void white_castle_kingside_and_undo()
    {
        var b = CreateBoardFromMoves("e2e4", "a7a6", // open a bit and keep black quiet
 "g1f3", "a6a5", // move knight off g1
 "f1e2", "h7h6" // move bishop off f1, path e1-f1-g1 is clear
        );
        Apply(b, "e1g1"); // castle
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "king should end up on g1 after O-O");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "rook jumps to f1 when castling O-O");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "king returns to e1 after undo");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "rook returns to h1 after undo");
    }

    [Fact]
    public void black_is_in_check_and_all_replies_escape()
    {
        var b = CreateBoardFromMoves("e2e4", "e7e5", "d1h5", "b8c6", "f1c4", "g8f6", "h5f7");
        b.IsCheck.Should().BeTrue("black king should be in check after Qxf7+");
        var replies = b.GetPossibleMoves();
        foreach (var m in replies)
        {
            b.ApplyMove(m);
            b.IsCheck.Should().BeFalse("any legal reply must get black OUT of check");
            b.UndoLastMove();
        }
    }

    // en passant for BLACK this time, to prove we didn't only test white direction
    [Fact]
    public void black_enpassant_simple()
    {
        var b = CreateBoardFromMoves("e2e4", "b7b5", "e4e5", "b5b4", "a2a4");
        var mvs = b.GetPossibleMoves();
        var fromB4 = GetMovesAtPosition(mvs, Pos("b4"));
        fromB4.Should().Contain(m => m.MoveType == ChessMoveType.EnPassant && m.EndPosition.Equals(Pos("a3")), "after a2a4, black should have EP from b4 to a3");
        Apply(b, Move(Pos("b4"), Pos("a3"), ChessMoveType.EnPassant));
        b.GetPieceAtPosition(Pos("a3")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn lands on a3 after EP");
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(0, "white pawn from a4 is captured/removed");
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "black should be up a pawn now");
    }

    [Fact]
    public void undo_black_pawn_capture_basic()
    {
        var b = CreateBoardFromMoves("e2e4", "d7d5", "a2a3", "d5e4"); // black pawn from d5 takes white pawn on e4
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "black grabbed a pawn so they should be up 1");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Pawn, "undo should put black pawn back on d5");
        b.GetPieceAtPosition(Pos("e4")).PieceType.Should().Be(ChessPieceType.Pawn, "undo should restore the white pawn on e4");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "advantage goes back to even after undo");
    }

    [Fact]
    public void possible_moves_rook_white_kinda_open_position()
    {
        var b = CreateBoardFromMoves("a2a4", "h7h6", // free the a1 rook
 "a1a3", "h6h5", // lift rook to rank 3
 "a3d3", "a7a6", // slide rook across rank 3 (b3,c3,d3 are empty), quiet a7
 "d3d4", "a6a5");
        var all = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(all, Pos("d4"));
        rookMoves.Should().Contain(Move("d4d5"), "rook should be able to go up one on the file").And.Contain(Move("d4d6"), "and another square up too since it's empty").And.Contain(Move("d4e4"), "rook moves right along the rank").And.Contain(Move("d4c4"), "rook moves left along the rank").And.OnlyContain(m => m.StartPosition.Equals(Pos("d4")), "every generated move in this filtered set should start at d4");
    }

    //validation of pins
    [Fact]
    public void PinCheck()
    {
        ChessBoard b = CreateBoardFromMoves("f2f4", "e7e5", "d2d4", "e8e7", "d1d3", "h7h5", "d3e3");
        var possMoves = b.GetPossibleMoves();
        var oneMove = GetMovesAtPosition(possMoves, Pos("e5"));
        // The black pawn at c5 should only have one move
        oneMove.Should().HaveCount(1, "The black pawn at e5 should only have one move");
    }

    //validation of short and long castle
    //uses UndoLastMove()
    [Fact]
    public void CastleCheck()
    {
        ChessBoard b = CreateBoardFromMoves("c2c3", "f7f6", "d1a4", "g8h6", "b1a3", "e7e6", "d2d3", "f8d6", "c1e3");
        //short castle
        Apply(b, "e8g8");
        b.GetPieceAtPosition(Pos(0, 5)).PieceType.Should().Be(ChessPieceType.Rook, "Black's rook at position (0,5)");
        b.GetPieceAtPosition(Pos(0, 6)).PieceType.Should().Be(ChessPieceType.King, "Black's king at position (0,6)");
        b.UndoLastMove();
        //does undo undo short castles correctly?
        b.GetPieceAtPosition(Pos(0, 7)).PieceType.Should().Be(ChessPieceType.Rook, "Black's rook at position (0,7)");
        b.GetPieceAtPosition(Pos(0, 4)).PieceType.Should().Be(ChessPieceType.King, "Black's king at position (0,4)");
        Apply(b, "e8g8");
        //long castle
        Apply(b, "e1c1");
        b.GetPieceAtPosition(Pos(7, 2)).PieceType.Should().Be(ChessPieceType.King, "White's king at position (7,2)");
        b.GetPieceAtPosition(Pos(7, 3)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (7,3)");
        //does undo undo long castles correctly?
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos(7, 4)).PieceType.Should().Be(ChessPieceType.King, "White's king at position (7,4)");
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (7,0)");
    }

    //validates that the king must move out of check and cannot move into check
    //this test places a king in check
    [Fact]
    public void KingCheckTest()
    {
        ChessBoard b = CreateBoardFromMoves("e2e3", "d7d6", "d1e2", "e8d7", "e2d3", "d7c6", "d3e4", "c6c5", "e4d4");
        var possMoves = b.GetPossibleMoves();
        var oneMove = GetMovesAtPosition(possMoves, Pos("c5"));
        // The black king at c5 should only have one move
        oneMove.Should().HaveCount(1, "The black king at c5 should only have one move");
    }

    //validation of GetPossibleMoves using a bishop
    //validates using GetPossibleMoves()
    //uses UndoLastMove()
    [Fact]
    public void GetPossibleMovesCheck()
    {
        ChessBoard b = CreateBoardFromMoves("g2g4", "b7b5", "f1g2", "h7h6", "g2a8", "c8b7");
        var possMoves = b.GetPossibleMoves();
        var oneMove = GetMovesAtPosition(possMoves, Pos("a8"));
        // The white bishop at a8 should only have one move
        oneMove.Should().HaveCount(1, "The white bishop at a8 should only have one move");
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(2, "undoing a move should change the current player");
        Apply(b, "g7g6");
        possMoves = b.GetPossibleMoves();
        oneMove = GetMovesAtPosition(possMoves, Pos("a8"));
        oneMove.Should().HaveCount(6, "The white bishop at a8 should have 6 moves");
    }

    //checks if white pawns can promote to all pieces (legal)
    //involves a tricky move
    //uses UndoLastMove()
    [Fact]
    public void PawnPromotionToAllPieces()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "h3g2");
        Apply(b, "b7a8knight");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Knight, "Whites's knight at position (0,0)");
        b.UndoLastMove();
        Apply(b, "b7a8rook");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Rook, "Whites's rook at position (0,0)");
        b.UndoLastMove();
        Apply(b, "b7a8bishop");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Bishop, "Whites's bishop at position (0,0)");
        b.UndoLastMove();
        Apply(b, "b7a8queen");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Queen, "Whites's queen at position (0,0)");
    }

    // checks if starting rooks were properly put in
    // is about the initial starting board state
    [Fact]
    public void StartingRooks()
    {
        ChessBoard b = new ChessBoard();
        b.GetPieceAtPosition(Pos(0, 0)).Player.Should().Be(2, "Player 2 rook should be in upper left of board");
        b.GetPieceAtPosition(Pos(7, 0)).Player.Should().Be(1, "Player 1 rook should be in lower left of board");
        b.GetPieceAtPosition(Pos(0, 7)).Player.Should().Be(2, "Player 2 rook should be in upper right of board");
        b.GetPieceAtPosition(Pos(7, 7)).Player.Should().Be(1, "Player 1 rook should be in lower right of board");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Rook, "Black's rook at position (0,0)");
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (7,0)");
        b.GetPieceAtPosition(Pos(0, 7)).PieceType.Should().Be(ChessPieceType.Rook, "Black's rook at position (0,7)");
        b.GetPieceAtPosition(Pos(7, 7)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook at position (7,7)");
    }

    [Fact]
    public void StartingBoard_KingsAndTurn()
    {
        var b = new ChessBoard();
        // sanity: kings on e1/e8, white to move
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "white king starts on e1");
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "black king starts on e8");
        b.CurrentPlayer.Should().Be(1, "white moves first");
        b.IsCheck.Should().BeFalse("no king in check at start");
    }

    [Fact]
    public void UndoLastMove_PawnCaptureRestoresState()
    {
        // quick pawn trade a2a4, b7b5, a4xb5
        var b = CreateBoardFromMoves("a2a4", "b7b5", "a4b5");
        b.GetPieceAtPosition(Pos("b5")).Player.Should().Be(1, "white pawn sits on b5 after capture");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "white should be up a pawn");
        // undo the capture
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(1, "white pawn back on a4");
        b.GetPieceAtPosition(Pos("b5")).Player.Should().Be(2, "black pawn back on b5");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "material back to equal");
    }

    [Fact]
    public void CastleKingSide_ThenUndo_RestoresKingAndRook()
    {
        // castle short (king side)
        var b = CreateBoardFromMoves("e2e4", "e7e5", "g1f3", "g8f6", "f1e2", "b8c6", "e1g1");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "white king should castle to g1");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "rook lands on f1 after castling");
        // undo castling
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "king back on e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "rook back on h1");
        b.CurrentPlayer.Should().Be(1, "white turn again after undo");
    }

    [Fact]
    public void GetPossibleMoves_RookBlockedAtStart_BlackRookHasZeroMoves()
    {
        var b = new ChessBoard();
        Apply(b, "a2a3"); // make it black's turn
        var poss = b.GetPossibleMoves();
        var a8 = GetMovesAtPosition(poss, Pos("a8"));
        a8.Should().BeEmpty("rook is stuck behind pawn/bishop early on");
    }

    [Fact]
    public void ScholarsMate_Checkmate_NoMovesRemain()
    {
        // scholar's mate sequence
        var b = CreateBoardFromMoves("e2e4", "e7e5", "d1h5", "b8c6", "f1c4", "g8f6", "h5f7");
        b.IsCheckmate.Should().BeTrue("Qxf7 is checkmate");
        b.GetPossibleMoves().Should().BeEmpty("no legal moves left for black");
        b.CurrentPlayer.Should().Be(2, "black to move but has nothing");
    }

    [Fact]
    public void EnPassant_ByBlack_ThenUndo_RestoresBoard()
    {
        // set up black en passant capture on b3
        var b = CreateBoardFromMoves("h2h3", "a7a5", "h3h4", "a5a4", "b2b4");
        var poss = b.GetPossibleMoves();
        var ep = GetMovesAtPosition(poss, Pos("a4"));
        ep.Should().Contain(Move(Pos("a4"), Pos("b3"), ChessMoveType.EnPassant), "black pawn should have en passant capture available");
        // do the capture
        Apply(b, Move(Pos("a4"), Pos("b3"), ChessMoveType.EnPassant));
        b.GetPieceAtPosition(Pos("b3")).PieceType.Should().Be(ChessPieceType.Pawn, "black pawn now on b3");
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(0, "white pawn removed from b4");
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "black should be up a pawn after en passant");
        // undo and restore
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(2, "black pawn back on a4");
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(1, "white pawn back on b4");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "material even again after undo");
    }

    /// <summary>
    /// Performs several simple test cases that checks the validity of the initial chess board.
    /// </summary>
    [Fact]
    public void InitialChessBoard()
    {
        ChessBoard b = new ChessBoard();
        var allMoves = b.GetPossibleMoves();
        // Initially, player 1 (white) should have 20 possible moves (16 pawn moves, 4 knight moves)
        allMoves.Should().HaveCount(20, "There are 20 possible moves for player 1 in the beginning");
        // White knights should be in their correct starting positions
        b.GetPieceAtPosition(Pos(7, 1)).PieceType.Should().Be(ChessPieceType.Knight, "White knight at position (7,1");
        b.GetPieceAtPosition(Pos(7, 1)).Player.Should().Be(1, "Chess piece at position (7,1) should be White's piece (Player 1)");
        // Knight at g1 should have 2 moves (to f3 and h3)
        var knightMoves = GetMovesAtPosition(allMoves, Pos("g1"));
        knightMoves.Should().HaveCount(2).And.BeEquivalentTo([Move("g1f3"), Move("g1h3")]);
        // Checks white and black pieces at specific positions
        b.GetPieceAtPosition(Pos(1, 0)).PieceType.Should().Be(ChessPieceType.Pawn, "Black pawn at position (1,0)");
        b.GetPieceAtPosition(Pos(0, 7)).PieceType.Should().Be(ChessPieceType.Rook, "Black pawn at position (0,7)");
        b.GetPieceAtPosition(Pos(0, 5)).PieceType.Should().Be(ChessPieceType.Bishop, "Black bishop at position (0,5)");
        b.GetPieceAtPosition(Pos(7, 7)).PieceType.Should().Be(ChessPieceType.Rook, "White rook at position (7,7)");
        b.GetPieceAtPosition(Pos(7, 3)).PieceType.Should().Be(ChessPieceType.Queen, "White queen at position (7,3)");
        b.GetPieceAtPosition(Pos(6, 3)).PieceType.Should().Be(ChessPieceType.Pawn, "White pawn at position (6,3)");
        b.GetPieceAtPosition(Pos(2, 0)).PieceType.Should().Be(0, "No piece at position (2,0");
        b.GetPieceAtPosition(Pos(4, 6)).Player.Should().Be(0, "No player piece at position (2,0");
        // Pawn at d2 should have 2 moves (to d3 and d4)
        var pawnMoves = GetMovesAtPosition(allMoves, Pos("d2"));
        pawnMoves.Should().HaveCount(2).And.BeEquivalentTo([Move("d2d3"), Move("d2d4")]);
    }

    /// <summary>
    /// Checks the validity of castling moves for white and black.
    /// </summary>
    [Fact]
    public void CastlingTest()
    {
        ChessBoard b = CreateBoardFromMoves("e2e3", "d7d5", "g1f3", "b8a6", "f1d3", "c8g4");
        // Check is castling is available for white's king
        var allMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(allMoves, Pos("e1"));
        kingMoves.Should().HaveCount(3).And.Contain(m => m.MoveType == ChessMoveType.CastleKingSide, "White king can castle in the King side");
        // White castle in king side
        Apply(b, Move("e1g1"));
        // Check White king and rook's positions after castling
        b.GetPieceAtPosition(Pos(7, 6)).PieceType.Should().Be(ChessPieceType.King, "White king at position (7,6) after castling");
        b.GetPieceAtPosition(Pos(7, 5)).PieceType.Should().Be(ChessPieceType.Rook, "White rook at position (7,5) after castling");
        Apply(b, Move("d8d6"), Move("d3b5"));
        // White move d3b5 puts the Black king in check
        // Get moves for black, check if castling is available
        allMoves = b.GetPossibleMoves();
        allMoves.Should().HaveCount(5) // 1 king move and 4 can block the check
        .And.NotContain(m => m.MoveType == ChessMoveType.CastleQueenSide, "Black king cannot castle in the Queen side because it is in check");
        // Black's queen blocks the check
        Apply(b, Move("d6c6"), Move("a2a4"));
        allMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(allMoves, Pos("e8"));
        kingMoves.Should().HaveCount(3).And.Contain(m => m.MoveType == ChessMoveType.CastleQueenSide, "Black king can now castle in the Queen side");
        // Black castle in queen side
        Apply(b, Move("e8c8"));
        // Check Black king and rook's positions after castling 
        b.GetPieceAtPosition(Pos(0, 2)).PieceType.Should().Be(ChessPieceType.King, "Black king at position (0,2) after castling");
        b.GetPieceAtPosition(Pos(0, 3)).PieceType.Should().Be(ChessPieceType.Rook, "Black rook at position (0,3) after castling");
    }

    /// <summary>
    /// Checks the validity of castling moves for black after moving the king and undoing.
    /// </summary>
    [Fact]
    public void UndoForCastling()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "g7g6", "b1c3", "f8h6", "d2d3", "g8f6", "e4e5", "e8f8", // Black moves king, so castling should not be available anymore
 "e5f6", // White pawn captures black knight
 "f8e8", // Black moves king back to original position
 "g2g4");
        // Check if castling is available for Black's king
        var allMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(allMoves, Pos("e8"));
        kingMoves.Should().HaveCount(1).And.NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "Black king cannot castle because it already moved");
        // Undo a move for both players
        b.UndoLastMove();
        b.UndoLastMove();
        // Castling is still not available since Black's king is still not back to its initial position
        allMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(allMoves, Pos("f8"));
        kingMoves.Should().HaveCount(2).And.NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "Black king cannot castle because it already moved");
        // Undo until Black's king has not moved
        b.UndoLastMove();
        b.UndoLastMove();
        // Check if can castle now
        allMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(allMoves, Pos("e8"));
        kingMoves.Should().HaveCount(2).And.Contain(m => m.MoveType == ChessMoveType.CastleKingSide, "Black king can castle in king side");
    }

    /// <summary>
    /// Undo tests for check and advantage.
    /// </summary>
    [Fact]
    public void UndoCheckAndAdvantage()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "g8h6", "f1c4", "b7b6", "d1h5", "c8a6", "h5f7" // White's queen takes a pawn and checks the Black's king
        );
        b.CurrentPlayer.Should().Be(2, "Player 2's turn (Black)");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "White queen took a Black pawn");
        b.IsCheck.Should().Be(true, "Black king is in check");
        var allMoves = b.GetPossibleMoves();
        allMoves.Should().HaveCount(1).And.Contain(Move("h6f7"), "The only move is for Black knight to take White queen, removing the check");
        // Undo White's move and make a different one
        b.UndoLastMove();
        Apply(b, Move("d2d3"));
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "White queen taking Black pawn was undone so no one has advantage");
        b.IsCheck.Should().Be(false, "Black king is no longer in check");
    }

    /// <summary>
    /// Test King's possible moves ((030188169 % 5) + 1 = 5 = King).
    /// </summary>
    [Fact]
    public void KingPossibleMoves()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d5");
        // Check possible moves for White king at e1
        var allMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(allMoves, Pos("e1"));
        kingMoves.Should().HaveCount(1).And.Contain(Move("e1e2"), "White king only has one move");
        // Move White king to f3
        Apply(b, Move("e1e2"), Move("b7b6"), Move("e2f3"), Move("e7e6"));
        // Check possible moves for White king at f3
        allMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(allMoves, Pos("f3"));
        kingMoves.Should().HaveCount(5).And.BeEquivalentTo([Move("f3e2"), Move("f3e3"), Move("f3f4"), Move("f3g3"), Move("f3g4")], "From f3, White king can move to e2, e3, f4, g3, g4");
        // Bring Black pawn closer to White king from e6 to e5
        Apply(b, Move("a2a4"), Move("e6e5"));
        // Check possible moves for White king at f3 again
        // Notice that the White king cannot move to f4 and g4 because it would be moving into check from Black pawn and bishop
        allMoves = b.GetPossibleMoves();
        kingMoves = GetMovesAtPosition(allMoves, Pos("f3"));
        kingMoves.Should().HaveCount(3).And.BeEquivalentTo([Move("f3e2"), Move("f3e3"), Move("f3g3")], "White king can only move to e2, e3, g3");
    }

    /// <summary>
    /// Check all possible moves when the White king is at check.
    /// </summary>
    [Fact]
    public void KingInCheck()
    {
        ChessBoard b = CreateBoardFromMoves("f2f4", "e7e6", "b1c3", "d8h4" // Black queen checks White king
        );
        var allMoves = b.GetPossibleMoves();
        allMoves.Should().HaveCount(1).And.Contain(Move("g2g3"), "Player 1 can only defend the check with a single pawn move");
        // Black queen advances forward to check, White pawn can only take Black queen to defend
        Apply(b, Move("g2g3"), Move("h4g3"));
        allMoves = b.GetPossibleMoves();
        allMoves.Should().HaveCount(1).And.Contain(Move("h2g3"), "Player 1 can only defend by taking the Black queen");
        // New scenario with Player 1 having multiple moves to defend a check
        b = CreateBoardFromMoves("e2e4", "e7e5", "d2d4", "g8f6", "g1f3", "f8b4" // Black bishop checks White king
        );
        allMoves = b.GetPossibleMoves();
        allMoves.Should().HaveCount(7).And.BeEquivalentTo([Move("c2c3"), Move("b1c3"), Move("b1d2"), Move("c1d2"), Move("d1d2"), Move("f3d2"), Move("e1e2")], "Player 1 can defend with 6 possible moves");
    }

    /// <summary>
    /// Test Case #1: All possible moves in a new board state
    /// </summary>
    [Fact]
    public void PossibleInitialMoves()
    {
        // New chess board state
        ChessBoard b = new ChessBoard();
        // 20 possible moves - Only pawns and knights can move
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(20, "There are 20 possible moves from the starting board");
        // The 8 pawns can move either 1 or 2 squares up
        var pawns = b.GetPositionsOfPiece(ChessPieceType.Pawn, 0);
        foreach (var pawn in pawns)
        {
            var movesAtPos = GetMovesAtPosition(possMoves, pawn);
            movesAtPos.Should().HaveCount(2, "A Pawn in it's starting position must have exactly 2 possible moves ")// White pawns can only move up, so offset by 1/2
            // For Black Pawns we would offset the row by -1/-2 since they move down
            .And.BeEquivalentTo([Move(pawn, pawn.Translate(1, 0)), Move(pawn, pawn.Translate(2, 0))], "A Pawn in it's starting position must have the option to move up by exactly 1 or 2 squares");
        }

        // Left white knight can move to a3/c3, right can move to h3/f3
        var knights = b.GetPositionsOfPiece(ChessPieceType.Knight, 0);
        foreach (var knight in knights)
        {
            var movesAtPos = GetMovesAtPosition(possMoves, knight);
            movesAtPos.Should().HaveCount(2, "A Knight in it's starting position must have exactly 2 possible moves ")// Knights skip two squares vertically and one square horizontally in any direction
            // At their starting positions, the only valid moves are up 2 and left/right 1.
            .And.BeEquivalentTo([Move(knight, knight.Translate(2, 1)), Move(knight, knight.Translate(2, -1))], "A Knight in it's starting position is only allowed to move up 2, and the option to move left/right by 1");
        }
    }

    /// <summary>
    /// Test Case #2: En Passant
    /// </summary>
    [Fact]
    public void EnPassant()
    {
        // Conditions for En Passant
        // 1) Only Pawns can perform the move
        // 2) Only triggered when opponent moves their pawn exactly 2 squares forward from its starting
        // position and it's directly beside the capturing pawn on the same row/rank. So, for White, the capturing
        // Pawn must be on the 5th rank, for Black, the 4th rank. Moving the Pawn one square at a time twice disqualifies it
        // 3) Capture must be immediate. The En Passant capture must be made on the next move or else that pawn will be
        // immune to an En Passant capture for the rest of the game
        // 4) Capturing Pawn moves diagonally to the square that the opponents pawn previously skipped over
        // Create board state where black pawn is on g4 directly next to two white pawns. On the left is a white pawn
        // that moved there in one move, and on the right is a pawn that moved there in two moves. 
        // Based on condition 2, the black pawn should be able to perform an en passant to f3, but not h3.
        ChessBoard b = CreateBoardFromMoves("h2h3", "g7g5", "h3h4", "g5g4", "f2f4");
        var possMoves = b.GetPossibleMoves();
        var enpass = GetMovesAtPosition(possMoves, Pos("g4"));
        // Possible moves are: en passant to f3, or forward 1 to g3
        enpass.Should().HaveCount(2, "Pawn allowed to move forward, or take En Passant move to f3, but not h3.").And.BeEquivalentTo([Move("g4g3"), Move("g4f3")]);
        // Now, perform en passant move and make sure the white pawn on f4 was taken out
        Apply(b, Move(Pos("g4"), Pos("f3"), ChessMoveType.EnPassant));
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(0, "En Passant move takes out the Pawn");
        // Undo last move, and now check for condition where en passant move is not immediately taken
        b.UndoLastMove();
        Apply(b, Move(Pos("a7"), Pos("a6")));
        Apply(b, "a2a3");
        possMoves = b.GetPossibleMoves();
        var notImmediate = GetMovesAtPosition(possMoves, Pos("g4"));
        notImmediate.Should().HaveCount(1, "White Pawn is now immune to En Passant move since it was not immediately taken").And.Contain(Move("g4g3"), "Pawn is only allowed to move forward one square");
    }

    /// <summary>
    /// Test Case #3: Undo Capture
    /// </summary>
    [Fact]
    public void UndoCapture()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "b7b5");
        Apply(b, "a4b5");
        b.UndoLastMove();
        b.GetPlayerAtPosition(Pos("b5")).Should().Be(2, "Square was occupied by black before its capture");
        b.GetPieceAtPosition(Pos("b5")).PieceType.Should().Be(ChessPieceType.Pawn, "Pawn was in that square before it was captured");
    }

    /// <summary>
    /// Test Case #4: Undo Pawn Promotion
    /// </summary>
    [Fact]
    public void UndoPawnPromo()
    {
        ChessBoard b = CreateBoardFromMoves("a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "h3g2", "c2c3");
        //Apply(b, Move(Pos("g2"),Pos("h1queen"), ChessMoveType.PawnPromote));
        Apply(b, "g2h1queen");
        // Make sure Black Pawn was promoted to queen
        b.GetPlayerAtPosition(Pos("h1")).Should().Be(2, "Black Pawn promoted");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Queen, "Pawn promoted to queen");
        // After undo, make sure g2 is still a Black Pawn and h1 is a White Rook
        b.UndoLastMove();
        b.GetPlayerAtPosition(Pos("g2")).Should().Be(2, "Black Pawn");
        b.GetPlayerAtPosition(Pos("h1")).Should().Be(1, "White Rook");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "Before Pawn Promotion, h1 had a White Rook");
    }

    // 015224688 mod 5 = 3 + 1 = 4 = Queen
    /// <summary>
    /// Test Case #5: Queen Stress Test
    /// </summary>
    [Fact]
    public void QueenStressTest()
    {
        // Queen can move horizontally, diagonally, vertically to any space as long as it's not blocked. If blocked by an enemy,
        // the queen can capture that square but can't move beyond it 
        // At start position, the queen cannot make any moves because it's blocked by allies
        ChessBoard b = new ChessBoard();
        var possMoves = b.GetPossibleMoves();
        var queenMoves = GetMovesAtPosition(possMoves, Pos("d1"));
        queenMoves.Should().HaveCount(0, "Queen cannot move when surrounded in all directions by border and allies");
        // White Queen put in left-middle of board and able to move in all directions
        // Board State: https://imgur.com/HzTWrsZ
        b = CreateBoardFromMoves("d2d4", "d7d5", "d1d3", "a7a6", "d3f5", "a6a5");
        possMoves = b.GetPossibleMoves();
        queenMoves = GetMovesAtPosition(possMoves, Pos("f5"));
        // Manually Counted Possible Moves by Queen from board state:
        var expectedMoves = new List<ChessMove>
        {
            // 2 to the right(g5, h5)
            Move("f5g5"),
            Move("f5h5"),
            // 2 to the left(e5, d5)
            Move("f5e5"),
            Move("f5d5"),
            // 2 to the top(f6, f7)
            Move("f5f6"),
            Move("f5f7"),
            // 2 to the bottom(f4, f3)
            Move("f5f4"),
            Move("f5f3"),
            // 2 top right(g6, h7)
            Move("f5g6"),
            Move("f5h7"),
            // 2 bot right(g4, h3)
            Move("f5g4"),
            Move("f5h3"),
            // 2 bot left(e4, d3)
            Move("f5e4"),
            Move("f5d3"),
            // 3 top left(e6, d7, c8)
            Move("f5e6"),
            Move("f5d7"),
            Move("f5c8"),
        // Total of 17
        };
        queenMoves.Should().HaveCount(expectedMoves.Count(), "The queen has 17 possible moves").And.BeEquivalentTo(expectedMoves, "The queen has 17 possible moves in all directions");
    }

    /// <summary>
    /// Test Case #6: King in Check
    /// </summary>
    [Fact]
    public void KingCheck__From_josephsong4258()
    {
        // King in Check when under direct attack by one or more of the opponent's pieces.
        // So, if opponent were to make their next move they could capture King
        // Board State: https://i.imgur.com/hQh8tfd.png
        // King is in check, so Blacks only possible moves are e8e7 which puts the King in safety
        // or g7g6 which puts the Pawn between the Queen's crossfire
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "a2a3", "f7f5", "d1h5");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(2, "King is in check").And.BeEquivalentTo([Move("e8e7"), Move("g7g6")], "e8e7 and g7g6 are the only moves that get the King to safety");
    }

    // <summary>
    // Checks the board is properly initalized by verifying that the current player is Player 1 and that there is no advantage
    // </summary>
    [Fact]
    public void WhiteStarts_AdvantageNeutral()
    {
        ChessBoard b = new ChessBoard();
        b.CurrentPlayer.Should().Be(1, "White starts the game.");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No player has an advantage or disadvantage yet.");
    }

    // <summary>
    // Sets up the board for a possible KingSide Castle (clears out the pieces between the white king and rook), then checks that a KingSide Castle is possible in the king's moveset
    // </summary>
    [Fact]
    public void CastleKingSideTest()
    {
        ChessBoard b = CreateBoardFromMoves("g2g4", "f7f6", "f1h3", "e7e6", "g1f3", "c7c6");
        // Current state of the board:
        //   a b c d e f g h
        // 8 R H B Q K B H R
        // 7 P P   P     P P 
        // 6     P   P P
        // 5
        // 4             P
        // 3           H   B
        // 2 P P P P P P   P
        // 1 R H B Q K     R 
        //   a b c d e f g h
        // Get possible moves across the current board
        var possMoves = b.GetPossibleMoves();
        // Look at moves possible for the king
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        // One of those moves should be of movetype CastleKingSide
        kingMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleKingSide);
        // Look at moves possible for the adjacent rook
        var rookMoves = GetMovesAtPosition(possMoves, Pos("h1"));
        // It should only have two possible moves, left one or left twice. The CastleKingSide is only possible on the king piece.
        rookMoves.Should().HaveCount(2, "Rook should be able to move left one or left twice.");
    }

    // <summary>
    // The board is set so that the white bishop can take the black knight. The advantage and piece on the position are checked before the bishop takes, after the bishop takes, then after the undo.
    // </summary>
    [Fact]
    public void WhiteBishopCapturesBlackKnight_UndoTest()
    {
        ChessBoard b = CreateBoardFromMoves("d2d3", "b8a6", "c1e3", "a6c5");
        // Current state of the board:
        //   a b c d e f g h
        // 8 R   B Q K B H R
        // 7 P P P P P P P P
        // 6 
        // 5     H
        // 4 
        // 3       P B
        // 2 P P P   P P P P
        // 1 R H   Q K B H R
        //   a b c d e f g h
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No operation should give an advantage yet.");
        b.GetPieceAtPosition(Pos("c5")).Player.Should().Be(2, "Black knight should currently be on c5.");
        // Moves white bishop to take black knight
        Apply(b, Move("e3c5"));
        b.CurrentAdvantage.Should().Be(Advantage(1, 3), "The white bishop took out the black knight on c5.");
        b.GetPieceAtPosition(Pos("c5")).Player.Should().Be(1, "White bishop took the black knight, replacing it on c5.");
        // Undos last move (e3 to c5)
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "Because of the UndoLastMove(), no operation has shifted the advantage yet. ");
        b.GetPieceAtPosition(Pos("c5")).Player.Should().Be(2, "Black knight should currently be on c5.");
    }

    // <summary>
    // Performs a series of moves including captures. Undos twice, checking each time for pieces at relevant locations and player advantages.
    // </summary>
    [Fact]
    public void MultipleMoves_UndoTest()
    {
        ChessBoard b = CreateBoardFromMoves("g1h3", "e7e6", "h3f4", "f8d6", "e2e3");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No operation should give an advantage yet.");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(2, "Black bishop should currently be on d6.");
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(1, "White knight should currently be on f4.");
        // Move black bishop to take white knight
        Apply(b, Move("d6f4"));
        b.CurrentAdvantage.Should().Be(Advantage(2, 3), "Player 2 should have the advantage since they took the white knight.");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(0, "No piece should currently be on d6.");
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(2, "Black bishop should currently be on f4");
        // Move white pawn to take black bishop
        Apply(b, "e3f4");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "Because a bishop and a knight are of the same value, the advtantage is now neutral.");
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(1, "White pawn should currently be on f4");
        b.GetPieceAtPosition(Pos("e3")).Player.Should().Be(0, "No piece should currently be on e3.");
        // Undo white pawn taking black bishop
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(2, 3), "Player 2 should have the advantage since they took the white knight.");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(0, "No piece should currently be on d6.");
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(2, "Black bishop should currently be on f4");
        // Undo black bishop taking white knight
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No operation should give an advantage yet.");
        b.GetPieceAtPosition(Pos("d6")).Player.Should().Be(2, "Black bishop should currently be on d6.");
        b.GetPieceAtPosition(Pos("f4")).Player.Should().Be(1, "White knight should currently be on f4.");
    }

    // <summary>
    // Places black rook in middle of board, along with a few friendly and enemy pieces. Checks all possible moves of the rook.
    // </summary>
    [Fact]
    public void CheckRookMoves_GetPossibleMovesTest()
    {
        ChessBoard b = CreateBoardFromMoves("d2d4", "h7h5", "c1f4", "h8h6", "d4d5", "h6f6", "f4g5", "f6f5", "a2a3");
        // Student ID: 029548231
        // 29548231mod5 = 1
        // Therefore, we are testing the black rook.
        var possMoves = b.GetPossibleMoves();
        var rookMoves = GetMovesAtPosition(possMoves, Pos("f5"));
        rookMoves.Should().HaveCount(7, "The rook should have 7 possible moves based on the preset board.").And.Contain(Move("f5g5")) // Right one space to capture white bishop - can't go further right until it is captured
        .And.Contain(Move("f5f6")) // Up one space - next space (f7) is filled by a friendly pawn
        .And.Contain(Move("f5e5")) // Left one space
        .And.Contain(Move("f5d5")) // Left two spaces to capture white pawn - can't go further left until it is captured
        .And.Contain(Move("f5f4")) // Down one space
        .And.Contain(Move("f5f3")) // Down two spaces
        .And.Contain(Move("f5f2")); // Down three spaces to capture white pawn - can't go further down until it is captured
    }

    // <summary> 
    // Sets up the black king to be in check by the white rook. Checks and verifies all possible moves the king can do while in check.
    // </summary>
    [Fact]
    public void BlackKingCheckedByWhiteRook_KingCheckTest()
    {
        ChessBoard b = CreateBoardFromMoves("h2h4", "f7f5", "h1h3", "e8f7", "g1f3", "f7e6", "h3g3", "g7g6", "g3g4", "f8g7", "b2b3", "b7b6", "c1a3", "c8b7", "g4e4");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(6, "There should only be 6 moves possible on the preset board while the king is in check.");
        var blackKingMoves = GetMovesAtPosition(possMoves, Pos("e6"));
        blackKingMoves.Should().HaveCount(3, "Black king should have three possible moves from here. ").And.Contain(Move("e6f7")) // Move diagonally up-right one
        .And.Contain(Move("e6d5")) // Move diagonally down-left one
        .And.Contain(Move("e6f6")) // Move right one
        .And.NotContain(Move("e6d6")) // Cannot move left one - gets in line of white bishop
        .And.NotContain(Move("e6e5")); // Cannot move down one - remains in line of white rook
        // Any other move by the king would be blocked by a friendly piece
        var right_blackBishopMoves = GetMovesAtPosition(possMoves, Pos("g7"));
        right_blackBishopMoves.Should().HaveCount(1, "Black bishop right should only be able to move to protect the king").And.Contain(Move("g7e5")); // Moves the right black bishop in front of the king
        var left_blackBishopMoves = GetMovesAtPosition(possMoves, Pos("b7"));
        left_blackBishopMoves.Should().HaveCount(1, "Black bishop left should only be able to move to protect the king").And.Contain(Move("b7e4")); // Moves the left black bishop to the white rook's spot, capturing the rook and taking the king out of check.
        var close_blackPawnMoves = GetMovesAtPosition(possMoves, Pos("f5"));
        close_blackPawnMoves.Should().HaveCount(1, "Black pawn should only be able to move to protect the king").And.Contain(Move("f5e4")); // Moves diagonally down-left to capture the white rook and taking the king out of check.
    }

    // ==============================================================================================================
    // We can throw and output current board state by trying to apply a move for a side when it is not its turn
    /// <summary>
    /// Checks every position of a new ChessBoard to ensure that pieces have been correctly initialized.
    /// </summary>
    [Fact]
    public void NewChessBoardCheckAllPositions()
    {
        ChessBoard b = new ChessBoard();
        List<ChessPieceType> listOfRoyalPieces = [ChessPieceType.Rook, ChessPieceType.Knight, ChessPieceType.Bishop, ChessPieceType.Queen, ChessPieceType.King, ChessPieceType.Bishop, ChessPieceType.Knight, ChessPieceType.Rook];
        // Check white pieces; bottom and 2nd to last rows
        for (int i = 7; i >= 6; i--)
        {
            for (int j = 0; j <= 7; j++)
            {
                // Check for color
                b.GetPieceAtPosition(Pos(i, j)).Player.Should().Be(1, $"Player 1 should be at position ({i},{j})");
                // Check for royal pieces
                if (i == 7)
                {
                    b.GetPieceAtPosition(Pos(i, j)).PieceType.Should().Be(listOfRoyalPieces[j], $"Player 1's {listOfRoyalPieces[j]} should be at position ({i},{j})");
                }
                // Check for pawns
                else if (i == 6)
                {
                    b.GetPieceAtPosition(Pos(i, j)).PieceType.Should().Be(ChessPieceType.Pawn, $"Player 1's {ChessPieceType.Pawn} should be at position ({i},{j})");
                }
            }
        }

        // Check black pieces; top and 2nd rows
        for (int i = 0; i <= 1; i++)
        {
            for (int j = 0; j <= 7; j++)
            {
                // Check for color
                b.GetPieceAtPosition(Pos(i, j)).Player.Should().Be(2, $"Player 2 should be at position ({i},{j})");
                // Check for royal pieces
                if (i == 0)
                {
                    b.GetPieceAtPosition(Pos(i, j)).PieceType.Should().Be(listOfRoyalPieces[j], $"Player 2's {listOfRoyalPieces[j]} should be at position ({i},{j})");
                }
                // Check for pawns
                else if (i == 1)
                {
                    b.GetPieceAtPosition(Pos(i, j)).PieceType.Should().Be(ChessPieceType.Pawn, $"Player 2's {ChessPieceType.Pawn} should be at position ({i},{j})");
                }
            }
        }

        // Check middle of the board; should be empty
        for (int i = 2; i <= 5; i++)
        {
            for (int j = 0; j <= 7; j++)
            {
                b.GetPieceAtPosition(Pos(i, j)).Player.Should().Be(0, $"Middle of the board should be empty at position ({i},{j})");
            }
        }

        // Test other properties
        b.CurrentPlayer.Should().Be(1, "Player 1 starts the game");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
    }

    /// <summary>
    /// Tests the ability to castle king side for white.
    /// </summary>
    [Fact]
    public void CastlingWhiteKingSideTest()
    {
        // Set up the board to allow white king to castle king side
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "f1c4", "f8c5", "g1h3", "g8h6");
        // White king should have a move for castling king side
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        kingMoves.Should().Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide), $"Player 1's {ChessPieceType.King} should have a move for {ChessMoveType.CastleKingSide}");
        // Apply the castling to king side for white
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        var king = b.GetPieceAtPosition(Pos("g1"));
        king.Player.Should().Be(1, $"Player 1's {ChessPieceType.King} performed {ChessMoveType.CastleKingSide} and moved to the right 2 spaces");
        king.PieceType.Should().Be(ChessPieceType.King, $"Player 1's {ChessPieceType.King} performed {ChessMoveType.CastleKingSide} and moved to the right 2 spaces");
        var rook = b.GetPieceAtPosition(Pos("f1"));
        rook.Player.Should().Be(1, $"Player 1's {ChessPieceType.Rook} performed {ChessMoveType.CastleKingSide} and moved to the left 2 spaces");
        rook.PieceType.Should().Be(ChessPieceType.Rook, $"Player 1's {ChessPieceType.Rook} performed {ChessMoveType.CastleKingSide} and moved to the left 2 spaces");
    }

    /// <summary>
    /// Tests the ability to undo castling on the king side for white.
    /// </summary>
    [Fact]
    public void UndoCastlingWhiteKingSideTest()
    {
        // Set up the board to allow white king to castle king side
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "f1c4", "f8c5", "g1h3", "g8h6");
        // White king should have a move for castling king side
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        kingMoves.Should().Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide), $"Player 1's {ChessPieceType.King} should have a move for {ChessMoveType.CastleKingSide}");
        // Apply the castling to king side for white
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        var king = b.GetPieceAtPosition(Pos("g1"));
        king.Player.Should().Be(1, $"Player 1's {ChessPieceType.King} performed {ChessMoveType.CastleKingSide} and moved to the right 2 spaces");
        king.PieceType.Should().Be(ChessPieceType.King, $"Player 1's {ChessPieceType.King} performed {ChessMoveType.CastleKingSide} and moved to the right 2 spaces");
        var rook = b.GetPieceAtPosition(Pos("f1"));
        rook.Player.Should().Be(1, $"Player 1's {ChessPieceType.Rook} performed {ChessMoveType.CastleKingSide} and moved to the left 2 spaces");
        rook.PieceType.Should().Be(ChessPieceType.Rook, $"Player 1's {ChessPieceType.Rook} performed {ChessMoveType.CastleKingSide} and moved to the left 2 spaces");
        // Try to undo the castling
        b.UndoLastMove();
        king = b.GetPieceAtPosition(Pos("e1"));
        king.Player.Should().Be(1, $"after UndoLastMove, Player 1's {ChessPieceType.King} should be at e1 after undoing {ChessMoveType.CastleKingSide}");
        king.PieceType.Should().Be(ChessPieceType.King, $"after UndoLastMove, Player 1's {ChessPieceType.King} should be at e1 after undoing {ChessMoveType.CastleKingSide}");
        rook = b.GetPieceAtPosition(Pos("h1"));
        rook.Player.Should().Be(1, $"after UndoLastMove, Player 1's {ChessPieceType.Rook} should be at h1 after undoing {ChessMoveType.CastleKingSide}");
        rook.PieceType.Should().Be(ChessPieceType.Rook, $"after UndoLastMove, Player 1's {ChessPieceType.Rook} should be at h1 after undoing {ChessMoveType.CastleKingSide}");
        b.CurrentPlayer.Should().Be(1, $"after undoing {ChessMoveType.CastleKingSide}, it should be Player 1's turn again");
    }

    /// <summary>
    /// Tests the ability to undo a check by putting the black king in check.
    /// </summary>
    [Fact]
    public void UndoBlackKingInCheckTest()
    {
        // Set up the board so that black king is put in check by the white bishop
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d2d4", "d7d5", "f1b5");
        b.IsCheck.Should().BeTrue($"Player 1's {ChessPieceType.Bishop} is threatening Player 2's {ChessPieceType.King}");
        b.UndoLastMove();
        b.IsCheck.Should().BeFalse($"after UndoLastMove, Player 1's {ChessPieceType.Bishop} is no longer threatening Player 2's {ChessPieceType.King}");
        b.CurrentPlayer.Should().Be(1, $"after undoing the move that threatens Player 2's {ChessPieceType.King} with Player 1's {ChessPieceType.Bishop}, it should be Player 1's turn again");
        var empty = b.GetPieceAtPosition(Pos("b5"));
        empty.Player.Should().Be(0, $"position b5 should be empty after undoing the previous move and moving Player 1's {ChessPieceType.Bishop} back to f1");
        var bishop = b.GetPieceAtPosition(Pos("f1"));
        bishop.Player.Should().Be(1, $"Player 1's {ChessPieceType.Bishop} should be moved back to f1 after UndoLastMove");
        bishop.PieceType.Should().Be(ChessPieceType.Bishop, $"Player 1's {ChessPieceType.Bishop} should be moved back to f1 after UndoLastMove");
    }

    /// <summary>
    /// Validates that the black king correctly reports its possible moves by putting it in a position where it only has one possible move.
    /// </summary>
    [Fact]
    public void GetPossibleMovesBlackKingTest()
    {
        // Move pawn in front of black king down; black king should only have 1 move (down)
        ChessBoard b = CreateBoardFromMoves("d2d4", "e7e5", "c2c4");
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e8"));
        kingMoves.Should().HaveCount(1, $"Player 2's {ChessPieceType.King} is surrounded by its pieces except for below").And.Contain(Move("e8e7"), $"Player 2's {ChessPieceType.King} can only move down");
    }

    /// <summary>
    /// Puts the black king in check and validates the possible moves for black.
    /// </summary>
    [Fact]
    public void GetPossibleMovesBlackKingInCheckTest()
    {
        // Set up the board so that black king is put in check by the white bishop
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "d2d4", "d7d5", "f1b5");
        b.IsCheck.Should().BeTrue($"Player 1's {ChessPieceType.Bishop} is threatening Player 2's {ChessPieceType.King}");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(6, "there are 6 possible moves that Player 2 has to get out of check: (b8, d7), (b8, c6), (c8, d7), (d8, d7), (e8, e7), (c7, c6)").And.BeEquivalentTo([Move("b8d7"), Move("b8c6"), Move("c8d7"), Move("d8d7"), Move("e8e7"), Move("c7c6")], "there are 6 possible moves that Player 2 has to get out of check: (b8, d7), (b8, c6), (c8, d7), (d8, d7), (e8, e7), (c7, c6)");
    }

    /// <summary>
    ///	Test the initial starting board state:
    /// - Verify each player has 8 pawns, 2 rooks, 2 knights, 2 bishops, 1 queen, and 1 king
    /// - Confirm there are exactly 20 possible moves (16 pawns and 4 knight moves) for each player
    /// </summary>
    [Fact]
    public void InitialBoard__From_mochi52()
    {
        ChessBoard b = new ChessBoard();
        // Verify the correct number of pieces for each player and the piece has to be at the correct location
        // Check player 1, 6 kinds of Pieces
        b.GetPositionsOfPiece(ChessPieceType.King, 1).Should().HaveCount(1, "Player 1 should only have 1 King at e1.").And.Contain(Pos("e1"));
        b.GetPositionsOfPiece(ChessPieceType.Queen, 1).Should().HaveCount(1, "Player 1 should only have 1 Queen at d1.").And.Contain(Pos("d1"));
        b.GetPositionsOfPiece(ChessPieceType.Bishop, 1).Should().HaveCount(2, "Player 1 should only have 2 Bishops at c1 and f1.").And.Contain(Pos("c1")).And.Contain(Pos("f1"));
        b.GetPositionsOfPiece(ChessPieceType.Knight, 1).Should().HaveCount(2, "Player 1 should have 2 Knights at b1 and g1.").And.Contain(Pos("b1")).And.Contain(Pos("g1"));
        b.GetPositionsOfPiece(ChessPieceType.Rook, 1).Should().HaveCount(2, "Player 1 should have 2 Rooks at a1 and h1.").And.Contain(Pos("a1")).And.Contain(Pos("h1"));
        b.GetPositionsOfPiece(ChessPieceType.Pawn, 1).Should().BeEquivalentTo(new[] { Pos("a2"), Pos("b2"), Pos("c2"), Pos("d2"), Pos("e2"), Pos("f2"), Pos("g2"), Pos("h2") }, "Player 1 should have 8 Pawns in the correct positions from a2 -> h2.");
        // Check player 2, 6 kinds of Pieces
        b.GetPositionsOfPiece(ChessPieceType.King, 2).Should().HaveCount(1, "Player 2 should only have 1 King at e8.").And.Contain(Pos("e8"));
        b.GetPositionsOfPiece(ChessPieceType.Queen, 2).Should().HaveCount(1, "Player 2 should only have 1 Queen at d8.").And.Contain(Pos("d8"));
        b.GetPositionsOfPiece(ChessPieceType.Bishop, 2).Should().HaveCount(2, "Player 2 should only have 2 Bishops at c8 and f8.").And.Contain(Pos("c8")).And.Contain(Pos("f8"));
        b.GetPositionsOfPiece(ChessPieceType.Knight, 2).Should().HaveCount(2, "Player 2 should have 2 Knights at b8 and g8.").And.Contain(Pos("b8")).And.Contain(Pos("g8"));
        b.GetPositionsOfPiece(ChessPieceType.Rook, 2).Should().HaveCount(2, "Player 2 should have 2 Rooks at a8 and h8.").And.Contain(Pos("a8")).And.Contain(Pos("h8"));
        b.GetPositionsOfPiece(ChessPieceType.Pawn, 2).Should().BeEquivalentTo(new[] { Pos("a7"), Pos("b7"), Pos("c7"), Pos("d7"), Pos("e7"), Pos("f7"), Pos("g7"), Pos("h7") }, "Player 2 should have 8 Pawns in the correct positions from a7 -> h7.");
        // Confirm there are 20 possible moves for Player 1 
        b.GetPossibleMoves().Should().BeEquivalentTo(new[] { // Pawn moves - 16 total (8 pawns × 2 possible moves each)
        Move("a2a3"), Move("a2a4"), Move("b2b3"), Move("b2b4"), Move("c2c3"), Move("c2c4"), Move("d2d3"), Move("d2d4"), Move("e2e3"), Move("e2e4"), Move("f2f3"), Move("f2f4"), Move("g2g3"), Move("g2g4"), Move("h2h3"), Move("h2h4"), // Knight moves - 4 total (2 knights × 2 possible moves each)
        Move("b1a3"), Move("b1c3"), Move("g1f3"), Move("g1h3") }, "The initial possible moves should match the expected collection of moves");
    }

    /// <summary>
    /// Test a tricky move: King Side Castling as Player 1
    /// Undo the move and check
    /// </summary>
    [Fact]
    public void KingSideCastling_Player1()
    {
        // Apply the sequences of move to open path for castling
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "g1f3", "b8c6", // Move knight
 "f1c4", "f8c5" // Move bishop, clearing castling path
        );
        // Make sure there are no piece between King and Rook
        b.PositionIsEmpty(Pos("f1")).Should().BeTrue("f1 should be empty");
        b.PositionIsEmpty(Pos("g1")).Should().BeTrue("g1 should be empty");
        // Make sure King and Rook has not been moved
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "King should be at position e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "Rook should be at position h1");
        b.GetPossibleMoves().Should().Contain(m => m.MoveType == ChessMoveType.CastleKingSide, "Player 1 should have the option to perform King Side Castling.");
        // Make sure the King is not in check
        b.IsCheck.Should().BeFalse("Check should be false");
        // Apply the King Side Castling move
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "Piece type should be Rook after castling.");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "Piece type should be King after castling.");
        // Undo the move
        b.UndoLastMove();
        b.PositionIsEmpty(Pos("f1")).Should().BeTrue("f1 should be empty after undo last move");
        b.PositionIsEmpty(Pos("g1")).Should().BeTrue("g1 should be empty after undo last move");
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "King should be at position e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "Rook should be at position h1");
    }

    /// <summary>
    /// Test a tricky move: Queen Side Castling as Player 2
    /// Undo the move and check
    /// </summary>
    [Fact]
    public void QueenSideCastling_Player2()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d5", // Open center
 "g1f3", "b8c6", // Develop knight
 "d2d4", "c8f5", // Develop bishop
 "b1c3", "d8d7", // Move queen
 "f1d3" // Now Black can castle queenside with "e8c8"
        );
        // Make sure there are no pieces between King and Rook
        b.PositionIsEmpty(Pos("b8")).Should().BeTrue("b8 should be empty");
        b.PositionIsEmpty(Pos("c8")).Should().BeTrue("c8 should be empty");
        b.PositionIsEmpty(Pos("d8")).Should().BeTrue("d8 should be empty");
        // Make sure King and Rook hasn't moved
        b.GetPieceAtPosition(Pos("a8")).PieceType.Should().Be(ChessPieceType.Rook, "Rook should be at position a8");
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "King should be at position e8");
        // Make sure the King is not in check
        b.IsCheck.Should().BeFalse("Check should be false");
        // Apply the castling
        Apply(b, Move(Pos("e8"), Pos("c8"), ChessMoveType.CastleQueenSide));
        b.GetPieceAtPosition(Pos("d8")).PieceType.Should().Be(ChessPieceType.Rook, "Piece type should be Rook after castling.");
        b.GetPieceAtPosition(Pos("c8")).PieceType.Should().Be(ChessPieceType.King, "Piece type should be King after castling.");
        // Undo last move
        b.UndoLastMove();
        b.PositionIsEmpty(Pos("b8")).Should().BeTrue("b8 should be empty after undo last move");
        b.PositionIsEmpty(Pos("c8")).Should().BeTrue("c8 should be empty after undo last move");
        b.PositionIsEmpty(Pos("d8")).Should().BeTrue("d8 should be empty after undo last move");
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "King should be at position e8");
        b.GetPieceAtPosition(Pos("a8")).PieceType.Should().Be(ChessPieceType.Rook, "Rook should be at position a8");
    }

    /// <summary>
    /// Test scenarios: Player 2's Bishop Capture Player1's Pawn 
    /// </summary>
    [Fact]
    public void BishopCapturePawn()
    {
        ChessBoard b = CreateBoardFromMoves("g1f3", "g7g5", "g2g4", "d7d5", "f3d4", "c8g4" // Black Bishop capture White Pawn
        );
        // Make sure the piece at g4 is a Player 2's bishop
        b.GetPieceAtPosition(Pos("g4")).PieceType.Should().Be(ChessPieceType.Bishop, "Bishop captured Pawn diagonally.");
        b.GetPieceAtPosition(Pos("g4")).Player.Should().Be(2, "Player 2 took Player 1's pawn diagonally");
        // Check the advantage
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "Player 2 took Player 1's pawn diagonally");
        // Undo last move
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "The game should be at neutral after undoing last move");
    }

    /// <summary>
    /// Test scenario: Player 2 promote a Pawn to Queen, put Player 1's King in check
    /// </summary>
    [Fact]
    public void PawnPromoteToQueen()
    {
        ChessBoard b = CreateBoardFromMoves("g1f3", "g7g5", "g2g4", "d7d5", "f3d4", "c8g4", // Black Bishop capture White Pawn
 "f1h3", "g4f3", "e2e4", "g5g4", "e4e5", "g4g3", "e5e6", "g3g2", "d4b3", "g2g1queen" // Black Pawn promote to Queen, White King should be in Check
        );
        // Check if the piece at g1 is a Black Queen
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.Queen, "Pawn promote to Queen");
        b.GetPieceAtPosition(Pos("g1")).Player.Should().Be(2, "Player 2 promoted Pawn to Queen");
        // Check if the King is in Check
        b.IsCheck.Should().BeTrue("White King should be in check");
    }

    /// <summary>
    /// Test scenario: When White King is in check, what are the possible move to resolve it
    /// </summary>
    [Fact]
    public void TestPossibleMoveToResolveCheck()
    {
        ChessBoard b = CreateBoardFromMoves("g1f3", "g7g5", "g2g4", "d7d5", "f3d4", "c8g4", // Black Bishop capture White Pawn
 "f1h3", "g4f3", "e2e4", "g5g4", "e4e5", "g4g3", "e5e6", "g3g2", "d4b3", "g2g1queen" // Black Pawn promote to Queen, White King should be in Check
        );
        // Check White Rook's possible moves
        var possibleMoves = b.GetPossibleMoves();
        GetMovesAtPosition(possibleMoves, Pos("h1")).Should().Contain(Move("h1g1"), "White Rook should be able to capture Black Queen");
        // Apply the move
        Apply(b, Move("h1g1"));
        // Check Rook position
        b.PositionIsEmpty(Pos("h1")).Should().BeTrue();
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.Rook, "Rook should be at position g1");
        // Make sure King is not in check anymore
        b.IsCheck.Should().BeFalse("White King should not be in check");
    }

    // KINOKO TEST CASE START HERE
    // TEST CASE GENERAL PRINCIPLE TO FOLLOW - kinoko
    // 1. get possible move
    // 2. check possible move for pieces
    //	- check count
    //	- check if it contain/ not contain certain move
    // 3. If take other pieces : check for player advantage
    // 4. Check for pieces type
    // 5. Check player
    // NOTE:
    // requirement : 1 tc init board, 2 tc  tricky move (could do all 3), 2 tc  undoLastMove, 1 tc  validating, 1 tc king in check
    // 7 tc in total
    // starting board (1 test case)
    // TEST CASE 1: initial starting board state - checking the first turn is white
    [Fact]
    public void InitNewBoard()
    {
        ChessBoard b = new ChessBoard();
        var possMove = b.GetPossibleMoves();
        b.CurrentPlayer.Should().Be(1, "White turn, player 1");
        b.GetPieceAtPosition(Pos("c1")).Player.Should().Be(1, "white bishop, should be player 1");
        b.GetPieceAtPosition(Pos("h8")).Player.Should().Be(2, "black rook, should be player 2");
        b.GetPieceAtPosition(Pos("g7")).PieceType.Should().Be(ChessPieceType.Pawn);
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King);
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No thing happen, so advantage is 0-0");
    }

    // undo last move (2 test case)
    // TEST CASE 2: undo king side castling 0-0 | white move 
    [Fact]
    public void UndoKingSideCastling()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "g1f3", "d7d6", "f1d3", "b8c6");
        // apply castling move
        var possMove = b.GetPossibleMoves();
        var castlingExpected = GetMovesAtPosition(possMove, Pos("e1")); // get possible move from king
        castlingExpected.Should().HaveCount(3, "King contained 3 possible move").And.Contain(Move("e1g1")).And.Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        // Apply the king side castling
        Apply(b, Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
        // check the king and root pieces type at that position
        var King = b.GetPieceAtPosition(Pos("g1"));
        King.Player.Should().Be(1, "king perform the king side castling move");
        King.PieceType.Should().Be(ChessPieceType.King);
        var KingRook = b.GetPieceAtPosition(Pos("f1"));
        KingRook.Player.Should().Be(1, "Root perform the king side castling move");
        KingRook.PieceType.Should().Be(ChessPieceType.Rook);
        // UNDO THE MOVE AND CHECK THE BOARD STATE
        b.UndoLastMove();
        //King = b.GetPieceAtPosition(Pos("e1"));
        King.Player.Should().Be(1);
        King.PieceType.Should().Be(ChessPieceType.King);
        //KingRook = b.GetPieceAtPosition(Pos("h1"));
        KingRook.Player.Should().Be(1);
        KingRook.PieceType.Should().Be(ChessPieceType.Rook);
        //check if it still contain king side castling move
        castlingExpected.Should().HaveCount(3, "King contained 3 possible move").And.Contain(Move("e1g1")).And.Contain(Move(Pos("e1"), Pos("g1"), ChessMoveType.CastleKingSide));
    }

    // TEST CASE 3: UNDO FOOL'S MATE | black move
    [Fact]
    public void UndoFoolMate()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "g7g5", "b1c3", "f7f5");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "No caputure any pieces yet");
        b.CurrentPlayer.Should().Be(1, "White turn"); //should be white turn
        var possMove = b.GetPossibleMoves();
        var QueenMove = GetMovesAtPosition(possMove, Pos("d1"));
        QueenMove.Should().HaveCount(4, "Queen have 4 possible moves").And.Contain(Move("d1h5")); // check if it contain the fool mate move
        // check the black king
        var BlackKing = b.GetPieceAtPosition(Pos("e8"));
        BlackKing.Player.Should().Be(2, "The black king");
        BlackKing.PieceType.Should().Be(ChessPieceType.King);
        //check for checkmate
        b.IsCheck.Should().Be(false);
        b.IsFinished.Should().Be(false);
        b.IsCheckmate.Should().Be(false);
        Apply(b, Move("a2a3"));
        possMove = b.GetPossibleMoves();
        //possMove= b.GetPossibleMoves();
        var BlackKingMove = GetMovesAtPosition(possMove, Pos("e8"));
        // should return 1
        BlackKingMove.Should().HaveCount(1, "Black have one possible move: e8f7");
        // ?[ERORR] test case should pass with 1 
        //APPLY QUEEN MOVE FOR FOOL MATE
        //check for the checkmate
        Apply(b, Move("a7a6"));
        Apply(b, Move("d1h5"));
        possMove = b.GetPossibleMoves();
        var Queen = b.GetPieceAtPosition(Pos("h5"));
        Queen.Player.Should().Be(1, "White player");
        Queen.PieceType.Should().Be(ChessPieceType.Queen);
        // update possible move for the black king
        BlackKingMove = GetMovesAtPosition(possMove, Pos("e8"));
        BlackKingMove.Should().HaveCount(0, "Black king in check and have no legal move left");
        // check for checkmate
        b.IsCheck.Should().Be(true);
        b.IsFinished.Should().Be(true);
        b.IsCheckmate.Should().Be(true);
        // UNDO THE MOVE QUEEN MOVE
        b.UndoLastMove();
        //check if the queen back in d1
        Queen = b.GetPieceAtPosition(Pos("d1"));
        Queen.PieceType.Should().Be(ChessPieceType.Queen); // check queen type
        QueenMove.Should().HaveCount(4, "Queen have 4 possible moves").And.Contain(Move("d1h5")); // recheck the queen undo move, still have 4 possible moves
        // check if the flag checkmate is undo
        b.IsCheck.Should().Be(false);
        b.IsFinished.Should().Be(false);
        b.IsCheckmate.Should().Be(false);
    }

    //tricky move set : (1 test case)
    // TEST CASE 4 : QUEEN SIDE CASTLING 0-0-0 | white move
    // ref game from Rashid Nezhmetdinov
    [Fact]
    public void QueenSideCastling()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "g1f3", "d7d6", "d2d4", "c5d4", // black pawn c5 capture white pawn d4
 "f3d4", // white pawn f3 capture black pawn d4
 "g8f6", // black knigth move
 "b1c3", "a7a6", "c1g5", // move white bishop to g5
 "e7e6", "d1f3", // move white queen to f3
 "f8e7" // move black bishop
        );
        var possMove = b.GetPossibleMoves();
        var QueenCastlingExpected = GetMovesAtPosition(possMove, Pos("e1"));
        QueenCastlingExpected.Should().HaveCount(4, "White King should have 4 possible move").And.Contain(Move("e1c1")).And.Contain(Move(Pos("e1"), Pos("c1"), ChessMoveType.CastleQueenSide));
        // checking king and rook pos before castling
        var WhiteKing = b.GetPieceAtPosition(Pos("e1"));
        WhiteKing.Player.Should().Be(1, "White pieces"); // check player
        WhiteKing.PieceType.Should().Be(ChessPieceType.King); //check type
        var WhiteQueenRook = b.GetPieceAtPosition(Pos("a1"));
        WhiteQueenRook.Player.Should().Be(1, "White pieces");
        WhiteQueenRook.PieceType.Should().Be(ChessPieceType.Rook);
        // apply queen side castling
        Apply(b, Move(Pos("e1"), Pos("c1"), ChessMoveType.CastleQueenSide));
        // check king and root pos after queen side castling
        //king
        WhiteKing = b.GetPieceAtPosition(Pos("c1"));
        WhiteKing.Player.Should().Be(1, "White pieces"); // check player
        WhiteKing.PieceType.Should().Be(ChessPieceType.King); //check type
        // root
        WhiteQueenRook = b.GetPieceAtPosition(Pos("d1"));
        WhiteQueenRook.Player.Should().Be(1, "White pieces");
        WhiteQueenRook.PieceType.Should().Be(ChessPieceType.Rook);
    }

    // get possible move (1 test case)
    // note : some function randomize the piece
    // TEST CASE 5 :GET POSSIBLE MOVE
    // ref game :  immortal game 1851
    [Fact]
    public void ImmortalGamePossibleMoveCheck()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "f2f4", "e5f4", //black pawn capture white pawn at f4 [exf4]
 "f1c4", //develop white king bishop [Bishop's Gambit]
 "d8h4", // black king check mate the white king [check diagonal]
 "e1f1", // white king run away [2 possible moves] [up or right]
 "b7b5", "c4b5", // white bishop capture black pawn
 "g8f6", "g1f3", "h4h6", "d2d3", "f6h5", "f3h4", "h6g5", "h4f5", "c7c6", "g2g4", "h5f6", "h1g1", "c6b5", //black pawn capture white bishop
 "h2h4", "g5g6", "h4h5", "g6g5", "d1f3", // move white queen up to f3
 "f6g8", // move black knight back
 "c1f4", // white bishop capture black pawn, pin the black queen
 "g5f6", // black queen move back
 "b1c3", "f8c5", "c3d5", "f6b2", // black Queen capture pawn at b2
 "f4d6", // dubious move: bishop to d6
 "c5g1", // ROOK SARCIFICE [?] - BLACK BISHOP CAPTURE WHITE ROOK
 "e4e5", "b2a1", // ROOK SARCIFICE [?] - BLACK QUEEN CAPTURE WHITE ROOK
 "f1e2", "b8a6", "f5g7", // white knight check mate the king
 "e8d8", // black king run away
 "f3f6", // WHITE QUEEN SACRIFICE [!!!]
 "g8f6" // BLACK KNIGHT CAPTURE WHITE QUEEN
        );
        // WHITE TURN 
        var possMove = b.GetPossibleMoves();
        int KINOKO_ID = 029895591;
        int randomSeed = (KINOKO_ID % 5) + 1;
        //Apply(b, Move("a1a2")); // apply illegal move to see the chess board
        //int randomSeed = 5;
        switch (randomSeed)
        {
            case 1: // test for rooks [only 2 black rook left]
                Apply(b, Move("c2c3")); // for the sake of testing white rook
                possMove = b.GetPossibleMoves();
                var BlackQueenSide_Rook = GetMovesAtPosition(possMove, Pos("a8"));
                var BlackKingSide_Rook = GetMovesAtPosition(possMove, Pos("h8"));
                //Console.WriteLine("Black King Side Rook possible move: ", BlackKingSide_Rook);
                // check for black rook possible move
                BlackKingSide_Rook.Should().HaveCount(3, "e8,f8,g8").And.Contain(Move("h8g8")).And.Contain(Move("h8f8")).And.Contain(Move("h8e8"));
                BlackQueenSide_Rook.Should().HaveCount(1, "b8").And.Contain(Move("a8b8"));
                break;
            case 2:
                // test white knights
                var White_Knight1 = GetMovesAtPosition(possMove, Pos("g7"));
                var White_Knight2 = GetMovesAtPosition(possMove, Pos("d5"));
                White_Knight1.Should().HaveCount(3, "e8,e6,f5");
                White_Knight2.Should().HaveCount(8, "c7,b6,b4,c3,e3,f4,e7,f6[capture K]").And.Contain(Move("d5f6"));
                // apply move to test black knight
                Apply(b, Move("c2c3"));
                possMove = b.GetPossibleMoves(); //update the possible move dicts
                var Black_Knight1 = GetMovesAtPosition(possMove, Pos("a6"));
                var Black_Knight2 = GetMovesAtPosition(possMove, Pos("f6"));
                Black_Knight1.Should().HaveCount(4, "b4,b8,c7,c5");
                Black_Knight2.Should().HaveCount(6, "g8,e8,e4,g4,g5[capture pawn],d5[capture K]").And.Contain(Move("f6h5")).And.Contain(Move("f6g4")).And.Contain(Move("f6d5"));
                break;
            case 3: // test queens
                // check for black queen
                Apply(b, Move("c2c3"));
                //Apply(b, Move("h8h7")); //apply illegal to see the board 
                possMove = b.GetPossibleMoves();
                var BlackQueen = GetMovesAtPosition(possMove, Pos("a1"));
                BlackQueen.Should().HaveCount(8, "have 8 possible move: a1,a2,b1,b2,c1,d1,e1,f1").And.Contain(Move("a1a2")).And.Contain(Move("a1c3")).And.NotContain(Move("a1g1"));
                // Undo move to check for the White Queen Move
                b.UndoLastMove(); //  undo c2c3
                b.UndoLastMove(); //  undo g8f6
                //possMove = b.GetPossibleMoves();
                var WhiteQueen = GetMovesAtPosition(possMove, Pos("f6"));
                WhiteQueen.Should().HaveCount(6, "have 6 possible move: e6,g6,g5,b4,f5,e7");
                break;
            case 4: // test bishop  [2 black and 1 white]
                var White_Bishop1 = GetMovesAtPosition(possMove, Pos("d6"));
                White_Bishop1.Should().HaveCount(7, "7 moves");
                // apply move to test for black turn
                Apply(b, Move("c2c3"));
                possMove = b.GetPossibleMoves();
                var Black_Bishop1 = GetMovesAtPosition(possMove, Pos("c8"));
                var Black_Bishop2 = GetMovesAtPosition(possMove, Pos("g1"));
                Black_Bishop1.Should().HaveCount(1, "b8");
                Black_Bishop2.Should().HaveCount(6, "f2,h2,e3,d4,c5,b6");
                break;
            default: // test king (otherwise)
                // test white king
                var WhiteKing = GetMovesAtPosition(possMove, Pos("e2"));
                WhiteKing.Should().HaveCount(2, "d2,f3");
                // apply move to test for black king
                Apply(b, Move("c2c3"));
                possMove = b.GetPossibleMoves();
                var BlackKing = GetMovesAtPosition(possMove, Pos("d8"));
                BlackKing.Should().HaveCount(0, "have no move");
                // test for check
                b.IsCheck.Should().Be(false);
                b.IsCheckmate.Should().Be(false);
                b.IsFinished.Should().Be(false);
                // apply check mate to check for black king checkmate and finished
                Apply(b, Move("b5b4")); // black turn
                Apply(b, Move("d6e7")); // bishop check mate [CHECK MATE, END GAME]
                BlackKing.Should().HaveCount(0, "have no move");
                b.IsCheck.Should().Be(true);
                b.IsCheckmate.Should().Be(true);
                b.IsFinished.Should().Be(true);
                break;
        }
    }

    // king in check (1 test case)
    // TEST CASE6 6: check form bishop and knight
    //game ref : legals mate chess
    [Fact]
    public void CheckAndValidateLegalMateGame()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "d7d6", "b1c3", "c8g4", "h2h3", "g4h5", "f3e5", //white K capture black P
 "h5d1", // WHITE QUEEN SARCIFIRE
 "c4f7" // move bishop check the king [white turn]
        );
        var possMove = b.GetPossibleMoves();
        b.IsCheck.Should().Be(true); // black king in check 
        b.IsCheckmate.Should().Be(false); // black king still have legal move left
        var BlackKingPieces = b.GetPieceAtPosition(Pos("e8"));
        BlackKingPieces.PieceType.Should().Be(ChessPieceType.King);
        var BlackKingMove = GetMovesAtPosition(possMove, Pos("e8")); // get possible move in black king
        possMove.Count().Should().Be(1, "There only one way for black king escape being in check");
        possMove.Should().BeEquivalentTo([Move("e8e7")]);
        BlackKingMove.Should().HaveCount(1, "only one way to run away, e8e7");
        // APPLY MOVE FOR KING TO RUN AWAY
        Apply(b, Move("e8e7"));
        Apply(b, Move("c3d5")); //white Knight checkmate the black king
        possMove = b.GetPossibleMoves();
        possMove.Count().Should().Be(0, "Black king checkmate");
        b.IsCheck.Should().Be(true);
        b.IsCheckmate.Should().Be(true);
        b.IsFinished.Should().Be(true);
    }

    //SOLUTION STARTS HERE
    /// <summary>
    /// Starting board sanity check: kings and side to move.
    /// (This is our ONLY test about the initial board state.)
    /// </summary>
    [Fact]
    public void StartingBoard_HasCorrectKingsAndTurn()
    {
        var b = new ChessBoard();
        b.CurrentPlayer.Should().Be(1, "White (Player 1) always moves first on a fresh board");
        b.GetPieceAtPosition(Pos(7, 4)).PieceType.Should().Be(ChessPieceType.King, "White's king should start on e1 (row 7, col 4)");
        b.GetPieceAtPosition(Pos(7, 4)).Player.Should().Be(1, "White's king belongs to Player 1");
        b.GetPieceAtPosition(Pos(0, 4)).PieceType.Should().Be(ChessPieceType.King, "Black's king should start on e8 (row 0, col 4)");
        b.GetPieceAtPosition(Pos(0, 4)).Player.Should().Be(2, "Black's king belongs to Player 2");
    }

    /// <summary>
    /// Tricky move (en passant) performed by Black, then undo.
    /// Sequence sets a Black pawn on c4 and enables en passant against White's b4.
    /// </summary>
    [Fact]
    public void BlackEnPassant_ThenUndo_RestoresBoard()
    {
        var b = CreateBoardFromMoves("h2h3", // White filler
 "c7c5", // Black push
 "h3h4", // White filler
 "c5c4", // Black pawn reaches c4
 "b2b4" // White double-push adjacent to c4 -> en passant enabled
        );
        var poss = b.GetPossibleMoves();
        var c4Moves = GetMovesAtPosition(poss, Pos("c4"));
        c4Moves.Should().Contain(Move(Pos("c4"), Pos("b3"), ChessMoveType.EnPassant), "Black may capture the b-pawn en passant from c4 to b3").And.Contain(Move("c4c3"), "Black can also step forward one to c3").And.HaveCount(2, "Those are the only two legal options from c4 in this position");
        // Apply en passant
        Apply(b, Move(Pos("c4"), Pos("b3"), ChessMoveType.EnPassant));
        b.GetPieceAtPosition(Pos("b3")).Should().Match<ChessPiece>(p => p.Player == 2 && p.PieceType == ChessPieceType.Pawn, "After en passant, Black's pawn must land on b3");
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(0, "The White pawn that advanced two squares is removed after en passant");
        // Undo en passant
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("c4")).Should().Match<ChessPiece>(p => p.Player == 2 && p.PieceType == ChessPieceType.Pawn, "Undo should return Black's pawn to c4");
        b.GetPieceAtPosition(Pos("b4")).Should().Match<ChessPiece>(p => p.Player == 1 && p.PieceType == ChessPieceType.Pawn, "Undo should restore White's pawn to b4");
        b.GetPieceAtPosition(Pos("b3")).Player.Should().Be(0, "Undo should clear b3 again");
        b.CurrentPlayer.Should().Be(2, "After undoing Black's last move, it is Black's turn again");
    }

    /// <summary>
    /// Tricky move (White king-side castling) and UndoLastMove().
    /// Clears f1 and g1, confirms castle is offered, applies it, checks piece squares, then undoes.
    /// </summary>
    [Fact]
    public void WhiteCastleKingSide_ThenUndo_RestoresState()
    {
        var b = CreateBoardFromMoves("e2e4", // free the diagonal to move the bishop
 "e7e5", "g1f3", // clear g1
 "b8c6", "f1e2", // clear f1
 "g8f6" // harmless
        );
        var poss = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(poss, Pos("e1"));
        kingMoves.Should().Contain(Move("e1g1"), "After clearing f1 and g1 with no attacks on the transit/target squares, White may castle king-side");
        Apply(b, "e1g1"); // castle
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "After O-O, White's king must be on g1");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "After O-O, White's h1 rook must land on f1");
        b.GetPieceAtPosition(Pos("e1")).Player.Should().Be(0, "After O-O, e1 should be empty");
        b.GetPieceAtPosition(Pos("h1")).Player.Should().Be(0, "After O-O, h1 should be empty");
        // Undo castling and verify rights restored
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "Undo should restore the king to e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "Undo should restore the rook to h1");
        b.GetPieceAtPosition(Pos("f1")).Player.Should().Be(0, "Undo should clear f1 again");
        b.GetPieceAtPosition(Pos("g1")).Player.Should().Be(0, "Undo should clear g1 again");
        // And castling should still be available again (since neither king nor rook remain 'moved')
        var poss2 = b.GetPossibleMoves();
        var kingMoves2 = GetMovesAtPosition(poss2, Pos("e1"));
        kingMoves2.Should().Contain(Move("e1g1"), "After undo, White should still be allowed to castle again in the same position");
    }

    /// <summary>
    /// GetPossibleMoves() focus test (piece-specific). 
    /// The assignment says pick a piece based on (studentId % 5) + 1.
    /// Here we demonstrate with KNIGHTS (case = 2). If your ID maps to a different piece,
    /// change the piece & expected moves accordingly.
    /// This test uses the Black knight at g8 on the starting board (after one White tempo).
    /// </summary>
    [Fact]
    public void GetPossibleMoves_Knight_BlackG8_HasTwo()
    {
        var b = CreateBoardFromMoves("h2h3"); // give turn to Black
        var poss = b.GetPossibleMoves();
        var g8Moves = GetMovesAtPosition(poss, Pos("g8"));
        g8Moves.Should().BeEquivalentTo(new[] { Move("g8h6"), Move("g8f6") }, "From the initial setup, the Black knight on g8 can only hop to h6 or f6").And.HaveCount(2, "There are exactly two legal knight moves from g8 at start");
    }

    /// <summary>
    /// King-in-check validation: Fool's Mate (checkmate against White).
    /// Confirms there are no legal moves for White and the board flags checkmate/finished.
    /// </summary>
    [Fact]
    public void FoolsMate_CheckmateForWhite_NoLegalMoves()
    {
        var b = CreateBoardFromMoves("f2f3", "e7e5", "g2g4", "d8h4" // Qh4#
        );
        b.IsCheck.Should().BeTrue("White's king is in check in Fool's Mate");
        b.IsCheckmate.Should().BeTrue("Fool's Mate is a known forced checkmate");
        b.IsFinished.Should().BeTrue("A checkmated game is finished");
        b.CurrentPlayer.Should().Be(1, "After Black delivers mate, it would be White's turn—but there are no legal moves");
        b.GetPossibleMoves().Should().BeEmpty("Checkmate leaves the side to move with zero legal moves");
    }

    /// <summary>
    /// Tricky move (Black queen-side castling) and UndoLastMove().
    /// We clear b8, c8, and d8; ensure squares are safe; verify castle appears, apply it, then undo.
    /// </summary>
    [Fact]
    public void BlackCastleQueenSide_PresentApplyAndUndo()
    {
        var b = CreateBoardFromMoves("d2d4", // White filler
 "d7d5", // Free d7 so Black queen can vacate d8
 "c1g5", // White harmless
 "c8f5", // Clear c8 bishop
 "g2g3", // White harmless
 "d8d7", // Vacate d8
 "f1g2", // White harmless
 "b8a6", // Vacate b8 knight (b8 must be empty for O-O-O)
 "a2a3" // White filler so it's Black's move now
        );
        var poss = b.GetPossibleMoves();
        var e8Moves = GetMovesAtPosition(poss, Pos("e8"));
        e8Moves.Should().Contain(Move("e8c8"), "With b8, c8, and d8 empty and safe, Black should be able to castle queen-side");
        Apply(b, "e8c8"); // O-O-O
        b.GetPieceAtPosition(Pos("c8")).PieceType.Should().Be(ChessPieceType.King, "After O-O-O, Black's king must be on c8");
        b.GetPieceAtPosition(Pos("d8")).PieceType.Should().Be(ChessPieceType.Rook, "After O-O-O, Black's a8 rook must land on d8");
        b.GetPieceAtPosition(Pos("e8")).Player.Should().Be(0, "After O-O-O, e8 is vacated by the king");
        b.GetPieceAtPosition(Pos("a8")).Player.Should().Be(0, "After O-O-O, a8 is vacated by the rook");
        // Undo the castle
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "Undo should restore the king to e8");
        b.GetPieceAtPosition(Pos("a8")).PieceType.Should().Be(ChessPieceType.Rook, "Undo should restore the rook to a8");
        b.GetPieceAtPosition(Pos("c8")).Player.Should().Be(0, "Undo should clear c8 again");
        b.GetPieceAtPosition(Pos("d8")).Player.Should().Be(0, "Undo should clear d8 again");
        // Castle should be available again in the same position
        var poss2 = b.GetPossibleMoves();
        var e8Moves2 = GetMovesAtPosition(poss2, Pos("e8"));
        e8Moves2.Should().Contain(Move("e8c8"), "After undo, Black should still be allowed to castle queen-side again in the same position");
    }

    /// <summary>
    /// Can castle king side if there are no pieces between the king and rook
    /// </summary>
    [Fact]
    public void CastleKing()
    {
        // Given
        ChessBoard b = CreateBoardFromMoves("e2e4", "a7a6", // white pawn move 2, black pawn move 1
 "g1f3", "a6a5", // white knight move 1, black pawn move 1
 "f1e2", "a5a4" // white bishop move 1, black pawn move 1
        );
        // When
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        // Then
        kingMoves.Should().Contain(Move("e1g1"), "white shoudl be able to castle kingside");
        kingMoves.Should().NotContain(Move("e1c1"), "white should not be able to castle queenside");
        Apply(b, Move("e1g1"));
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "king should be at g1 after castling");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "rook should be at f1 after castling");
        b.GetPieceAtPosition(Pos("e1")).Player.Should().Be(0, "e1 should be empty after castling");
        b.GetPieceAtPosition(Pos("h1")).Player.Should().Be(0, "h1 should be empty after castling");
    }

    /// <summary>
    /// Knight has 2 possible moves at the start of a game
    /// </summary>
    [Fact]
    public void KnightMoveStart()
    {
        // Given
        ChessBoard b = new ChessBoard();
        // When
        var possMoves = b.GetPossibleMoves();
        var knightMoves = GetMovesAtPosition(possMoves, Pos("b1"));
        // Then
        knightMoves.Should().HaveCount(2, "knight should have 2 possible moves at start of game");
        knightMoves.Should().Contain(Move("b1a3"), "knight should be able to move to a3");
        knightMoves.Should().Contain(Move("b1c3"), "knight should be able to move to c3");
    }

    /// <summary>
    /// Bishop can move any amount diagnol until piece confronted
    /// </summary>
    [Fact]
    public void BishopCapture()
    {
        // Given
        ChessBoard b = CreateBoardFromMoves("b2b3", "g7g5", "c2c4", "f8g7", "b3b4");
        // When
        var possMoves = b.GetPossibleMoves();
        var expected = GetMovesAtPosition(possMoves, Pos("g7"));
        expected.Should().Contain(Move("g7a1"), "black bishop shoudl be able to capture white rook at a1");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
        // Then
        Apply(b, Move("g7a1"));
        b.GetPieceAtPosition(Pos("a1")).PieceType.Should().Be(ChessPieceType.Bishop, "black bishop should be at a1 after capture");
        b.CurrentAdvantage.Should().Be(Advantage(2, 5), "White lost a rook of value 5");
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "after undoing the bishop capture, advantage is neutral");
    }

    /// <summary>
    /// A bishop can move any amount diagonally until it confronts a piece
    /// student ID: 021867592 % 5 + 1 = 3 = bishop
    /// </summary>
    [Fact]
    public void BishopMoves()
    {
        // Given
        ChessBoard b = CreateBoardFromMoves("d2d4", "e7e5", "c1g5", "g8f6", "h2h4", "b8c6", "f2f4", "h7h6");
        var possMoves = b.GetPossibleMoves();
        var expected = GetMovesAtPosition(possMoves, Pos("g5"));
        expected.Should().Contain(Move("g5f6"), "white bishop should be able to capture knight at f6 and capture knight");
        expected.Should().Contain(Move("g5h6"), "white bishop should be able to move to h6 and capture pawn");
        expected.Should().HaveCount(2, "white bishop should have 2 possible moves");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "no operations have changed the advantage");
        Apply(b, Move("g5f6"));
        b.GetPieceAtPosition(Pos("f6")).PieceType.Should().Be(ChessPieceType.Bishop, "bishop should be at f6 after capture");
        b.CurrentAdvantage.Should().Be(Advantage(1, 3), "black lost a knight value of 3");
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "after undoing the bishop capture, advantage is neutral");
    }

    /// <summary>
    /// King is in checkmate and cannot move anywhere, also there are no other pieces to block or capture the checking piece
    /// </summary>
    [Fact]
    public void kingCheckmate()
    {
        // Given
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e5", "g2g4", "d8h4");
        // When
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        // Then 
        kingMoves.Should().BeEmpty("white king is in checkmate and cannot move anywhere, black wins");
    }

    [Fact]
    /// <summary>
    /// King is in check and must move out of check, cannot castle out of check
    /// </summary>
    public void KingInCheck__From_Nicktochme()
    {
        ChessBoard b = CreateBoardFromMoves("d2d4", "e7e5", "c1g5", "g8f6", "d1d3", "d7d5", "d3f5", "d8d7", "b1a3", "f8b4");
        var possMoves = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(possMoves, Pos("e1"));
        kingMoves.Should().Contain(Move("e1d1"), "white king should be able to move to d1 to get out of check").And.NotContain(Move("e1d2"), "white king cannot move to d2, it is an illegal move into check").And.NotContain(Move("e1c1"), "white king cannot castle during check").And.HaveCount(1, "white king should only have 1 possible move to get out of check");
    }

    // Tricky move: White king-side castling (small castle)
    // I clear f1 and g1, make sure e1g1 is available, apply it, then undo and check pieces went back
    [Fact]
    public void WhiteCastle_KingSide_ThenUndo()
    {
        // Free up f1 (bishop) and g1 (knight) first
        var b = CreateBoardFromMoves("g1f3", // white
 "a7a6", // black
 "e2e3", // free e2 so bishop can move there
 "a6a5", "f1e2", "a5a4");
        var poss = b.GetPossibleMoves();
        var kingMoves = GetMovesAtPosition(poss, Pos("e1"));
        kingMoves.Should().Contain(Move("e1g1"), "white should be able to castle king-side when f1/g1 are clear and nothing illegal is happening");
        // Do the castle and check where king/rook ended up
        Apply(b, "e1g1");
        b.GetPieceAtPosition(Pos("g1")).PieceType.Should().Be(ChessPieceType.King, "after castling, the king should be on g1");
        b.GetPieceAtPosition(Pos("f1")).PieceType.Should().Be(ChessPieceType.Rook, "after castling, the rook should be on f1");
        b.GetPieceAtPosition(Pos("e1")).Player.Should().Be(0, "e1 should be empty after the castle");
        b.GetPieceAtPosition(Pos("h1")).Player.Should().Be(0, "h1 should be empty because the rook moved");
        // Undo should put everything back exactly
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "undo should return the king to e1");
        b.GetPieceAtPosition(Pos("h1")).PieceType.Should().Be(ChessPieceType.Rook, "undo should return the rook to h1");
        b.GetPieceAtPosition(Pos("f1")).Player.Should().Be(0, "f1 should be cleared again after undo");
        b.GetPieceAtPosition(Pos("g1")).Player.Should().Be(0, "g1 should be cleared again after undo");
    }

    // Tricky move: Black king-side castling (small castle)
    // Same as above but for black. Also undo at the end
    [Fact]
    public void BlackCastle_KingSide_ThenUndo()
    {
        var b = CreateBoardFromMoves("a2a3", "g8f6", "b2b3", "e7e6", // free e7 so bishop can move there
 "c2c3", "f8e7", "h2h3" // white filler so it is Black’s turn again
        );
        var poss = b.GetPossibleMoves();
        var e8Moves = GetMovesAtPosition(poss, Pos("e8"));
        e8Moves.Should().Contain(Move("e8g8"), "black should be allowed to castle king-side when f8/g8 are cleared and conditions are ok");
        Apply(b, "e8g8");
        b.GetPieceAtPosition(Pos("g8")).PieceType.Should().Be(ChessPieceType.King, "after castling, the king should be on g8");
        b.GetPieceAtPosition(Pos("f8")).PieceType.Should().Be(ChessPieceType.Rook, "after castling, the rook should be on f8");
        b.GetPieceAtPosition(Pos("e8")).Player.Should().Be(0, "e8 should be empty after the castle");
        b.GetPieceAtPosition(Pos("h8")).Player.Should().Be(0, "h8 should be empty because the rook moved");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "undo should return the king to e8");
        b.GetPieceAtPosition(Pos("h8")).PieceType.Should().Be(ChessPieceType.Rook, "undo should return the rook to h8");
        b.GetPieceAtPosition(Pos("f8")).Player.Should().Be(0, "f8 should be cleared again after undo");
        b.GetPieceAtPosition(Pos("g8")).Player.Should().Be(0, "g8 should be cleared again after undo");
    }

    // Being in check should restrict legal moves to only those that get out of check
    // Here white plays Qxe5+ and black must respond properly (e.g., block with Bf8e7 or something equivalent)
    [Fact]
    public void InCheck_BlackMustPlayAResolvingMove()
    {
        var b = CreateBoardFromMoves("e2e4", "e7e5", "d1h5", "b8c6", "h5e5" // Qxe5+, check
        );
        b.IsCheck.Should().BeTrue("after Qxe5+, black should be in check");
        var poss = b.GetPossibleMoves();
        // A move that ignores the check (like a7a6) shouldn't be listed.
        poss.Should().NotContain(Move("a7a6"), "moves that do not resolve the check should not be legal when the king is in check");
        // At least one reasonable resolving move should be present (block with the bishop)
        poss.Should().Contain(Move("f8e7"), "blocking with Bf8-e7 resolves the queen check");
    }

    // Undo should restore pieces: there is a quick center trade and black's queen recaptures on d5
    // After undoing the queen move, the pawn should be back and the queen back to d8
    [Fact]
    public void Undo_AfterQueenRecapture_RestoresState()
    {
        var b = CreateBoardFromMoves("e2e4", "d7d5", "e4d5", // white pawn takes on d5
 "d8d5" // black queen recaptures d5
        );
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Queen, "after the recapture, the black queen should be sitting on d5");
        // now undo just the last move
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Pawn, "after undo, the white pawn should be back on d5");
        b.GetPieceAtPosition(Pos("d8")).PieceType.Should().Be(ChessPieceType.Queen, "after undo, the black queen should return to d8");
        b.GetPieceAtPosition(Pos("e4")).Player.Should().Be(0, "e4 should be empty again because the pawn is on d5 (not still on e4)");
    }

    // Tricky move + Undo: black performs "en passant" from a4 to b3 after white moves b2 to b4
    // Undo should put the black pawn back on a4 and the white pawn back on b4, and b3 empty
    [Fact]
    public void Black_EnPassant_CaptureAndUndo()
    {
        var b = CreateBoardFromMoves("h2h4", // white moves first
 "a7a5", // black advances a-pawn
 "h4h5", // white filler
 "a5a4" // black to a4, ready for "en passant" against b-pawn
        );
        Apply(b, "b2b4"); // white two-step to enable en passant
        var poss = b.GetPossibleMoves();
        var a4Moves = GetMovesAtPosition(poss, Pos("a4"));
        a4Moves.Should().Contain(Move(Pos("a4"), Pos("b3"), ChessMoveType.EnPassant), "after b2-b4, black pawn on a4 should be able to capture en passant on b3");
        a4Moves.Should().Contain(Move("a4a3"), "normal a4a3 forward move should also be available");
        // do the en passant
        Apply(b, Move(Pos("a4"), Pos("b3"), ChessMoveType.EnPassant));
        b.GetPieceAtPosition(Pos("b3")).Player.Should().Be(2, "black pawn should land on b3 after en passant");
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(0, "white pawn from b4 should be captured via en passant");
        // undo en passant: everything should go back
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("a4")).Player.Should().Be(2, "black pawn returns to a4 after undo");
        b.GetPieceAtPosition(Pos("b4")).Player.Should().Be(1, "white pawn returns to b4 after undo");
        b.GetPieceAtPosition(Pos("b3")).Player.Should().Be(0, "b3 should be empty again after undo");
    }

    // Piece-move generation test :I’m testing knights (case 2), from the starting board
    // The knight on b1 should only have a3 and c3.
    [Fact]
    public void KnightMoves_FromStart_B1OnlyHasA3orC3()
    {
        var b = new ChessBoard();
        var poss = b.GetPossibleMoves();
        var fromB1 = GetMovesAtPosition(poss, Pos("b1"));
        // exactly two: b1a3 and b1c3
        fromB1.Should().HaveCount(2, "from the start, the b1 knight can jump to a3 or c3 and nothing else").And.BeEquivalentTo(new[] { Move("b1a3"), Move("b1c3") }, "knight jumps over pieces so the only legal squares are a3 and c3 here");
    }

    [Fact]
    // Checking initial state of the board, and confirming the rooks are in the corners.
    public void InitialBoard__From_RileyDSt() // Involves both white player and black player
    {
        ChessBoard b = new ChessBoard();
        //White's Non-Pawn Pieces
        b.GetPieceAtPosition(Pos(7, 0)).Player.Should().Be(1, "White's piece is at a1");
        b.GetPieceAtPosition(Pos(7, 1)).Player.Should().Be(1, "White's piece is at b1");
        b.GetPieceAtPosition(Pos(7, 2)).Player.Should().Be(1, "White's piece is at c1");
        b.GetPieceAtPosition(Pos(7, 3)).Player.Should().Be(1, "White's piece is at d1");
        b.GetPieceAtPosition(Pos(7, 4)).Player.Should().Be(1, "White's piece is at e1");
        b.GetPieceAtPosition(Pos(7, 5)).Player.Should().Be(1, "White's piece is at f1");
        b.GetPieceAtPosition(Pos(7, 6)).Player.Should().Be(1, "White's piece is at g1");
        b.GetPieceAtPosition(Pos(7, 7)).Player.Should().Be(1, "White's piece is at h1");
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook is at a1");
        b.GetPieceAtPosition(Pos(7, 7)).PieceType.Should().Be(ChessPieceType.Rook, "White's other rook is at h1");
        //Black's Non-Pawn Pieces
        b.GetPieceAtPosition(Pos(0, 0)).Player.Should().Be(2, "Black's piece is at a8");
        b.GetPieceAtPosition(Pos(0, 1)).Player.Should().Be(2, "Black's piece is at b8");
        b.GetPieceAtPosition(Pos(0, 2)).Player.Should().Be(2, "Black's piece is at c8");
        b.GetPieceAtPosition(Pos(0, 3)).Player.Should().Be(2, "Black's piece is at d8");
        b.GetPieceAtPosition(Pos(0, 4)).Player.Should().Be(2, "Black's piece is at e8");
        b.GetPieceAtPosition(Pos(0, 5)).Player.Should().Be(2, "Black's piece is at f8");
        b.GetPieceAtPosition(Pos(0, 6)).Player.Should().Be(2, "Black's piece is at g8");
        b.GetPieceAtPosition(Pos(0, 7)).Player.Should().Be(2, "Black's piece is at h8");
        b.GetPieceAtPosition(Pos(0, 0)).PieceType.Should().Be(ChessPieceType.Rook, "Black's rook is at a8");
        b.GetPieceAtPosition(Pos(0, 7)).PieceType.Should().Be(ChessPieceType.Rook, "Black's other rook is at h8");
    }

    [Fact]
    // Tests for a successful castle of white's king and rook + inverse w/ undo
    public void Castling__From_RileyDSt() // Involves white player
    {
        // Clear out the space for the white rook & white king
        ChessBoard b = CreateBoardFromMoves("d2d3", "h7h6", "c1e3", "g7g6", "d1d2", "f7f6", "b1c3", "e7e6");
        // Verify that the rook and king are still at their starting position
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook should be at a1, which is its starting position");
        b.GetPieceAtPosition(Pos(7, 4)).PieceType.Should().Be(ChessPieceType.King, "White's king should be at e1, which is its starting position");
        // Check king's valid moves (Should be 2)
        var possMoves = b.GetPossibleMoves();
        var pos = Pos("e1");
        var movesAtPos = GetMovesAtPosition(possMoves, pos);
        movesAtPos.Should().HaveCount(2, "The king can either move one to the left or two to the left (activating the castling move)").And.BeEquivalentTo([Move(pos, pos.Translate(0, -1)), Move(pos, pos.Translate(0, -2))]);
        Apply(b, "e1c1");
        // Verify that the castling occured
        b.GetPieceAtPosition(Pos(7, 3)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook should be at d1, having jumped over white's king");
        b.GetPieceAtPosition(Pos(7, 2)).PieceType.Should().Be(ChessPieceType.King, "White's king should be at c1, having moved two spaces to the left");
        // Undo castling move to ensure it works backwards
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(1, "White's turn");
        b.GetPieceAtPosition(Pos(7, 0)).PieceType.Should().Be(ChessPieceType.Rook, "White's rook should be back at a1, its original starting position");
        b.GetPieceAtPosition(Pos(7, 4)).PieceType.Should().Be(ChessPieceType.King, "White's king should be back at e1, its original starting position");
    }

    [Fact]
    // Tests for all the possible moves a knight could make on a given chess board
    public void PossibleKnightMoves() // involves black player, 030468371 mod 5 + 1 = 2
    {
        ChessBoard b = CreateBoardFromMoves("b1c3", "c7c5", "c3e4", "d7d6", "g2g3", "g8f6", "e4c3", "f6e4", "a2a3");
        // Verify all the pieces which black knight's potential moves would interact with
        b.GetPieceAtPosition(Pos(6, 3)).PieceType.Should().Be(ChessPieceType.Pawn, "White's Pawn is at d2");
        b.GetPieceAtPosition(Pos(6, 5)).PieceType.Should().Be(ChessPieceType.Pawn, "White's Pawn is at f2");
        b.GetPieceAtPosition(Pos(5, 2)).PieceType.Should().Be(ChessPieceType.Knight, "White's Knight is at c3");
        b.GetPieceAtPosition(Pos(5, 6)).PieceType.Should().Be(ChessPieceType.Pawn, "White's Pawn is at c3");
        b.GetPieceAtPosition(Pos(3, 2)).Player.Should().Be(2, "Black's piece is at c5, and can't be taken by the black knight");
        b.GetPieceAtPosition(Pos(3, 2)).PieceType.Should().Be(ChessPieceType.Pawn, "Black's Pawn is at c5");
        b.GetPieceAtPosition(Pos(2, 3)).Player.Should().Be(2, "Black's piece is at d6, and can't be taken by the black knight");
        b.GetPieceAtPosition(Pos(2, 3)).PieceType.Should().Be(ChessPieceType.Pawn, "Black's Pawn is at d6");
        b.GetPieceAtPosition(Pos(2, 5)).Player.Should().Be(0, "f6 should be empty");
        b.GetPieceAtPosition(Pos(3, 6)).Player.Should().Be(0, "g5 should be empty");
        // Check black knight's possible moves
        var possMoves = b.GetPossibleMoves();
        var pos = Pos("e4");
        var movesAtPos = GetMovesAtPosition(possMoves, pos);
        movesAtPos.Should().HaveCount(6, "Knight not near edge --> max of 8 possible moves; two are occupied by black's pieces, therefore there are 6 available moves").And.BeEquivalentTo([Move(pos, pos.Translate(1, -2)), Move(pos, pos.Translate(2, -1)), Move(pos, pos.Translate(2, 1)), Move(pos, pos.Translate(1, 2)), Move(pos, pos.Translate(-1, 2)), Move(pos, pos.Translate(-2, 1))]);
    }

    [Fact]
    // Tests for black's king in a check state, as well as escaping check
    public void KingInCheck__From_RileyDSt() // involves black player
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "b1c3", "e8e7", "f2f3", "a7a6", "h2h3", "e7d6", "c3b5");
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(5, "4 moves that the king can do, 1 move that a nearby piece can do").And.BeEquivalentTo([Move("d6c6"), // King Option #1
 Move("d6e6"), // King Option #2
 Move("d6e7"), // King Option #3
 Move("d6c5"), // King Option #4
 Move("a6b5") // Pawn takes out White Knight
        ]);
        b.IsCheck.Should().BeTrue("Black's king is in immediate danger, therefore it is in check");
        b.IsCheckmate.Should().BeFalse("Black has options to escape check, so it is not a checkmate");
        // Move a piece to escape check
        Apply(b, "a6b5");
        b.IsCheck.Should().BeFalse("Black's king is not in immediate danger, therefore it is not in check");
    }

    [Fact]
    // Tests for a white pawn promoting into a queen + inverse w/ undo
    public void PawnPromotion() // Involves white player
    {
        ChessBoard b = CreateBoardFromMoves("b2b4", "c7c5", "b4c5", "b7b6", "c5b6", "a7a5", "b6b7", "b8c6");
        var possMoves = b.GetPossibleMoves();
        var pos = Pos("b7");
        var movesAtPos = GetMovesAtPosition(possMoves, pos);
        movesAtPos.Should().HaveCount(12, "Three possible squares, four promotion options per square").And.OnlyContain(m => m.MoveType == ChessMoveType.PawnPromote);
        // Pawn Promotion --> Queen
        Apply(b, "b7c8queen");
        b.GetPieceAtPosition(Pos(0, 2)).PieceType.Should().Be(ChessPieceType.Queen, "White's Pawn, which just took black's bishop, promoted to a queen");
        b.GetPieceAtPosition(Pos(0, 2)).Player.Should().Be(1, "White's Pawn --> Queen stayed as a white piece");
        // Undo promoting move to ensure it works backwards
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(1, "Back to White's turn");
        b.GetPieceAtPosition(Pos(1, 1)).PieceType.Should().Be(ChessPieceType.Pawn, "White's Pawn resets to square prior to promotion, which is b6");
        b.GetPieceAtPosition(Pos(0, 2)).PieceType.Should().Be(ChessPieceType.Bishop, "Black's Bishop returns to the board after being taken out by white's pawn");
    }

    [Fact]
    // Tests for a king which is in a checkmate state (in this case, it is black's king)
    public void KingCheckmate() // involves both white and black players
    {
        ChessBoard b = CreateBoardFromMoves("e2e3", "f7f6", "g1h3", // Used to pad turns since I'm testing for black's king being checkmated
 "g7g5");
        // Check current status of Black's king
        b.GetPieceAtPosition(Pos(0, 4)).Player.Should().Be(2, "Black's piece is at e8");
        b.GetPieceAtPosition(Pos(0, 4)).PieceType.Should().Be(ChessPieceType.King, "Black's piece at e8 is a King");
        b.IsCheck.Should().BeFalse("Black's king is not in immediate danger, therefore it is not in check");
        b.IsCheckmate.Should().BeFalse("Black's king is not in a check state, so by extension they are not in a checkmate state");
        //Enact the checkmate
        Apply(b, "d1h5"); //Queen moves into position to cause a check
        // Check updated status of board based on the current check status
        var possMoves = b.GetPossibleMoves();
        possMoves.Should().HaveCount(0, "No possible moves, validating the checkmate");
        b.IsCheck.Should().BeTrue("Black's king is in immediate danger, therefore it is in check");
        b.IsCheckmate.Should().BeTrue("Black has no available moves that would stop the check in one turn, so it is checkmate");
        // Undo the checkmate
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(1, "Back to White's turn");
        b.IsCheck.Should().BeFalse("Black's king is not in immediate danger, therefore it is not in check");
        b.IsCheckmate.Should().BeFalse("Black's king is not in a check state, so by extension they are not in a checkmate state");
    }

    [Fact]
    public void NewChessBoard__From_Theonlyhamstertoh()
    {
        ChessBoard b = new ChessBoard();
        b.CurrentPlayer.Should().Be(1, "Player 1 starts the game");
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "White's king at e1");
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "White's king at e1");
        b.GetPieceAtPosition(Pos("e8")).PieceType.Should().Be(ChessPieceType.King, "White's king at e1");
        b.GetPieceAtPosition(Pos("e1")).PieceType.Should().Be(ChessPieceType.King, "Black's king at e8");
        GetAllPiecesForPlayer(b, 1).Count().Should().Be(16, "Player 1 should have 16 pieces");
        GetAllPiecesForPlayer(b, 2).Count().Should().Be(16, "Player 2 should have 16 pieces");
        foreach (var pos in GetPositionsInRank(3))
        {
            b.GetPieceAtPosition(pos).PieceType.Should().Be(ChessPieceType.Empty, "There should be no pieces in rank 3");
        }

        foreach (var pos in GetPositionsInRank(4))
        {
            b.GetPieceAtPosition(pos).PieceType.Should().Be(ChessPieceType.Empty, "there should be no pieces in rank 6");
        }
    }

    [Fact]
    public void CheckmateBlackKingByWhiteQueen()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "f1c4", "f8d6", "d1f3", "b8c6");
        b.CurrentPlayer.Should().Be(1, "it should be white to move before delivering checkmate");
        Apply(b, Move(("f3f7")));
        b.CurrentPlayer.Should().Be(2, "after white's move, it should be black's turn");
        b.IsCheckmate.Should().Be(true, "black should be checkmate by white");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "queen captures pawn at f7, leading to checkmate");
        b.GetPieceAtPosition(Pos("f7")).PieceType.Should().Be(ChessPieceType.Queen, "queen should be at f7");
    }

    [Fact]
    public void PawnCapture_UndoMove()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "e7e5", "g1f3", "b8c6", "d2d4");
        b.CurrentPlayer.Should().Be(2, "it should be black to move after white moves pawn at d2 to d4");
        var possMoves = b.GetPossibleMoves();
        var pawnAtE5Moves = GetMovesAtPosition(possMoves, Pos("e5"));
        pawnAtE5Moves.Should().HaveCount(1, "black pawn at e5 can only capture to white pawn at d4").And.BeEquivalentTo([Move("e5d4")]);
        Apply(b, Move("e5d4"));
        b.CurrentPlayer.Should().Be(1, "after black's move, it should be white's turn");
        b.CurrentAdvantage.Should().Be(Advantage(2, 1), "black pawn at e5 captures white pawn at d4");
        b.GetPieceAtPosition(Pos("d4")).PieceType.Should().Be(ChessPieceType.Pawn, "pawn should be at e4");
        b.GetPieceAtPosition(Pos("d4")).Player.Should().Be(2, "pawn belongs to black");
        b.UndoLastMove();
        b.CurrentPlayer.Should().Be(2, "Undo should restore the side to move");
        b.GetPieceAtPosition(Pos("e5")).PieceType.Should().Be(ChessPieceType.Pawn, "undo must return the pawn to e5");
        b.GetPieceAtPosition(Pos("d4")).PieceType.Should().Be(ChessPieceType.Pawn, "undo must restore the captured pawn on d4");
        GetAllPiecesForPlayer(b, 1).Count().Should().Be(16, "Player 1 should have 16 pieces after undo");
        GetAllPiecesForPlayer(b, 2).Count().Should().Be(16, "Player 2 should have 16 pieces after undo");
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "undo must restore the advantage to 0");
    }

    [Fact]
    public void ConfirmValidMovesForBishop()
    {
        ChessBoard b = CreateBoardFromMoves("d2d4", "g8f6", "c1g5", "e7e6");
        b.CurrentPlayer.Should().Be(1, "it should be white to move");
        var possMoves = b.GetPossibleMoves();
        var bishopMoves = GetMovesAtPosition(possMoves, Pos("g5"));
        bishopMoves.Should().HaveCount(7, "black pawn at e5 can only capture to white pawn at d4").And.BeEquivalentTo([Move("g5f6"), Move("g5h4"), Move("g5h6"), Move("g5f4"), Move("g5e3"), Move("g5d2"), Move("g5c1")]);
    }

    [Fact]
    public void EnPassantTest()
    {
        ChessBoard b = CreateBoardFromMoves("g2g4", "h7h5", "g4h5");
        // Move black pawn forward twice, enabling en passant from h5
        Apply(b, "g7g5");
        var passMoves = b.GetPossibleMoves();
        var enPassantExpectedMove = GetMovesAtPosition(passMoves, Pos("h5"));
        enPassantExpectedMove.Should().HaveCount(2, "pawn can move forward one or en passant").And.Contain(Move("h5h6")).And.Contain(Move(Pos("h5"), Pos("g6"), ChessMoveType.EnPassant));
        // Apply the en passant
        Apply(b, "h5g6");
        var pawn = b.GetPieceAtPosition(Pos("g6"));
        pawn.Player.Should().Be(1, "white pawn performed en passant move on black");
        pawn.PieceType.Should().Be(ChessPieceType.Pawn, "pawn moved to g6");
        var captured = b.GetPieceAtPosition(Pos("h5"));
        captured.Player.Should().Be(0, "the pawn that moved to h5 was captured by en passant");
        b.CurrentAdvantage.Should().Be(Advantage(1, 2), "white should have an advantage of 2");
        // undo the en passant
        b.UndoLastMove();
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "white advantage should be back to 1 after undoing en passant");
        pawn = b.GetPieceAtPosition(Pos("h5"));
        pawn.Player.Should().Be(1, "white pawn should be back in position");
        pawn.PieceType.Should().Be(ChessPieceType.Pawn, "pawn should be back in position");
        captured = b.GetPieceAtPosition(Pos("g6"));
        captured.Player.Should().Be(0, "the pawn that moved to g6 should be back in position");
    }

    [Fact]
    public void CheckKingAndValidateMoves()
    {
        // This puts the white king in check by black's bishop
        ChessBoard b = CreateBoardFromMoves("d2d4", "e7e5", "c2c4", "f8b4");
        // Check if all possible moves are legal
        b.IsCheck.Should().Be(true, "white king is in check");
        var possMoves = b.GetPossibleMoves();
        possMoves.Count().Should().Be(4, "there are four possible moves to escape check");
        possMoves.Should().BeEquivalentTo([Move("d1d2"), Move("c1d2"), Move("b1d2"), Move("b1c3")]);
        // validate if check will disappear after valid move
        // moves horse b1 -> d2 to block check
        Apply(b, Move("b1d2"));
        var knight = b.GetPieceAtPosition(Pos("d2"));
        knight.PieceType.Should().Be(ChessPieceType.Knight, "piece should be knight");
        b.IsCheck.Should().Be(false, "white's horse blocks black's check");
    }

    //White and black
    [Fact]
    public void KingsAndQueensStartingPosition()
    {
        ChessBoard b = new ChessBoard();
        ChessPiece blackKing = b.GetPieceAtPosition(Pos(0, 4));
        blackKing.PieceType.Should().Be(ChessPieceType.King, "The black king should start at e8");
        blackKing.Player.Should().Be(2, "The king at e8 should be black");
        ChessPiece blackQueen = b.GetPieceAtPosition(Pos(0, 3));
        blackQueen.PieceType.Should().Be(ChessPieceType.Queen, "The black queen should start at d8");
        blackQueen.Player.Should().Be(2, "The queen at d8 should be black");
        ChessPiece whiteKing = b.GetPieceAtPosition(Pos(7, 4));
        whiteKing.PieceType.Should().Be(ChessPieceType.King, "The white king should start at e1");
        whiteKing.Player.Should().Be(1, "The king at e1 should be white");
        ChessPiece whiteQueen = b.GetPieceAtPosition(Pos(7, 3));
        whiteQueen.PieceType.Should().Be(ChessPieceType.Queen, "The white queen should start at d1");
        whiteQueen.Player.Should().Be(1, "The queen at d1 should be white");
    }

    //White
    [Fact]
    public void EnPassant__From_ThomasViveros()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "b8a6", "e4e5", "d7d5");
        //Check to see if an en passant is a possible move
        var possMoves = b.GetPossibleMoves();
        var enPassant = GetMovesAtPosition(possMoves, Pos("e5"));
        enPassant.Should().Contain(Move("e5d6"), "Attacking pawn should be able to move to the position between the victim pawns starting position and his end position when performing a 2 square advance");
        Apply(b, Move("e5d6"));
        //Check to make sure victim is killed
        b.GetPieceAtPosition(Pos(3, 3)).PieceType.Should().Be(ChessPieceType.Empty, "The pawn that got passed in the en passant should be removed from the game");
        b.CurrentAdvantage.Should().Be(Advantage(1, 1), "En passant should update advantage");
        //Check to see if an en passant can only be done when the victim moves two squares
        Apply(b, Move("c7c6"));
        possMoves = b.GetPossibleMoves();
        var enPassantFail = GetMovesAtPosition(possMoves, Pos("d6"));
        enPassantFail.Should().NotContain(Move("d6c7"), "Attacking pawn cannot perform an en passant when the victim pawn did not perform a 2 square advance as the previous move");
    }

    //White
    [Fact]
    public void UndoLastMoveRevive()
    {
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d5", "e4d5");
        b.UndoLastMove();
        //Check advantage
        b.CurrentAdvantage.Should().Be(Advantage(0, 0), "Undo last move should update advantage");
        //Check attacker reset
        b.GetPieceAtPosition(Pos("e4")).PieceType.Should().Be(ChessPieceType.Pawn, "Undo last move should move piece back to original position");
        //Check victim revive
        b.GetPieceAtPosition(Pos("d5")).PieceType.Should().Be(ChessPieceType.Pawn, "Undo last move should revive any piece that was taken during the last move");
    }

    //Black
    [Fact]
    public void UndoLastMovePromotion()
    {
        //Make sure when we undo a pawn promotion, the piece is a pawn
        ChessBoard b = CreateBoardFromMoves("a2a3", "d7d5", "h2h3", "d8d7", "h1h2", "d7h3", "a3a4", "h7h5", "h2h1", "h3h1", "g2g3", "h5h4", "g3g4", "h4h3", "g4g5", "h3h2", "g5g6", "h2g1queen");
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("h2")).PieceType.Should().Be(ChessPieceType.Pawn, "Undo last move should undo a pawn promotion");
    }

    //Black
    [Fact]
    public void QueenGetPossibleMoves()
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e6", "d2d4", "d8h4", "e1d2");
        var possMoves = b.GetPossibleMoves();
        var queenMoves = GetMovesAtPosition(possMoves, Pos("h4"));
        //Make sure we have specific amount of possible moves for this board state
        queenMoves.Should().HaveCount(15, "Queen possible movement logic incorrect, expected 15 possible moves with test board state");
        //Make sure we cant move through a piece
        queenMoves.Should().NotContain(Move("h4h1"), "Queen shouldnt be able to move through pieces");
        //Make sure we cant move in an invalid path
        queenMoves.Should().NotContain(Move("h4f5"), "Queen movement invalid. Queen can only move directly left, right, up, down, and diagional in a straight line.");
    }

    //Black
    [Fact]
    public void KingCheck__From_ThomasViveros()
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e6", "f3f4", "d8h4");
        b.IsCheck.Should().BeTrue("King should be in check when a piece moves and a piece can attack the king");
        b.IsCheckmate.Should().BeFalse("If the king can escape or be rescued then its not a checkmate but rather a check");
    }

    [Fact]
    public void NewChessBoard__From_warrensiu()
    {
        // Create new board and ensure board positions
        ChessBoard b = new ChessBoard();
        b.GetPieceAtPosition(Pos(0, 4)).Player.Should().Be(2, "Player 2 should be in top row of board");
        b.GetPieceAtPosition(Pos(0, 2)).PieceType.Should().Be(ChessPieceType.Bishop, "Black Bishop should be here");
    }

    [Fact]
    public void EnPassant__From_warrensiu()
    {
        // White pawn can take black pawn via en passant
        ChessBoard b = CreateBoardFromMoves("b2b4", "e7e5", "b4b5", "c7c5");
        var possibleMoves = b.GetPossibleMoves();
        var expectedMoves = GetMovesAtPosition(possibleMoves, Pos(3, 1));
        expectedMoves.Should().Contain(Move("b5c6"), "A possible en passant move for white");
    }

    [Fact]
    public void KingInCheck__From_warrensiu()
    {
        // Black King is placed in check so it must be defended
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "g1f3", "f7f5", "f3e5", "b7b5", "d1h5");
        var possibleMoves = b.GetPossibleMoves();
        possibleMoves.Count().Should().Be(1, "The black king is in check and black only has 1 move");
        possibleMoves.Should().BeEquivalentTo([Move("g7g6")]);
    }

    [Fact]
    public void UndoMove1()
    {
        // Undo a Move by White Queen, and then check which possible moves the Queen can make
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "g1f3", "f7f5", "f3e5", "b7b5", "d1h5");
        b.UndoLastMove();
        var possibleMoves = b.GetPossibleMoves();
        var possibleQueenMoves = GetMovesAtPosition(possibleMoves, Pos(7, 3));
        possibleQueenMoves.Should().Contain(Move("d1e2"));
        possibleQueenMoves.Count().Should().Be(4);
    }

    [Fact]
    public void UndoMove2()
    {
        // Undoes a Move by Black Pawn taking a Knight, and ensures capture piece is returned to board
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "g1f3", "f7f5", "f3e5", "b7b5", "d1h5", "g7g6", "e5g6", "h7g6");
        b.UndoLastMove();
        var knight = b.GetPieceAtPosition(Pos(2, 6));
        knight.PieceType.Should().Be(ChessPieceType.Knight, "White knight should be here");
        knight.Player.Should().Be(1, "Player 1 Knight should be here");
        b.CurrentPlayer.Should().Be(2, "It is Black's turn to move");
    }

    [Fact]
    public void TestQueenRange()
    {
        // Places a Black queen in middle of board to test all possible moves
        ChessBoard b = CreateBoardFromMoves("e2e4", "c7c5", "g1f3", "f7f5", "f3e5", "b7b5", "d1h5", "g7g6", "e5g6", "h7h6", "h5f5", "d8b6", "e4e5");
        var possibleMoves = b.GetPossibleMoves();
        var possibleQueenMoves = GetMovesAtPosition(possibleMoves, Pos(2, 1));
        possibleQueenMoves.Count().Should().Be(10, "Black Queen has 10 possible moves");
        possibleQueenMoves.Should().Contain(Move("b6g6"));
        possibleQueenMoves.Should().Contain(Move("b6a5"));
        possibleQueenMoves.Should().NotContain(Move("b6b5"));
    }

    [Fact]
    public void newChessBoard() //Test initial board, test if queens are in correct position and if white starts game
    //(checks both Black and white queens)
    {
        ChessBoard b = new ChessBoard();
        b.CurrentPlayer.Should().Be(1, "White should start the game");
        b.GetPieceAtPosition(Pos(7, 3)).PieceType.Should().Be(ChessPieceType.Queen, "White queen should be in this position");
        b.GetPieceAtPosition(Pos(0, 3)).PieceType.Should().Be(ChessPieceType.Queen, "Black Queen should be in this position");
    }

    [Fact]
    public void knightJumpCheck() //This test checks that rooks are allowed to jump over pieces (Black Knight)
    {
        ChessBoard b = new ChessBoard();
        Apply(b, "a2a3");
        var possMoves = b.GetPossibleMoves();
        var movesAtPos = GetMovesAtPosition(possMoves, Pos("b8"));
        movesAtPos.Should().Contain(Move("b8a6"));
        movesAtPos.Should().Contain(Move("b8c6"));
        var movesAtOther = GetMovesAtPosition(possMoves, Pos("g8"));
        movesAtOther.Should().Contain(Move("g8f6"));
        movesAtOther.Should().Contain(Move("g8h6"));
    }

    [Fact]
    public void queenMoveCheck() //id:027139643 mod 5 = 3 +1= 4 (white queen) (Undo Used)
    {
        //Checks queens possible moves with many(16) and small(2) amount of moves to stress test
        ChessBoard b = CreateBoardFromMoves("d2d4", "d7d5");
        Apply(b, "d1d3");
        Apply(b, "e7e6"); //place holder move to give turn back to white
        var possMoves = b.GetPossibleMoves();
        var checkQueensMoves = GetMovesAtPosition(possMoves, Pos("d3"));
        checkQueensMoves.Count().Should().Be(16, "There should be 16 queen moves allowed");
        b.UndoLastMove();
        b.UndoLastMove();
        Apply(b, "b2b3");
        Apply(b, "a7a6");
        var RecheckPossMoves = b.GetPossibleMoves();
        var ReCheckQueen = GetMovesAtPosition(RecheckPossMoves, Pos("d1"));
        ReCheckQueen.Count().Should().Be(2, "Queen Should only be allowed to move one or two spaces forward now");
    }

    [Fact]
    public void kingCheck() //puts king in check (Black)(Undo used)
    {
        //This test puts the king in check and checks to see if possible moves are legal
        ChessBoard b = CreateBoardFromMoves("e2e4", "d7d6", //
 "g2g4", "e7e6");
        Apply(b, "f1b5"); //king in check
        var possMoves = b.GetPossibleMoves();
        var checkKing = GetMovesAtPosition(possMoves, Pos("e8"));
        checkKing.Count().Should().Be(1, "There should be 1 possible move to cancel the checking of black's king");
        checkKing.Should().BeEquivalentTo([Move("e8e7")], "the king cannot move into check"); //e8 to d7 should not be valid move
        b.UndoLastMove();
        b.GetPieceAtPosition(Pos("e8"));
        //stress test king check after undo
        checkKing.Count().Should().Be(1, "There should be 1 possible move to cancel the checking of black's king");
        checkKing.Should().BeEquivalentTo([Move("e8e7")], "the king cannot move into check");
    }

    [Fact]
    public void CastleCheck__From_yewookbae() //check if castle works (white)
    {
        ChessBoard b = CreateBoardFromMoves("b1c3", //moves rook,bishop, and queen out of the way
 "h7h6", "b2b4", "g7g6", "c1b2", "a7a6", "d2d4", "b7b6");
        Apply(b, "d1d3"); //move queen put of the way to enable castle
        Apply(b, "h6h5"); // black move to turn it back to white move
        var possMoves = b.GetPossibleMoves();
        var castleExpected = GetMovesAtPosition(possMoves, Pos("e1"));
        castleExpected.Should().HaveCount(3, "King can either move left once, diagonally to the left once, or castle").And.Contain(Move("e1d1")).And.Contain(Move("e1d2")).And.Contain(Move(Pos("e1"), Pos("c1"), ChessMoveType.CastleQueenSide));
        //Apply Castle
        Apply(b, Move(Pos("e1"), Pos("c1"), ChessMoveType.CastleQueenSide));
    }

    [Fact]
    public void CheckMateCheck() //Checks if checkmate is identified and if game is finished
    {
        ChessBoard b = CreateBoardFromMoves("f2f3", "e7e6", "g2g4");
        Apply(b, "d8h4"); //checkmate
        b.IsCheckmate.Equals(true);
        b.IsFinished.Equals(true);
    }

    [Fact]
    public void EnPassantIntoCheck() {
        ChessBoard b = CreateBoardFromMoves(
            "e2e4",
            "d7d5",
            "e4d5",
            "e8d7",
            "h2h3",
            "e7e5",
            "d5e6"
        );

        b.IsCheck.Should().BeTrue("white pawn has checked Black King via En Passant");
    }

    [Fact]
    public void EnPassantNotAllowedAfterNonPawnMove() {
        ChessBoard b = CreateBoardFromMoves(
            "e2e4",
            "a7a6",
            "e4e5",
            "d7d5",
            "g1f3",
            "b8c6"
        );

        var possMoves = b.GetPossibleMoves();
        possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.EnPassant, "en passant is not allowed after a non-pawn move by either player");
    }

    [Fact]
    public void Draw50Moves() {
        ChessBoard b = new();
        // 24 times, move one knight from each player out of the starting row, then back.
        for (int i = 0; i < 24; i++) {
            Apply(b, "b1c3", "b8c6", "c3b1", "c6b8");
            b.IsDraw.Should().BeFalse("we have not reached the 50 turns rule yet");
        }

        b.DrawCounter.Should().Be(96, "96 non-pawn non-capturing moves in a row");
        Apply(b, "b1c3");
		b.IsDraw.Should().BeFalse("we have not reached the 50 move rule yet");
		b.DrawCounter.Should().Be(97, "97 non-pawn non-capturing moves in a row");
		Apply(b, "b8c6");
		b.IsDraw.Should().BeFalse("we have not reached the 50 move rule yet");
		b.DrawCounter.Should().Be(98, "98 non-pawn non-capturing moves in a row");
		Apply(b, "c3b1");
		b.IsDraw.Should().BeFalse("we have not reached the 50 move rule yet");
		b.DrawCounter.Should().Be(99, "99 non-pawn non-capturing moves in a row");
		Apply(b, "c6b8");
		b.IsDraw.Should().BeTrue("100 non-pawn non-capturing moves in a row");
		b.DrawCounter.Should().Be(100, "100 non-pawn non-capturing moves in a row");
	}

    [Fact]
    public void DrawCounterUndo() {
        ChessBoard b = CreateBoardFromMoves(
            "b1c3",
            "b8c6"
        );
        b.DrawCounter.Should().Be(2, "2 non-pawn non-capturing moves in a row");
        Apply(b, "a2a4");
		b.DrawCounter.Should().Be(0, "a pawn was moved");
        b.UndoLastMove();
		b.DrawCounter.Should().Be(2, "2 non-pawn non-capturing moves in a row");
	}

	[Fact]
	public void DrawCounterEnPassant() {
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"c7c5",
            "a4a5",
			"g8h6",
            "b1c3"
		);
		b.DrawCounter.Should().Be(2, "2 non-pawn non-capturing moves in a row");
		Apply(b, "b7b5", "a5b6"); // en passant
		b.DrawCounter.Should().Be(0, "en passant is a pawn move, so draw counter should be 0");
		b.UndoLastMove();
		b.UndoLastMove();
		b.DrawCounter.Should().Be(2, "2 non-pawn non-capturing moves in a row");
	}

	[Fact]
	public void DrawCounterPromote() {
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"b7b5",
			"a4b5",
			"a7a5",
            "b5a6",
			"g8h6",
            "a6a7",
            "h6f5"
            
		);
		b.DrawCounter.Should().Be(1, "1 non-pawn non-capturing moves in a row");
		Apply(b, "a7b8queen"); // promote and capture
		b.DrawCounter.Should().Be(0, "promoted and captured, draw count should be 0");
		b.UndoLastMove();
		b.DrawCounter.Should().Be(1, "1 non-pawn non-capturing moves in a row");
	}


	[Fact]
	public void DrawCounterPromote_NonCapture() {
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"b7b5",
			"a4b5",
			"c7c5",
			"b5c6",
			"c8a6",
			"c6c7",
			"b8c6"

		);
		b.DrawCounter.Should().Be(1, "1 non-pawn non-capturing moves in a row");
		Apply(b, "c7c8knight"); // promote
		b.DrawCounter.Should().Be(0, "pawn promoted, draw count should be 0");
		b.UndoLastMove();
		b.DrawCounter.Should().Be(1, "1 non-pawn non-capturing moves in a row");
	}

	[Fact]
    public void RobustCastlingTests_1() {
        // Test 1: CAN castle if the rook moves through attacked squares, but the king does not.
        ChessBoard b = CreateBoardFromMoves(
            "a2a4",
            "b7b5",
            "a4b5",
            "b8c6",
            "b5c6",
            "c8a6",
            "c6d7",
            "d8d7",
            "a1a6",
            "c7c5",
            "a6b6"
        );

        var possMoves = b.GetPossibleMoves();
        possMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleQueenSide, "black can castle queen side, because the king does not move through a square under attack");
	}

	[Fact]
	public void RobustCastlingTests_2() {
		// Test 2: CANNOT castle if the rook has previously moved, then moved back to its starting spot.
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"b7b5",
			"a4b5",
			"b8c6",
			"b5c6",
			"c8a6",
			"c6d7",
			"d8d7",
			"a1a6",
			"c7c5",
			"a6b6"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleQueenSide, "black can castle queen side, because the king does not move through a square under attack");

		Apply(b,
			"a8b8", // move black rook
			"b2b3", // arbitrary white response
			"b8a8", // black rook moves back
			"b3b4" // arbitrary white response
		);

		possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleQueenSide, "black cannot castle queen side, because the rook has previously moved and then moved back to its starting square");
	}

	[Fact]
	public void RobustCastlingTests_3() {
		// Test 3: CANNOT castle if the king passes into an attacked square.
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"b7b5",
			"a4b5",
			"b8c6",
			"b5c6",
			"c8a6",
			"c6d7",
			"d8d7",
			"a1a6",
			"c7c5",
			"a6c6"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleQueenSide, "black cannot castle queen side, because the king move into a square under attack");
	}

	[Fact]
	public void RobustCastlingTests_4() {
		// Test 3: CANNOT castle if the king passes through an attacked square.
		ChessBoard b = CreateBoardFromMoves(
			"a2a4",
			"b7b5",
			"a4b5",
			"b8c6",
			"b5c6",
			"c8a6",
			"c6d7",
			"d8d7",
			"a1a6",
			"d7d5",
			"a6d6"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleQueenSide, "black cannot castle queen side, because the king move into a square under attack");
	}

	[Fact]
	public void RobustCastlingTests_5() {
		// Test 3: CANNOT castle if the king moves and then moves back.
		ChessBoard b = CreateBoardFromMoves(
			"h2h4",
			"g7g5",
			"h4g5",
			"g8f6",
			"g5f6",
			"f8h6",
			"f6e7",
			"e8e7",
			"h1h6",
			"e7e8",
			"g2g3"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "black cannot castle king side, because the king moved and then moved back");
	}

	[Fact]
	public void RobustCastlingTests_6() {
		// Test 3: CANNOT castle king side if moving through squares under attack.
		ChessBoard b = CreateBoardFromMoves(
			"h2h4",
			"g7g5",
			"h4g5",
			"g8f6",
			"g5f6",
			"f8h6",
			"f6e7",
			"a7a6",
			"h1h6",
			"a6a5",
			"h6g6"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "black cannot castle king side, because the king ends in an attacked position");
	}

	[Fact]
	public void RobustCastlingTests_7() {
		// Test 3: CANNOT castle if currently in check.
		ChessBoard b = CreateBoardFromMoves(
			"f2f4",
			"e7e5",
			"g2g4",
			"d7d5",
			"f1h3",
			"c7c5",
			"g1f3",
            "d8h4"
		);

        b.IsCheck.Should().BeTrue("queen has the king in check\n" + ConsoleView.BoardToString(b));
		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "cannot castle if king is in check");
	}

	[Fact]
	public void RobustCastlingTests_8() {
		// Test 3: CANNOT castle if the rook previously moved.
		ChessBoard b = CreateBoardFromMoves(
			"f2f4",
			"e7e5",
			"g2g4",
			"d7d5",
			"f1h3",
			"c7c5",
			"g1f3",
			"b7b5",
            "h1f1",
            "a7a5",
            "f1h1",
            "e8e7"
		);

		var possMoves = b.GetPossibleMoves();
        possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "cannot castle if the rook previously moved");
	}

	[Fact]
	public void RobustCastlingTests_9() {
		// Test 3: CANNOT castle if the king previously moved.
		ChessBoard b = CreateBoardFromMoves(
			"f2f4",
			"e7e5",
			"g2g4",
			"d7d5",
			"f1h3",
			"c7c5",
			"g1f3",
			"b7b5",
			"e1f1",
			"a7a5",
			"f1e1",
			"e8e7"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "cannot castle if the king previously moved");
	}

	[Fact]
	public void RobustCastlingTests_10() {
		// Test 3: CAN castle after UNDOING a king move.
		ChessBoard b = CreateBoardFromMoves(
			"f2f4",
			"e7e5",
			"g2g4",
			"d7d5",
			"f1h3",
			"c7c5",
			"g1f3",
			"b7b5",
			"e1f1",
            "a7a5"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "cannot castle if the king previously moved");

        b.UndoLastMove();
		b.UndoLastMove();
		possMoves = b.GetPossibleMoves();
		possMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleKingSide, "can castle if the king previously moved but we UNDID that");
	}

	[Fact]
	public void RobustCastlingTests_11() {
		// Test 3: CAN castle after UNDOING a rook move.
		ChessBoard b = CreateBoardFromMoves(
			"f2f4",
			"e7e5",
			"g2g4",
			"d7d5",
			"f1h3",
			"c7c5",
			"g1f3",
			"b7b5",
			"h1f1",
			"a7a5",
			"f1h1",
			"e8e7"
		);

		var possMoves = b.GetPossibleMoves();
		possMoves.Should().NotContain(m => m.MoveType == ChessMoveType.CastleKingSide, "cannot castle if the rook previously moved");

        b.UndoLastMove();
		b.UndoLastMove();
		b.UndoLastMove();
		b.UndoLastMove();
		possMoves = b.GetPossibleMoves();
		possMoves.Should().Contain(m => m.MoveType == ChessMoveType.CastleKingSide, "can castle if the rook previously moved but we UNDID that");
	}
}
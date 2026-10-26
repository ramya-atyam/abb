using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Tests;

public sealed class GameServiceTests
{
    [Fact]
    public void ValidMoveUpdatesBoardAndTurn()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);

        var result = service.Play(game.Id, new MoveRequest("X", 1, 1));

        Assert.Equal("X", result.Board[0]);
        Assert.Equal("O", result.CurrentPlayer);
        Assert.Single(result.History);
    }

    [Fact]
    public void RejectsInvalidAndOccupiedMovesWithoutChangingState()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        service.Play(game.Id, new MoveRequest("X", 1, 1));

        Assert.Throws<GameRuleException>(() => service.Play(game.Id, new MoveRequest("O", 1, 1)));
        Assert.Throws<GameRuleException>(() => service.Play(game.Id, new MoveRequest("O", 4, 1)));
        Assert.Single(service.Get(game.Id).History);
    }

    [Fact]
    public void DetectsWinsAndUpdatesScoreOnlyOnce()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, game.Id, (1, 1), (2, 1), (1, 2), (2, 2), (1, 3));

        var won = service.Get(game.Id);
        Assert.Equal(GameStatus.Won, won.Status);
        Assert.Equal("X", won.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, won.WinningCells);
        Assert.Equal(1, service.GetScoreboard().XWins);
        Assert.Throws<GameRuleException>(() => service.Play(game.Id, new MoveRequest("O", 3, 3)));
        Assert.Equal(1, service.GetScoreboard().XWins);
    }

    [Fact]
    public void ComputerMakesPriorityMoveAndUndoRemovesMovePair()
    {
        var service = new GameService();
        var game = service.Create(GameMode.Computer);

        var moved = service.Play(game.Id, new MoveRequest("X", 1, 1));

        Assert.Equal("O", moved.Board[4]);
        Assert.Equal("X", moved.CurrentPlayer);
        Assert.Equal(2, moved.History.Count);
        var undone = service.Undo(game.Id);
        Assert.Empty(undone.History);
        Assert.All(undone.Board, cell => Assert.Null(cell));
        Assert.Equal("X", undone.CurrentPlayer);
    }

    [Fact]
    public void ResetPreservesScoreboardAndUnknownGameIsNotFound()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, game.Id, (1, 1), (2, 1), (1, 2), (2, 2), (1, 3));

        var reset = service.Reset(game.Id);

        Assert.Equal(GameStatus.InProgress, reset.Status);
        Assert.Equal(1, service.GetScoreboard().XWins);
        Assert.Throws<GameRuleException>(() => service.Get(Guid.NewGuid()));
    }

    [Fact]
    public void UndoRemovesOnlyLatestTwoPlayerMove()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        service.Play(game.Id, new MoveRequest("X", 1, 1));
        service.Play(game.Id, new MoveRequest("O", 2, 2));

        var result = service.Undo(game.Id);

        Assert.Single(result.History);
        Assert.Equal("O", result.CurrentPlayer);
        Assert.Null(result.Board[4]);
    }

    [Fact]
    public void DetectsDrawAndCountsItOnce()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, game.Id, (1, 1), (1, 2), (1, 3), (2, 2), (2, 1), (2, 3), (3, 2), (3, 1), (3, 3));

        Assert.Equal(GameStatus.Draw, service.Get(game.Id).Status);
        Assert.Equal(1, service.GetScoreboard().Draws);
    }

    [Fact]
    public void DetectsColumnAndDiagonalWins()
    {
        var service = new GameService();
        var columnGame = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, columnGame.Id, (1, 1), (1, 2), (2, 1), (1, 3), (3, 1));
        Assert.Equal(new[] { 0, 3, 6 }, service.Get(columnGame.Id).WinningCells);

        var diagonalGame = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, diagonalGame.Id, (1, 1), (1, 2), (2, 2), (1, 3), (3, 3));
        Assert.Equal(new[] { 0, 4, 8 }, service.Get(diagonalGame.Id).WinningCells);
    }

    [Fact]
    public void ComputerBlocksHumanWinningMove()
    {
        var service = new GameService();
        var game = service.Create(GameMode.Computer);
        service.Play(game.Id, new MoveRequest("X", 1, 1));

        var result = service.Play(game.Id, new MoveRequest("X", 1, 2));

        Assert.Equal("O", result.Board[2]);
        Assert.Equal(GameStatus.InProgress, result.Status);
    }

    [Fact]
    public void ComputerTakesWinningMoveBeforeBlocking()
    {
        var service = new GameService();
        var game = service.Create(GameMode.Computer);
        service.Play(game.Id, new MoveRequest("X", 1, 2));
        service.Play(game.Id, new MoveRequest("X", 1, 1));

        var result = service.Play(game.Id, new MoveRequest("X", 3, 3));

        Assert.Equal(GameStatus.Won, result.Status);
        Assert.Equal("O", result.Winner);
        Assert.Contains(6, result.WinningCells);
        Assert.Equal(1, service.GetScoreboard().OWins);
    }

    [Fact]
    public void ComputerUsesCenterThenFirstAvailableCorner()
    {
        var service = new GameService();
        var game = service.Create(GameMode.Computer);
        var first = service.Play(game.Id, new MoveRequest("X", 1, 1));
        Assert.Equal("O", first.Board[4]);

        var cornerGame = service.Create(GameMode.Computer);
        var corner = service.Play(cornerGame.Id, new MoveRequest("X", 2, 2));
        Assert.Equal("O", corner.Board[0]);
    }

    [Fact]
    public void CompletedGameCannotBeUndoneAndScoreboardCanReset()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);
        PlaySequence(service, game.Id, (1, 1), (2, 1), (1, 2), (2, 2), (1, 3));

        Assert.Throws<GameRuleException>(() => service.Undo(game.Id));
        Assert.Equal(new Scoreboard(0, 0, 0), service.ResetScoreboard());
        Assert.Equal(new Scoreboard(0, 0, 0), service.GetScoreboard());
    }

    [Fact]
    public void RejectsOutOfTurnMoveWithoutChangingState()
    {
        var service = new GameService();
        var game = service.Create(GameMode.TwoPlayer);

        Assert.Throws<GameRuleException>(() => service.Play(game.Id, new MoveRequest("O", 1, 1)));
        Assert.Empty(service.Get(game.Id).History);
    }

    private static void PlaySequence(GameService service, Guid id, params (int Row, int Column)[] moves)
    {
        var player = "X";
        foreach (var (row, column) in moves)
        {
            service.Play(id, new MoveRequest(player, row, column));
            player = player == "X" ? "O" : "X";
        }
    }
}
namespace TicTacToe.Api.Models;

public enum GameMode
{
    TwoPlayer,
    Computer
}

public enum GameStatus
{
    InProgress,
    Won,
    Draw
}

public sealed record MoveEntry(int MoveNumber, string Player, int Row, int Column);

public sealed record GameState(
    Guid Id,
    string?[] Board,
    string CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    string? Winner,
    int[] WinningCells,
    IReadOnlyList<MoveEntry> History);

public sealed record Scoreboard(int XWins, int OWins, int Draws);

public sealed record CreateGameRequest(GameMode Mode);

public sealed record MoveRequest(string Player, int Row, int Column);

public sealed record ApiError(int Status, string Error);
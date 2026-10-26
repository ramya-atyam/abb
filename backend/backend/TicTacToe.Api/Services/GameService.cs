using TicTacToe.Api.Models;

namespace TicTacToe.Api.Services;

public sealed class GameService
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    private readonly object _gate = new();
    private readonly Dictionary<Guid, Game> _games = [];
    private int _xWins;
    private int _oWins;
    private int _draws;

    public GameState Create(GameMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new GameRuleException(400, "Game mode must be TwoPlayer or Computer.");
        }

        lock (_gate)
        {
            var game = new Game(Guid.NewGuid(), mode);
            _games.Add(game.Id, game);
            return Snapshot(game);
        }
    }

    public GameState Get(Guid id)
    {
        lock (_gate)
        {
            return Snapshot(Find(id));
        }
    }

    public GameState Play(Guid id, MoveRequest request)
    {
        lock (_gate)
        {
            var game = Find(id);
            EnsureInProgress(game);
            if (request.Row is < 1 or > 3 || request.Column is < 1 or > 3)
            {
                throw new GameRuleException(400, "Row and column must be between 1 and 3.");
            }

            var player = request.Player?.ToUpperInvariant();
            if (player is not ("X" or "O"))
            {
                throw new GameRuleException(400, "Player must be X or O.");
            }

            if (game.Mode == GameMode.Computer && player == "O")
            {
                throw new GameRuleException(409, "The computer makes its own moves.");
            }
            if (player != game.CurrentPlayer)
            {
                throw new GameRuleException(409, $"It is {game.CurrentPlayer}'s turn.");
            }

            var index = (request.Row - 1) * 3 + request.Column - 1;
            if (game.Board[index] is not null)
            {
                throw new GameRuleException(409, "That cell is already occupied.");
            }

            ApplyMove(game, index, player);
            if (game.Mode == GameMode.Computer && game.Status == GameStatus.InProgress)
            {
                ApplyMove(game, ChooseComputerMove(game), "O");
            }

            return Snapshot(game);
        }
    }

    public GameState Undo(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            EnsureInProgress(game);
            if (game.History.Count == 0)
            {
                throw new GameRuleException(409, "There are no moves to undo.");
            }

            var removeCount = game.Mode == GameMode.Computer ? Math.Min(2, game.History.Count) : 1;
            for (var count = 0; count < removeCount; count++)
            {
                var last = game.History[^1];
                game.Board[(last.Row - 1) * 3 + last.Column - 1] = null;
                game.History.RemoveAt(game.History.Count - 1);
            }
            game.CurrentPlayer = game.Mode == GameMode.Computer ? "X" : game.History.Count % 2 == 0 ? "X" : "O";
            game.Winner = null;
            game.WinningCells = [];
            game.Status = GameStatus.InProgress;
            return Snapshot(game);
        }
    }

    public GameState Reset(Guid id)
    {
        lock (_gate)
        {
            var game = Find(id);
            game.Board = new string?[9];
            game.History.Clear();
            game.CurrentPlayer = "X";
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = [];
            return Snapshot(game);
        }
    }

    public Scoreboard GetScoreboard()
    {
        lock (_gate)
        {
            return new Scoreboard(_xWins, _oWins, _draws);
        }
    }

    public Scoreboard ResetScoreboard()
    {
        lock (_gate)
        {
            _xWins = 0;
            _oWins = 0;
            _draws = 0;
            return new Scoreboard(_xWins, _oWins, _draws);
        }
    }

    private Game Find(Guid id) => _games.TryGetValue(id, out var game)
        ? game
        : throw new GameRuleException(404, "Game session was not found.");

    private static void EnsureInProgress(Game game)
    {
        if (game.Status != GameStatus.InProgress)
        {
            throw new GameRuleException(409, "This game has already completed.");
        }
    }

    private void ApplyMove(Game game, int index, string player)
    {
        game.Board[index] = player;
        game.History.Add(new MoveEntry(game.History.Count + 1, player, index / 3 + 1, index % 3 + 1));
        var line = WinningLines.FirstOrDefault(candidate => candidate.All(cell => game.Board[cell] == player));
        if (line is not null)
        {
            game.Status = GameStatus.Won;
            game.Winner = player;
            game.WinningCells = line;
            if (player == "X") _xWins++;
            else _oWins++;
            return;
        }

        if (game.Board.All(cell => cell is not null))
        {
            game.Status = GameStatus.Draw;
            _draws++;
            return;
        }
        game.CurrentPlayer = player == "X" ? "O" : "X";
    }

    private static int ChooseComputerMove(Game game)
    {
        foreach (var symbol in new[] { "O", "X" })
        {
            foreach (var line in WinningLines)
            {
                var open = line.Where(index => game.Board[index] is null).ToArray();
                if (open.Length == 1 && line.Count(index => game.Board[index] == symbol) == 2)
                {
                    return open[0];
                }
            }
        }

        if (game.Board[4] is null) return 4;
        foreach (var corner in new[] { 0, 2, 6, 8 })
        {
            if (game.Board[corner] is null) return corner;
        }
        return Array.FindIndex(game.Board, cell => cell is null);
    }

    private static GameState Snapshot(Game game) => new(
        game.Id,
        [.. game.Board],
        game.CurrentPlayer,
        game.Mode,
        game.Status,
        game.Winner,
        [.. game.WinningCells],
        game.History.ToArray());

    private sealed class Game(Guid id, GameMode mode)
    {
        public Guid Id { get; } = id;
        public GameMode Mode { get; } = mode;
        public string?[] Board { get; set; } = new string?[9];
        public string CurrentPlayer { get; set; } = "X";
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public string? Winner { get; set; }
        public int[] WinningCells { get; set; } = [];
        public List<MoveEntry> History { get; } = [];
    }
}
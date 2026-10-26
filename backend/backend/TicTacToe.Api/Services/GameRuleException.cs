namespace TicTacToe.Api.Services;

public sealed class GameRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
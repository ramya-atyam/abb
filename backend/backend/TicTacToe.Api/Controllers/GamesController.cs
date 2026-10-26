using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(GameService games) : ControllerBase
{
    [HttpPost]
    public ActionResult<GameState> Create([FromBody] CreateGameRequest request) => Ok(games.Create(request.Mode));

    [HttpGet("{id:guid}")]
    public ActionResult<GameState> Get(Guid id) => Ok(games.Get(id));

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameState> Move(Guid id, [FromBody] MoveRequest request) => Ok(games.Play(id, request));

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameState> Undo(Guid id) => Ok(games.Undo(id));

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameState> Reset(Guid id) => Ok(games.Reset(id));
}
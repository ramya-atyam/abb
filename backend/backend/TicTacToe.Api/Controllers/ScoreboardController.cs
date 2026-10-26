using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(GameService games) : ControllerBase
{
    [HttpGet]
    public ActionResult<Scoreboard> Get() => Ok(games.GetScoreboard());

    [HttpPost("reset")]
    public ActionResult<Scoreboard> Reset() => Ok(games.ResetScoreboard());
}
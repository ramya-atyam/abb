# Gridline Tic Tac Toe

A browser-based Tic Tac Toe game with a React and TypeScript client and an ASP.NET Core API. The backend owns game state, move validation, the computer strategy, undo behavior, and session scoreboard.

## Features

- Two-player and human-versus-computer modes.
- Server-side validation, win/draw detection, winning-cell indices, and move history.
- Deterministic computer opponent: win, block, center, first available corner, then first available cell.
- Undo takes back one move in two-player mode or the human/computer pair in computer mode. Undo is disabled after a completed game.
- Reset starts a clean round without changing the scoreboard; scoreboard reset is separate and confirmed in the UI.
- Responsive, keyboard-accessible UI with loading, disabled, and API error states.
- OpenAPI document in the development environment.

## Technology

- React 19, TypeScript, Vite, Vitest, React Testing Library.
- ASP.NET Core Web API and C# on .NET 10.
- xUnit for backend unit tests.
- In-memory singleton service for games and scoreboard. All state is lost when the API process restarts.

## Project Structure

```text
backend/backend/TicTacToe.Api/       ASP.NET Core API, controllers, models, services
backend/backend/TicTacToe.Tests/     xUnit service tests
frontend/src/                        React client, API service, UI tests, styles
README.md                            Setup and API guide
```

## Prerequisites

- .NET 10 SDK
- Node.js 20.19+ or 22.12+ and npm

## Run Locally

From the repository root, start the API:

```powershell
dotnet run --project backend/backend/TicTacToe.Api --urls http://localhost:5130
```

In another terminal, install dependencies and start Vite:

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The API defaults to `http://localhost:5130`. To use another API origin, set `VITE_API_URL` in the frontend environment before starting or building, for example `VITE_API_URL=http://localhost:5130`.

The API permits the default Vite origin `http://localhost:5173` through CORS. For a different frontend origin, update the allowed origin in `backend/backend/TicTacToe.Api/Program.cs`.

## API

JSON properties use camel case. Enum values are strings. Rows and columns are one-based; winning-cell indices are zero-based offsets into the nine-element board array. Empty board cells are `null`.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/games` | Create a game with `{ "mode": "Computer" }` or `{ "mode": "TwoPlayer" }` |
| `GET` | `/api/games/{id}` | Retrieve a game |
| `POST` | `/api/games/{id}/moves` | Submit `{ "player": "X", "row": 1, "column": 1 }` |
| `POST` | `/api/games/{id}/undo` | Undo the latest move or move pair |
| `POST` | `/api/games/{id}/reset` | Reset the board while preserving scores |
| `GET` | `/api/scoreboard` | Retrieve session scores |
| `POST` | `/api/scoreboard/reset` | Reset all session scores |

Example create request:

```http
POST /api/games
Content-Type: application/json

{ "mode": "Computer" }
```

Example response (abridged):

```json
{
  "id": "d3d74d0f-4310-4ab2-9450-687132d00bba",
  "board": [null, null, null, null, null, null, null, null, null],
  "currentPlayer": "X",
  "mode": "Computer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "history": []
}
```

Successful requests return `200`. Invalid coordinates or symbols return `400`; missing games return `404`; illegal turns, occupied cells, completed games, and unavailable undo return `409`. Errors have the shape `{ "status": 409, "error": "..." }`. The OpenAPI JSON is available at `http://localhost:5130/openapi/v1.json` while running in Development.

## Tests

Run backend tests from the repository root:

```powershell
dotnet test backend/backend/TicTacToe.Tests/TicTacToe.Tests.csproj
```

Run frontend tests and production build from `frontend/`:

```powershell
npm test
npm run build
```

The backend tests exercise move validation, turn changes, wins, draw, reset, undo, scoreboard behavior, unknown game IDs, and computer strategy. Frontend tests cover server-backed initialization, move submission/history, and connection errors.

## Publish to GitHub

Install Git for Windows first, then open a new PowerShell terminal in the project root. The root `.gitignore` excludes generated build output and local environment files.

If `https://github.com/ramya-atyam/abb` is an empty repository, initialize and push this workspace:

```powershell
git init -b main
git add .
git commit -m "Add Tic Tac Toe application"
git remote add origin https://github.com/ramya-atyam/abb.git
git push -u origin main
```

If the GitHub repository already contains commits or files, clone it first and copy the project files into that clone before committing. Do not force-push over the existing repository history. GitHub may prompt you to authenticate through the configured Git credential manager; never put a password or personal access token in a command.

## Decisions and Limitations

- Game rules and transitions live in a singleton in-memory service. A lock serializes transitions and scoreboard updates for this single-process assignment app.
- Completed games cannot be undone. Reset preserves the session scoreboard, and reset-scoreboard does not alter active games.
- The computer uses deterministic priority order and plays O. In computer mode, the client submits only X; the API returns the state after O's automatic response when the game remains active.
- In-memory games and scores are not durable and are cleared on backend restart. The singleton and lock are intended for a single API process, not distributed hosting.
- Malformed JSON uses ASP.NET Core's standard `400` response; game-rule errors use the structured response described above.
- The interface uses Google Fonts when online and falls back to system sans-serif and monospace fonts when unavailable.

## AI-Assisted Development Summary

The implementation followed the supplied assignment: establish backend-owned game state and REST contracts first, connect the React interface through a dedicated API service, add automated tests, and document startup, endpoints, rules, and trade-offs. Design choices prioritize a small, reviewable service boundary and deterministic behavior suitable for an interview walkthrough.
export type GameMode = 'TwoPlayer' | 'Computer'
export type GameStatus = 'InProgress' | 'Won' | 'Draw'
export type MoveEntry = { moveNumber: number; player: 'X' | 'O'; row: number; column: number }
export type GameState = { id: string; board: (null | 'X' | 'O')[]; currentPlayer: 'X' | 'O'; mode: GameMode; status: GameStatus; winner: 'X' | 'O' | null; winningCells: number[]; history: MoveEntry[] }
export type Scoreboard = { xWins: number; oWins: number; draws: number }

const baseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5130'

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, { ...options, headers: { 'Content-Type': 'application/json', ...options?.headers } })
  } catch {
    throw new Error('Could not reach the game server. Make sure the backend is running.')
  }
  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { error?: string; title?: string } | null
    throw new Error(payload?.error ?? payload?.title ?? `Request failed (${response.status}).`)
  }
  return response.json() as Promise<T>
}

export const api = {
  createGame: (mode: GameMode) => request<GameState>('/api/games', { method: 'POST', body: JSON.stringify({ mode }) }),
  getScoreboard: () => request<Scoreboard>('/api/scoreboard'),
  play: (id: string, player: 'X' | 'O', row: number, column: number) => request<GameState>(`/api/games/${id}/moves`, { method: 'POST', body: JSON.stringify({ player, row, column }) }),
  undo: (id: string) => request<GameState>(`/api/games/${id}/undo`, { method: 'POST' }),
  reset: (id: string) => request<GameState>(`/api/games/${id}/reset`, { method: 'POST' }),
  resetScoreboard: () => request<Scoreboard>('/api/scoreboard/reset', { method: 'POST' }),
}
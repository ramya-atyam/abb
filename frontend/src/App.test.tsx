import { afterEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from './App'

const game = { id: 'test-game', board: Array(9).fill(null), currentPlayer: 'X', mode: 'Computer', status: 'InProgress', winner: null, winningCells: [], history: [] }
function jsonResponse(data: unknown) { return Promise.resolve(new Response(JSON.stringify(data), { status: 200, headers: { 'Content-Type': 'application/json' } })) }

afterEach(() => { cleanup(); vi.restoreAllMocks() })

describe('Tic Tac Toe app', () => {
  it('loads the game and scoreboard, then submits a move to the API', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
      const url = String(input)
      if (url.endsWith('/api/games')) return jsonResponse(game)
      if (url.endsWith('/api/scoreboard')) return jsonResponse({ xWins: 2, oWins: 1, draws: 0 })
      return jsonResponse({ ...game, board: ['X', 'O', ...Array(7).fill(null)], currentPlayer: 'X', history: [{ moveNumber: 1, player: 'X', row: 1, column: 1 }, { moveNumber: 2, player: 'O', row: 2, column: 2 }] })
    })
    render(<App />)
    expect(await screen.findByText('2')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('gridcell', { name: 'Row 1, column 1, empty' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/api/games/test-game/moves'), expect.objectContaining({ method: 'POST' })))
    expect(await screen.findByText('R2 / C2')).toBeInTheDocument()
  })

  it('shows a readable error when the API cannot be reached', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('network error'))
    render(<App />)
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not reach the game server')
  })
})
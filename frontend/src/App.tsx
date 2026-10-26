import { useCallback, useEffect, useRef, useState } from 'react'
import { api, type GameMode, type GameState, type Scoreboard } from './services/api'
import './App.css'

const emptyScoreboard: Scoreboard = { xWins: 0, oWins: 0, draws: 0 }

function App() {
  const [mode, setMode] = useState<GameMode>('Computer')
  const [game, setGame] = useState<GameState | null>(null)
  const [scoreboard, setScoreboard] = useState(emptyScoreboard)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const initialized = useRef(false)

  const startGame = useCallback(async (nextMode: GameMode) => {
    setBusy(true)
    setError('')
    try {
      const [created, scores] = await Promise.all([api.createGame(nextMode), api.getScoreboard()])
      setMode(nextMode)
      setGame(created)
      setScoreboard(scores)
    } catch (reason) {
      setError(errorMessage(reason))
    } finally {
      setBusy(false)
    }
  }, [])

  useEffect(() => {
    if (initialized.current) return
    initialized.current = true
    void startGame('Computer')
  }, [startGame])

  async function perform(action: () => Promise<GameState>, refreshScore = false) {
    if (!game) return
    setBusy(true)
    setError('')
    try {
      const updated = await action()
      setGame(updated)
      if (refreshScore) setScoreboard(await api.getScoreboard())
    } catch (reason) {
      setError(errorMessage(reason))
    } finally {
      setBusy(false)
    }
  }

  async function resetScoreboard() {
    if (!window.confirm('Reset all session scores?')) return
    setBusy(true)
    setError('')
    try {
      setScoreboard(await api.resetScoreboard())
    } catch (reason) {
      setError(errorMessage(reason))
    } finally {
      setBusy(false)
    }
  }

  const finished = game?.status !== 'InProgress'
  const statusText = game?.status === 'Won'
    ? `${game.winner} takes the round`
    : game?.status === 'Draw'
      ? 'A well-played draw'
      : game
        ? `${game.currentPlayer} to move`
        : 'Connecting to game server'

  return (
    <main className="app-shell">
      <header className="topbar">
        <a className="wordmark" href="#top" aria-label="Gridline home">GRIDLINE<span> / 03</span></a>
        <div className="topbar-note"><span className="online-dot" /> SESSION PLAY · {game?.id.slice(0, 8) ?? 'LOADING'}</div>
      </header>

      <section className="intro" id="top">
        <div>
          <p className="eyebrow">THE CLASSIC, IN GOOD COMPANY</p>
          <h1>Three in<br className="mobile-break" /> a row<span className="title-period">.</span></h1>
        </div>
        <p className="intro-copy">A little strategy goes a long way.<br />Make your mark, then make it count.</p>
      </section>

      <section className="game-layout" aria-label="Tic Tac Toe game">
        <div className="play-panel">
          <div className="game-toolbar">
            <div className="mode-switch" role="group" aria-label="Game mode">
              <button className={mode === 'Computer' ? 'mode-option selected' : 'mode-option'} onClick={() => void startGame('Computer')} disabled={busy}>
                <span className="mode-icon">◎</span> vs computer
              </button>
              <button className={mode === 'TwoPlayer' ? 'mode-option selected' : 'mode-option'} onClick={() => void startGame('TwoPlayer')} disabled={busy}>
                <span className="mode-icon">◉</span> two players
              </button>
            </div>
            <span className="round-label">ROUND 01</span>
          </div>

          <div className={`turn-display ${finished ? 'is-finished' : ''}`} aria-live="polite">
            <span className="turn-symbol">{game?.status === 'Won' ? game.winner : game?.status === 'Draw' ? '=' : game?.currentPlayer ?? '·'}</span>
            <div>
              <p className="turn-label">{game?.status === 'InProgress' ? 'CURRENT TURN' : 'ROUND COMPLETE'}</p>
              <p className="turn-message">{statusText}</p>
            </div>
            <span className="turn-mark">{game?.status === 'Won' ? 'WIN' : game?.status === 'Draw' ? 'DRAW' : 'YOUR MOVE'}</span>
          </div>

          <div className="board" role="grid" aria-label="Tic Tac Toe board">
            {Array.from({ length: 9 }, (_, index) => {
              const value = game?.board[index] ?? null
              const won = game?.winningCells.includes(index) ?? false
              return (
                <button
                  key={index}
                  className={`cell ${value === 'X' ? 'cell-x' : value === 'O' ? 'cell-o' : ''} ${won ? 'cell-winner' : ''}`}
                  role="gridcell"
                  aria-label={`Row ${Math.floor(index / 3) + 1}, column ${index % 3 + 1}${value ? `, ${value}` : ', empty'}`}
                  aria-disabled={!game || Boolean(value) || finished || busy}
                  disabled={!game || Boolean(value) || finished || busy}
                  onClick={() => game && void perform(() => api.play(game.id, game.currentPlayer, Math.floor(index / 3) + 1, index % 3 + 1), true)}
                >
                  {value && <span>{value}</span>}
                </button>
              )
            })}
          </div>

          <div className="board-footer">
            <div className="player-key"><span className="key-x">X</span><span>YOU</span><span className="key-o">O</span><span>{mode === 'Computer' ? 'COMPUTER' : 'PLAYER 2'}</span></div>
            <span className="coordinate-note">3 × 3 · FIRST TO THREE</span>
          </div>
        </div>

        <aside className="sidebar">
          <section className="score-section" aria-labelledby="score-heading">
            <div className="section-heading"><div><p className="eyebrow">KEEP THE TALLY</p><h2 id="score-heading">Scoreboard</h2></div><span className="session-chip">THIS SESSION</span></div>
            <div className="score-grid">
              <div className="score-item"><span className="score-player x-text">X</span><strong>{scoreboard.xWins}</strong><span className="score-caption">WINS</span></div>
              <div className="score-item"><span className="score-player o-text">O</span><strong>{scoreboard.oWins}</strong><span className="score-caption">WINS</span></div>
              <div className="score-item draw-score"><span className="score-player draw-text">=</span><strong>{scoreboard.draws}</strong><span className="score-caption">DRAWS</span></div>
            </div>
            <button className="text-action" onClick={() => void resetScoreboard()} disabled={busy}>↺ <span>Reset scoreboard</span></button>
          </section>

          <section className="history-section" aria-labelledby="history-heading">
            <div className="section-heading"><div><p className="eyebrow">PLAY BY PLAY</p><h2 id="history-heading">Move history</h2></div><span className="move-count">{String(game?.history.length ?? 0).padStart(2, '0')}</span></div>
            <div className="history-table">
              <div className="history-head"><span>MOVE</span><span>PLAYER</span><span>POSITION</span></div>
              {game?.history.length ? [...game.history].reverse().map((move) => (
                <div className="history-row" key={`${move.moveNumber}-${move.player}`}>
                  <span className="move-number">{String(move.moveNumber).padStart(2, '0')}</span>
                  <span className={move.player === 'X' ? 'history-player x-text' : 'history-player o-text'}>{move.player}</span>
                  <span className="position-label">R{move.row} / C{move.column}</span>
                </div>
              )) : <p className="empty-history">Your first move starts the story.</p>}
            </div>
          </section>

          <div className="controls">
            <button className="undo-button" onClick={() => game && void perform(() => api.undo(game.id))} disabled={!game?.history.length || finished || busy}>
              <span aria-hidden="true">↶</span> Undo last move
            </button>
            <button className="reset-button" onClick={() => game && void perform(() => api.reset(game.id))} disabled={!game || busy}>
              <span aria-hidden="true">↻</span> New round
            </button>
          </div>
          <p className="undo-note">{mode === 'Computer' ? 'Undo removes your move and the computer reply.' : 'Undo takes back the last move.'} Disabled once a round is complete.</p>
        </aside>
      </section>

      {error && <div className="error-banner" role="alert"><span>!</span>{error}<button onClick={() => setError('')} aria-label="Dismiss error">×</button></div>}
      <footer className="page-footer"><span>ONE BOARD. NINE SQUARES. ENDLESS REMATCHES.</span><span>BUILT FOR THE LOVE OF THE GAME</span></footer>
    </main>
  )
}

function errorMessage(reason: unknown) {
  return reason instanceof Error ? reason.message : 'Something went wrong. Check the API connection and try again.'
}

export default App

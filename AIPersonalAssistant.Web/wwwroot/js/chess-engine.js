// Chess Engine Wrapper - Stockfish WASM via Web Worker
class ChessEngine {
    constructor() {
        this.worker = null;
        this.isReady = false;
        this.pendingCallbacks = [];
        this.onEvalUpdate = null;
    }

    init() {
        return new Promise((resolve, reject) => {
            try {
                this.worker = new Worker('https://cdn.jsdelivr.net/npm/stockfish.js@10.0.2/stockfish.js');
                
                this.worker.onmessage = (e) => {
                    this.handleMessage(e.data);
                };

                this.worker.onerror = (e) => {
                    console.error('Stockfish worker error:', e);
                    reject(e);
                };

                // Wait for initial ready
                this.sendCommand('uci');
                this.waitFor('uciok').then(() => {
                    this.sendCommand('isready');
                    return this.waitFor('readyok');
                }).then(() => {
                    this.isReady = true;
                    resolve();
                }).catch(reject);
            } catch (err) {
                reject(err);
            }
        });
    }

    sendCommand(cmd) {
        if (this.worker) {
            this.worker.postMessage(cmd);
        }
    }

    waitFor(token) {
        return new Promise((resolve) => {
            this.pendingCallbacks.push({ token, resolve });
        });
    }

    handleMessage(data) {
        const line = typeof data === 'string' ? data : '';
        
        // Check pending callbacks
        for (let i = this.pendingCallbacks.length - 1; i >= 0; i--) {
            if (line.includes(this.pendingCallbacks[i].token)) {
                this.pendingCallbacks[i].resolve(line);
                this.pendingCallbacks.splice(i, 1);
            }
        }

        // Parse evaluation info
        if (line.startsWith('info') && this.onEvalUpdate) {
            const info = this.parseInfo(line);
            if (info) this.onEvalUpdate(info);
        }

        // Parse bestmove
        if (line.startsWith('bestmove')) {
            const parts = line.split(' ');
            const move = parts[1];
            if (this._bestMoveResolve) {
                this._bestMoveResolve(move);
                this._bestMoveResolve = null;
            }
        }
    }

    parseInfo(line) {
        const info = {};
        const parts = line.split(' ');
        
        for (let i = 0; i < parts.length; i++) {
            if (parts[i] === 'depth') info.depth = parseInt(parts[i + 1]);
            if (parts[i] === 'score') {
                if (parts[i + 1] === 'cp') {
                    info.score = parseInt(parts[i + 2]);
                    info.scoreType = 'cp';
                } else if (parts[i + 1] === 'mate') {
                    info.score = parseInt(parts[i + 2]);
                    info.scoreType = 'mate';
                }
            }
            if (parts[i] === 'multipv') info.multipv = parseInt(parts[i + 1]);
            if (parts[i] === 'pv') {
                info.pv = parts.slice(i + 1);
                break;
            }
        }
        
        return info.depth ? info : null;
    }

    setPosition(fen, moves = []) {
        if (moves.length > 0) {
            this.sendCommand(`position fen ${fen} moves ${moves.join(' ')}`);
        } else {
            this.sendCommand(`position fen ${fen}`);
        }
    }

    setStartPosition(moves = []) {
        if (moves.length > 0) {
            this.sendCommand(`position startpos moves ${moves.join(' ')}`);
        } else {
            this.sendCommand('position startpos');
        }
    }

    getBestMove(depth = 12) {
        return new Promise((resolve) => {
            this._bestMoveResolve = resolve;
            this.sendCommand(`go depth ${depth}`);
        });
    }

    getEvaluation(fen, depth = 15, multiPV = 3) {
        return new Promise((resolve) => {
            const lines = [];
            
            this.onEvalUpdate = (info) => {
                if (info.depth === depth && info.pv) {
                    lines.push(info);
                }
            };

            this.setPosition(fen);
            this.sendCommand(`setoption name MultiPV value ${multiPV}`);
            this.sendCommand(`go depth ${depth}`);
            
            // Resolve when bestmove comes
            const origResolve = this._bestMoveResolve;
            this._bestMoveResolve = (bestMove) => {
                this.onEvalUpdate = null;
                this.sendCommand('setoption name MultiPV value 1');
                resolve({ bestMove, lines });
                if (origResolve) origResolve(bestMove);
            };
        });
    }

    setDifficulty(level) {
        // level 1-5 mapping to Stockfish settings
        const settings = {
            1: { depth: 1, skillLevel: 0 },    // Beginner
            2: { depth: 3, skillLevel: 5 },    // Casual
            3: { depth: 8, skillLevel: 10 },   // Intermediate
            4: { depth: 14, skillLevel: 15 },  // Advanced
            5: { depth: 20, skillLevel: 20 }   // Grandmaster
        };
        const s = settings[level] || settings[3];
        this.currentDepth = s.depth;
        this.sendCommand(`setoption name Skill Level value ${s.skillLevel}`);
    }

    stop() {
        this.sendCommand('stop');
    }

    newGame() {
        this.sendCommand('ucinewgame');
        this.sendCommand('isready');
    }

    destroy() {
        if (this.worker) {
            this.worker.terminate();
            this.worker = null;
            this.isReady = false;
        }
    }
}

// Export for use in other modules
window.ChessEngine = ChessEngine;

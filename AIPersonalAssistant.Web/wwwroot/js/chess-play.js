// Chess Play Mode - Play vs AI
(function () {
    let board = null;
    let game = null;
    let engine = null;
    let engineReady = false;
    let playerColor = 'w';
    let aiColor = 'b';
    let gameActive = false;
    let hintSquares = [];
    let initialized = false;

    const PIECE_THEME = 'https://unpkg.com/@chrisoakman/chessboardjs@1.0.0/dist/img/chesspieces/wikipedia/{piece}.png';
    const STATS_KEY = 'chess-play-stats';

    function loadStats() {
        try {
            return JSON.parse(localStorage.getItem(STATS_KEY)) || { wins: 0, losses: 0, draws: 0 };
        } catch { return { wins: 0, losses: 0, draws: 0 }; }
    }

    function saveStats(stats) {
        localStorage.setItem(STATS_KEY, JSON.stringify(stats));
    }

    function getDifficulty() {
        return parseInt(document.getElementById('difficultySelect').value) || 3;
    }

    function getDepthForDifficulty() {
        const map = { 1: 1, 2: 3, 3: 8, 4: 14, 5: 20 };
        return map[getDifficulty()] || 8;
    }

    async function ensureEngine() {
        if (!engine) {
            engine = new ChessEngine();
        }
        if (!engineReady) {
            try {
                await engine.init();
                engineReady = true;
            } catch (e) {
                console.error('Engine init failed:', e);
                updateStatus('Engine failed to load. Refresh to retry.');
            }
        }
    }

    function updateStatus(msg) {
        document.getElementById('gameStatus').textContent = msg;
    }

    function updateMoveList() {
        const history = game.history();
        const el = document.getElementById('moveList');
        let html = '';
        for (let i = 0; i < history.length; i += 2) {
            const num = Math.floor(i / 2) + 1;
            html += `<span class="move-pair"><span class="move-num">${num}.</span> `;
            html += `<span class="move">${history[i]}</span>`;
            if (history[i + 1]) {
                html += ` <span class="move">${history[i + 1]}</span>`;
            }
            html += '</span> ';
        }
        el.innerHTML = html;
        el.scrollTop = el.scrollHeight;
    }

    function checkGameState() {
        if (game.in_checkmate()) {
            const winner = game.turn() === 'w' ? 'Black' : 'White';
            updateStatus(`Checkmate! ${winner} wins.`);
            endGame(game.turn() === playerColor ? 'loss' : 'win');
            return true;
        }
        if (game.in_stalemate()) {
            updateStatus('Stalemate! Draw.');
            endGame('draw');
            return true;
        }
        if (game.in_threefold_repetition()) {
            updateStatus('Threefold repetition! Draw.');
            endGame('draw');
            return true;
        }
        if (game.insufficient_material()) {
            updateStatus('Insufficient material! Draw.');
            endGame('draw');
            return true;
        }
        if (game.in_draw()) {
            updateStatus('Draw!');
            endGame('draw');
            return true;
        }
        if (game.in_check()) {
            const turn = game.turn() === 'w' ? 'White' : 'Black';
            updateStatus(`${turn} is in check.`);
        } else {
            const turn = game.turn() === 'w' ? 'White' : 'Black';
            updateStatus(`${turn} to move.`);
        }
        return false;
    }

    function endGame(result) {
        gameActive = false;
        document.getElementById('undoBtn').disabled = true;
        document.getElementById('resignBtn').disabled = true;

        const stats = loadStats();
        if (result === 'win') stats.wins++;
        else if (result === 'loss') stats.losses++;
        else stats.draws++;
        saveStats(stats);

        showPostGame(result);
    }

    function showPostGame(result) {
        const totalMoves = game.history().length;
        const fullMoves = Math.ceil(totalMoves / 2);
        let resultText = '';
        if (result === 'win') resultText = '🎉 You won!';
        else if (result === 'loss') resultText = '😞 You lost.';
        else resultText = '🤝 Draw.';

        const stats = loadStats();
        const el = document.getElementById('postGameAnalysis');
        el.style.display = 'block';
        document.getElementById('analysisResults').innerHTML = `
            <div class="analysis-summary">
                <p><strong>${resultText}</strong></p>
                <p>Total moves: ${fullMoves}</p>
                <p>Difficulty: ${document.getElementById('difficultySelect').selectedOptions[0].text}</p>
                <hr>
                <p><strong>Your Stats:</strong> ${stats.wins}W / ${stats.losses}L / ${stats.draws}D</p>
            </div>
        `;
    }

    function clearHints() {
        if (hintSquares.length > 0) {
            const boardEl = document.getElementById('playBoard');
            boardEl.querySelectorAll('.highlight-hint').forEach(el => el.remove());
            hintSquares = [];
        }
    }

    function highlightSquare(square, color) {
        const boardEl = document.getElementById('playBoard');
        const squareEl = boardEl.querySelector(`.square-${square}`);
        if (squareEl) {
            const highlight = document.createElement('div');
            highlight.className = 'highlight-hint';
            highlight.style.cssText = `position:absolute;top:0;left:0;width:100%;height:100%;background:${color};opacity:0.4;pointer-events:none;z-index:10;`;
            squareEl.style.position = 'relative';
            squareEl.appendChild(highlight);
            hintSquares.push(square);
        }
    }

    function onDragStart(source, piece) {
        if (!gameActive) return false;
        if (game.game_over()) return false;
        if (game.turn() !== playerColor) return false;
        if ((playerColor === 'w' && piece.search(/^b/) !== -1) ||
            (playerColor === 'b' && piece.search(/^w/) !== -1)) {
            return false;
        }
    }

    function onDrop(source, target) {
        clearHints();
        const move = game.move({ from: source, to: target, promotion: 'q' });
        if (move === null) return 'snapback';

        updateMoveList();
        if (!checkGameState()) {
            document.getElementById('undoBtn').disabled = false;
            setTimeout(makeAiMove, 300);
        }
    }

    function onSnapEnd() {
        board.position(game.fen());
    }

    async function makeAiMove() {
        if (!gameActive || game.game_over() || game.turn() !== aiColor) return;

        updateStatus('AI is thinking...');
        await ensureEngine();
        engine.setPosition(game.fen());
        const bestMove = await engine.getBestMove(getDepthForDifficulty());

        if (!bestMove || bestMove === '(none)') return;

        const from = bestMove.substring(0, 2);
        const to = bestMove.substring(2, 4);
        const promotion = bestMove.length > 4 ? bestMove[4] : undefined;

        const moveObj = { from, to };
        if (promotion) moveObj.promotion = promotion;
        game.move(moveObj);

        board.position(game.fen());
        updateMoveList();
        checkGameState();
    }

    function startNewGame() {
        clearHints();
        document.getElementById('postGameAnalysis').style.display = 'none';

        game = new Chess();

        const colorChoice = document.getElementById('colorSelect').value;
        if (colorChoice === 'random') {
            playerColor = Math.random() < 0.5 ? 'w' : 'b';
        } else {
            playerColor = colorChoice === 'white' ? 'w' : 'b';
        }
        aiColor = playerColor === 'w' ? 'b' : 'w';

        const orientation = playerColor === 'w' ? 'white' : 'black';

        if (board) board.destroy();

        board = Chessboard('playBoard', {
            draggable: true,
            position: 'start',
            orientation: orientation,
            pieceTheme: PIECE_THEME,
            onDragStart: onDragStart,
            onDrop: onDrop,
            onSnapEnd: onSnapEnd
        });

        ensureEngine().then(() => {
            engine.newGame();
            engine.setDifficulty(getDifficulty());
        });

        gameActive = true;
        document.getElementById('moveList').innerHTML = '';
        document.getElementById('undoBtn').disabled = true;
        document.getElementById('resignBtn').disabled = false;

        if (playerColor === 'w') {
            updateStatus('White to move. Your turn!');
        } else {
            updateStatus('AI is thinking...');
            setTimeout(makeAiMove, 500);
        }
    }

    async function showHint() {
        if (!gameActive || game.turn() !== playerColor) return;

        clearHints();
        await ensureEngine();
        engine.setPosition(game.fen());
        const bestMove = await engine.getBestMove(getDepthForDifficulty());
        if (!bestMove || bestMove === '(none)') return;

        const from = bestMove.substring(0, 2);
        const to = bestMove.substring(2, 4);
        highlightSquare(from, '#22c55e');
        highlightSquare(to, '#3b82f6');
    }

    function undoMove() {
        if (!gameActive || game.history().length < 2) return;
        game.undo(); // undo AI move
        game.undo(); // undo player move
        board.position(game.fen());
        updateMoveList();
        checkGameState();
        if (game.history().length < 2) {
            document.getElementById('undoBtn').disabled = true;
        }
    }

    function resign() {
        if (!gameActive) return;
        updateStatus('You resigned.');
        endGame('loss');
    }

    window.initPlayMode = function () {
        if (initialized) return;
        initialized = true;

        if (typeof Chessboard === 'undefined') {
            document.getElementById('gameStatus').textContent = 'Chess libraries failed to load. Please check your internet connection and refresh.';
            return;
        }

        document.getElementById('newGameBtn').addEventListener('click', startNewGame);
        document.getElementById('hintBtn').addEventListener('click', showHint);
        document.getElementById('undoBtn').addEventListener('click', undoMove);
        document.getElementById('resignBtn').addEventListener('click', resign);

        // Render initial board so it's not blank before clicking New Game
        game = new Chess();
        board = Chessboard('playBoard', {
            position: 'start',
            pieceTheme: PIECE_THEME,
            draggable: false
        });

        window.addEventListener('resize', () => {
            if (board) board.resize();
        });
    };
})();

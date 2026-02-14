// Chess Analysis Board Mode
(function () {
    let analysisBoard = null;
    let analysisGame = null;
    let engine = null;
    let engineReady = false;
    let moveHistory = [];
    let currentMoveIndex = -1;
    let positions = [];
    let evalPending = false;

    const pieceTheme = 'https://unpkg.com/@chrisoakman/chessboardjs@1.0.0/dist/img/chesspieces/wikipedia/{piece}.png';

    function initAnalysisMode() {
        analysisGame = new Chess();
        analysisBoard = Chessboard('analysisBoard', {
            draggable: true,
            position: 'start',
            pieceTheme: pieceTheme,
            onDrop: onAnalysisDrop,
            onSnapEnd: onSnapEnd
        });

        document.getElementById('loadPgnBtn')?.addEventListener('click', loadPgn);
        document.getElementById('analysisStartBtn')?.addEventListener('click', () => goToMove(-1));
        document.getElementById('analysisPrevBtn')?.addEventListener('click', () => goToMove(currentMoveIndex - 1));
        document.getElementById('analysisNextBtn')?.addEventListener('click', () => goToMove(currentMoveIndex + 1));
        document.getElementById('analysisEndBtn')?.addEventListener('click', () => goToMove(moveHistory.length - 1));

        initEngine();
    }

    async function initEngine() {
        if (typeof ChessEngine === 'undefined') {
            showEngineUnavailable();
            return;
        }
        try {
            engine = new ChessEngine();
            await engine.init();
            engineReady = true;
        } catch (e) {
            console.error('Engine init failed:', e);
            showEngineUnavailable();
        }
    }

    function showEngineUnavailable() {
        const scoreEl = document.getElementById('evalScore');
        const linesEl = document.getElementById('topLines');
        if (scoreEl) scoreEl.textContent = 'Engine unavailable';
        if (linesEl) linesEl.innerHTML = '<div class="text-muted">Engine unavailable</div>';
    }

    function isFen(input) {
        const trimmed = input.trim();
        const parts = trimmed.split(/\s+/);
        return parts.length >= 2 && trimmed.includes('/');
    }

    function loadPgn() {
        const input = document.getElementById('pgnInput')?.value?.trim();
        if (!input) return;

        analysisGame = new Chess();
        moveHistory = [];
        positions = [];
        currentMoveIndex = -1;

        if (isFen(input)) {
            const loaded = analysisGame.load(input);
            if (!loaded) {
                alert('Invalid FEN string');
                return;
            }
            positions = [analysisGame.fen()];
            analysisBoard.position(analysisGame.fen());
            renderMoveList();
            evaluatePosition();
            return;
        }

        const loaded = analysisGame.load_pgn(input);
        if (!loaded) {
            alert('Invalid PGN');
            return;
        }

        const history = analysisGame.history();
        analysisGame.reset();
        positions = [analysisGame.fen()];

        for (const move of history) {
            analysisGame.move(move);
            moveHistory.push(move);
            positions.push(analysisGame.fen());
        }

        currentMoveIndex = moveHistory.length - 1;
        analysisBoard.position(analysisGame.fen());
        renderMoveList();
        evaluatePosition();
    }

    function goToMove(index) {
        if (moveHistory.length === 0) return;
        index = Math.max(-1, Math.min(index, moveHistory.length - 1));
        currentMoveIndex = index;
        const fen = positions[index + 1];
        analysisGame.load(fen);
        analysisBoard.position(fen);
        highlightCurrentMove();
        evaluatePosition();
    }

    function renderMoveList() {
        const container = document.getElementById('analysisMoveList');
        if (!container) return;
        container.innerHTML = '';

        for (let i = 0; i < moveHistory.length; i++) {
            const moveNum = Math.floor(i / 2) + 1;
            const isWhite = i % 2 === 0;

            if (isWhite) {
                const numSpan = document.createElement('span');
                numSpan.className = 'move-number';
                numSpan.textContent = moveNum + '.';
                container.appendChild(numSpan);
            }

            const moveSpan = document.createElement('span');
            moveSpan.className = 'move-link' + (i === currentMoveIndex ? ' active' : '');
            moveSpan.textContent = moveHistory[i];
            moveSpan.dataset.index = i;
            moveSpan.style.cursor = 'pointer';
            moveSpan.addEventListener('click', () => goToMove(i));
            container.appendChild(moveSpan);
            container.appendChild(document.createTextNode(' '));
        }
        highlightCurrentMove();
    }

    function highlightCurrentMove() {
        const container = document.getElementById('analysisMoveList');
        if (!container) return;
        container.querySelectorAll('.move-link').forEach(el => {
            el.classList.toggle('active', parseInt(el.dataset.index) === currentMoveIndex);
        });
    }

    function onAnalysisDrop(source, target) {
        const move = analysisGame.move({
            from: source,
            to: target,
            promotion: 'q'
        });
        if (move === null) return 'snapback';

        // Truncate future moves if navigated back
        if (currentMoveIndex < moveHistory.length - 1) {
            moveHistory = moveHistory.slice(0, currentMoveIndex + 1);
            positions = positions.slice(0, currentMoveIndex + 2);
        }

        moveHistory.push(move.san);
        positions.push(analysisGame.fen());
        currentMoveIndex = moveHistory.length - 1;
        renderMoveList();
        evaluatePosition();
    }

    function onSnapEnd() {
        analysisBoard.position(analysisGame.fen());
    }

    async function evaluatePosition() {
        if (!engineReady || !engine) return;
        if (evalPending) {
            engine.stop();
        }

        evalPending = true;
        const fen = analysisGame.fen();
        const isBlackTurn = fen.split(' ')[1] === 'b';

        try {
            const result = await engine.getEvaluation(fen, 12, 3);
            evalPending = false;

            if (analysisGame.fen() !== fen) return; // Position changed during eval

            const lines = result.lines || [];
            const topLine = lines.find(l => l.multipv === 1) || lines[0];

            if (topLine) {
                let score = topLine.score;
                const isMate = topLine.scoreType === 'mate';

                // Flip score for black's perspective (engine reports from side to move)
                if (isBlackTurn) score = -score;

                updateEvalBar(score, isMate);
                updateEvalScore(score, isMate);
            }

            updateEngineLines(lines, isBlackTurn);
        } catch (e) {
            evalPending = false;
            console.error('Eval error:', e);
        }
    }

    function updateEvalBar(score, isMate) {
        const fill = document.getElementById('evalBarFill');
        if (!fill) return;

        let pct;
        if (isMate) {
            pct = score > 0 ? 100 : 0;
        } else {
            // Cap at ±1000 cp (±10 pawns), map to 0-100%
            const capped = Math.max(-1000, Math.min(1000, score));
            pct = 50 + (capped / 1000) * 50;
        }
        fill.style.height = pct + '%';
    }

    function updateEvalScore(score, isMate) {
        const el = document.getElementById('evalScore');
        if (!el) return;

        if (isMate) {
            el.textContent = (score > 0 ? 'M' : '-M') + Math.abs(score);
        } else {
            const pawns = score / 100;
            el.textContent = (pawns >= 0 ? '+' : '') + pawns.toFixed(1);
        }
    }

    function updateEngineLines(lines, isBlackTurn) {
        const container = document.getElementById('topLines');
        if (!container) return;

        if (lines.length === 0) {
            container.innerHTML = '<div class="text-muted">Analyzing...</div>';
            return;
        }

        // Sort by multipv
        const sorted = [...lines].sort((a, b) => (a.multipv || 1) - (b.multipv || 1));
        container.innerHTML = '';

        for (const line of sorted.slice(0, 3)) {
            let score = line.score;
            if (isBlackTurn) score = -score;

            const div = document.createElement('div');
            div.className = 'engine-line';

            let scoreText;
            if (line.scoreType === 'mate') {
                scoreText = (score > 0 ? 'M' : '-M') + Math.abs(score);
            } else {
                const pawns = score / 100;
                scoreText = (pawns >= 0 ? '+' : '') + pawns.toFixed(1);
            }

            const pv = (line.pv || []).slice(0, 8).join(' ');
            div.innerHTML = `<strong>${scoreText}</strong> ${pv}`;
            container.appendChild(div);
        }
    }

    window.initAnalysisMode = initAnalysisMode;
})();

// Chess Puzzles Mode
(function () {
    'use strict';

    const PIECE_THEME = 'https://unpkg.com/@chrisoakman/chessboardjs@1.0.0/dist/img/chesspieces/wikipedia/{piece}.png';
    const LS_RATING = 'chess-puzzle-rating';
    const LS_SOLVED = 'chess-puzzles-solved';
    const LS_STREAK = 'chess-puzzle-streak';

    let puzzles = [];
    let currentPuzzle = null;
    let solutionIndex = 0;
    let puzzleBoard = null;
    let puzzleGame = null;
    let currentDifficulty = 'all';
    let solvedPuzzleIds = new Set();
    let puzzleActive = false;

    function loadStats() {
        const rating = parseInt(localStorage.getItem(LS_RATING)) || 1200;
        const solved = parseInt(localStorage.getItem(LS_SOLVED)) || 0;
        const streak = parseInt(localStorage.getItem(LS_STREAK)) || 0;
        updateStatsUI(rating, solved, streak);
        return { rating, solved, streak };
    }

    function saveStats(rating, solved, streak) {
        localStorage.setItem(LS_RATING, rating);
        localStorage.setItem(LS_SOLVED, solved);
        localStorage.setItem(LS_STREAK, streak);
        updateStatsUI(rating, solved, streak);
    }

    function updateStatsUI(rating, solved, streak) {
        const ratingEl = document.getElementById('puzzleRating');
        const solvedEl = document.getElementById('puzzlesSolved');
        const streakEl = document.getElementById('puzzleStreak');
        if (ratingEl) ratingEl.textContent = rating;
        if (solvedEl) solvedEl.textContent = solved;
        if (streakEl) streakEl.textContent = streak;
    }

    function showFeedback(message, type) {
        const el = document.getElementById('puzzleFeedback');
        if (!el) return;
        el.textContent = message;
        el.className = 'puzzle-feedback ' + type;
        if (type === 'correct' || type === 'wrong') {
            setTimeout(() => {
                if (el.textContent === message) el.className = 'puzzle-feedback';
            }, 3000);
        }
    }

    function getFilteredPuzzles() {
        if (currentDifficulty === 'all') return puzzles;
        return puzzles.filter(p => p.difficulty === currentDifficulty);
    }

    function pickNextPuzzle() {
        const filtered = getFilteredPuzzles();
        const unsolved = filtered.filter(p => !solvedPuzzleIds.has(p.id));
        const pool = unsolved.length > 0 ? unsolved : filtered;
        if (pool.length === 0) return null;
        return pool[Math.floor(Math.random() * pool.length)];
    }

    function loadPuzzle(puzzle) {
        if (!puzzle) {
            showFeedback('No puzzles available for this difficulty.', 'wrong');
            return;
        }
        currentPuzzle = puzzle;
        solutionIndex = 0;
        puzzleActive = true;

        puzzleGame = new Chess(puzzle.fen);

        const turn = puzzleGame.turn() === 'w' ? 'White' : 'Black';
        const orientation = puzzleGame.turn() === 'w' ? 'white' : 'black';

        const titleEl = document.getElementById('puzzleTitle');
        const themeEl = document.getElementById('puzzleTheme');
        const turnEl = document.getElementById('puzzleTurn');
        if (titleEl) titleEl.textContent = puzzle.description || 'Puzzle';
        if (themeEl) themeEl.textContent = '🏷️ ' + puzzle.theme;
        if (turnEl) turnEl.textContent = '⬜ ' + turn + ' to move';

        showFeedback('', '');

        if (puzzleBoard) {
            puzzleBoard.orientation(orientation);
            puzzleBoard.position(puzzle.fen, false);
        }

        const showBtn = document.getElementById('showSolutionBtn');
        if (showBtn) showBtn.disabled = false;
    }

    function uciToMove(uci) {
        if (!uci || uci.length < 4) return null;
        return {
            from: uci.substring(0, 2),
            to: uci.substring(2, 4),
            promotion: uci.length > 4 ? uci[4] : undefined
        };
    }

    function onDrop(source, target) {
        if (!puzzleActive || !currentPuzzle) return 'snapback';

        const expectedUci = currentPuzzle.solution[solutionIndex];
        const expected = uciToMove(expectedUci);
        if (!expected) return 'snapback';

        if (source !== expected.from || target !== expected.to) {
            // Wrong move
            const stats = loadStats();
            const newRating = Math.max(100, stats.rating - 10);
            saveStats(newRating, stats.solved, 0);
            showFeedback('❌ Try again', 'wrong');
            puzzleActive = false;
            return 'snapback';
        }

        // Correct move
        const moveResult = puzzleGame.move({
            from: source,
            to: target,
            promotion: expected.promotion || 'q'
        });

        if (!moveResult) return 'snapback';

        solutionIndex++;

        if (solutionIndex >= currentPuzzle.solution.length) {
            // Puzzle solved
            puzzleActive = false;
            const stats = loadStats();
            const newRating = stats.rating + 15;
            const newSolved = stats.solved + 1;
            const newStreak = stats.streak + 1;
            saveStats(newRating, newSolved, newStreak);
            solvedPuzzleIds.add(currentPuzzle.id);
            showFeedback('✅ Correct!', 'correct');
        } else {
            // Play opponent response
            showFeedback('✅ Good move!', 'correct');
            setTimeout(() => {
                const opponentUci = currentPuzzle.solution[solutionIndex];
                const opMove = uciToMove(opponentUci);
                if (opMove) {
                    puzzleGame.move({
                        from: opMove.from,
                        to: opMove.to,
                        promotion: opMove.promotion || 'q'
                    });
                    puzzleBoard.position(puzzleGame.fen());
                    solutionIndex++;

                    if (solutionIndex >= currentPuzzle.solution.length) {
                        puzzleActive = false;
                        const stats = loadStats();
                        const newRating = stats.rating + 15;
                        const newSolved = stats.solved + 1;
                        const newStreak = stats.streak + 1;
                        saveStats(newRating, newSolved, newStreak);
                        solvedPuzzleIds.add(currentPuzzle.id);
                        showFeedback('✅ Correct!', 'correct');
                    } else {
                        showFeedback('Your turn...', '');
                    }
                }
            }, 400);
        }
    }

    function onDragStart(source, piece) {
        if (!puzzleActive) return false;
        if (!puzzleGame) return false;
        if (puzzleGame.game_over()) return false;
        if ((puzzleGame.turn() === 'w' && piece.search(/^b/) !== -1) ||
            (puzzleGame.turn() === 'b' && piece.search(/^w/) !== -1)) {
            return false;
        }
    }

    function onSnapEnd() {
        if (puzzleBoard && puzzleGame) {
            puzzleBoard.position(puzzleGame.fen());
        }
    }

    function showSolution() {
        if (!currentPuzzle) return;
        puzzleActive = false;

        const showBtn = document.getElementById('showSolutionBtn');
        if (showBtn) showBtn.disabled = true;

        const tempGame = new Chess(currentPuzzle.fen);
        let moveText = [];

        for (const uci of currentPuzzle.solution) {
            const m = uciToMove(uci);
            if (m) {
                const result = tempGame.move({ from: m.from, to: m.to, promotion: m.promotion || 'q' });
                if (result) moveText.push(result.san);
            }
        }

        showFeedback('Solution: ' + moveText.join(' → '), 'solution');

        // Animate the solution
        const animGame = new Chess(currentPuzzle.fen);
        let i = 0;
        function playNext() {
            if (i >= currentPuzzle.solution.length) return;
            const uci = currentPuzzle.solution[i];
            const m = uciToMove(uci);
            if (m) {
                animGame.move({ from: m.from, to: m.to, promotion: m.promotion || 'q' });
                puzzleBoard.position(animGame.fen());
            }
            i++;
            if (i < currentPuzzle.solution.length) {
                setTimeout(playNext, 700);
            }
        }
        setTimeout(playNext, 300);
    }

    function initBoard() {
        const boardEl = document.getElementById('puzzleBoard');
        if (!boardEl) return;

        const config = {
            draggable: true,
            position: 'start',
            pieceTheme: PIECE_THEME,
            onDragStart: onDragStart,
            onDrop: onDrop,
            onSnapEnd: onSnapEnd
        };

        puzzleBoard = Chessboard('puzzleBoard', config);

        $(window).on('resize', function () {
            if (puzzleBoard) puzzleBoard.resize();
        });
    }

    function bindEvents() {
        const nextBtn = document.getElementById('nextPuzzleBtn');
        if (nextBtn) {
            nextBtn.addEventListener('click', function () {
                const puzzle = pickNextPuzzle();
                loadPuzzle(puzzle);
            });
        }

        const showBtn = document.getElementById('showSolutionBtn');
        if (showBtn) {
            showBtn.addEventListener('click', showSolution);
        }

        document.querySelectorAll('.pill-btn[data-difficulty]').forEach(btn => {
            btn.addEventListener('click', function () {
                document.querySelectorAll('.pill-btn[data-difficulty]').forEach(b => b.classList.remove('active'));
                this.classList.add('active');
                currentDifficulty = this.getAttribute('data-difficulty');
                const puzzle = pickNextPuzzle();
                loadPuzzle(puzzle);
            });
        });
    }

    window.initPuzzleMode = function () {
        loadStats();

        if (puzzles.length > 0) {
            if (!puzzleBoard) {
                initBoard();
                bindEvents();
            }
            const puzzle = pickNextPuzzle();
            loadPuzzle(puzzle);
            return;
        }

        fetch('/data/chess-puzzles.json')
            .then(res => res.json())
            .then(data => {
                puzzles = data;
                initBoard();
                bindEvents();
                const puzzle = pickNextPuzzle();
                loadPuzzle(puzzle);
            })
            .catch(err => {
                console.error('Failed to load puzzles:', err);
                showFeedback('Failed to load puzzles.', 'wrong');
            });
    };
})();

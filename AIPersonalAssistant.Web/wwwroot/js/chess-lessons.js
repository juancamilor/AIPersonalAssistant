// Chess Lessons Mode
(function () {
    let board = null;
    let game = null;
    let lessons = [];
    let currentLesson = null;
    let currentStepIndex = 0;
    let initialized = false;

    const PIECE_THEME = 'https://unpkg.com/@chrisoakman/chessboardjs@1.0.0/dist/img/chesspieces/wikipedia/{piece}.png';
    const COMPLETED_KEY = 'chess-lessons-completed';

    function getCompleted() {
        try {
            return JSON.parse(localStorage.getItem(COMPLETED_KEY)) || [];
        } catch { return []; }
    }

    function saveCompleted(arr) {
        localStorage.setItem(COMPLETED_KEY, JSON.stringify(arr));
    }

    function markCompleted(lessonId) {
        const completed = getCompleted();
        if (!completed.includes(lessonId)) {
            completed.push(lessonId);
            saveCompleted(completed);
        }
    }

    function renderLessonCards() {
        const container = document.getElementById('lessonsList');
        const completed = getCompleted();
        container.innerHTML = lessons.map(l => `
            <div class="lesson-card" data-lesson-id="${l.id}">
                <div class="lesson-card-icon">${l.icon}</div>
                <h4>${l.title}</h4>
                <p>${l.description}</p>
                ${completed.includes(l.id) ? '<span class="lesson-badge">✅ Completed</span>' : ''}
            </div>
        `).join('');

        container.querySelectorAll('.lesson-card').forEach(card => {
            card.addEventListener('click', () => {
                const id = card.dataset.lessonId;
                currentLesson = lessons.find(l => l.id === id);
                if (currentLesson) openLesson();
            });
        });
    }

    function openLesson() {
        currentStepIndex = 0;
        document.getElementById('lessonsList').style.display = 'none';
        document.getElementById('lessonView').style.display = '';
        document.getElementById('lessonTitle').textContent = currentLesson.title;

        if (board) board.destroy();
        game = new Chess();
        board = Chessboard('lessonBoard', {
            position: 'start',
            pieceTheme: PIECE_THEME,
            draggable: false
        });

        loadStep();
    }

    function closeLesson() {
        currentLesson = null;
        if (board) { board.destroy(); board = null; }
        document.getElementById('lessonView').style.display = 'none';
        document.getElementById('lessonsList').style.display = 'grid';
        renderLessonCards();
    }

    function loadStep() {
        if (!currentLesson) return;
        const step = currentLesson.steps[currentStepIndex];
        const total = currentLesson.steps.length;

        // Update progress
        document.getElementById('lessonProgress').textContent = `Step ${currentStepIndex + 1} of ${total}`;
        document.getElementById('lessonPrevBtn').disabled = currentStepIndex === 0;

        const isLast = currentStepIndex === total - 1;
        const nextBtn = document.getElementById('lessonNextBtn');
        nextBtn.textContent = isLast ? 'Finish ✓' : 'Next →';

        // Clear highlights
        clearHighlights();

        // Set board position
        if (step.fen) {
            game = new Chess(step.fen);
            board.position(step.fen, false);
        }

        // Render content
        const content = document.getElementById('lessonContent');
        if (step.exercise) {
            content.innerHTML = `
                <p>${step.text}</p>
                <div class="lesson-exercise-hint">🎯 <em>${step.exercise.hint}</em></div>
                <div id="lessonFeedback" class="lesson-feedback"></div>
            `;
            enableExercise(step.exercise);
        } else {
            content.innerHTML = `<p>${step.text}</p>`;
            disableDrag();
            // Apply highlights
            if (step.highlights && step.highlights.length > 0) {
                setTimeout(() => highlightSquares(step.highlights), 100);
            }
        }
    }

    function enableExercise(exercise) {
        if (board) board.destroy();
        board = Chessboard('lessonBoard', {
            position: game.fen(),
            pieceTheme: PIECE_THEME,
            draggable: true,
            onDragStart: onDragStart,
            onDrop: function (source, target) {
                return onDrop(source, target, exercise);
            },
            onSnapEnd: onSnapEnd
        });
    }

    function disableDrag() {
        if (board) board.destroy();
        board = Chessboard('lessonBoard', {
            position: game.fen(),
            pieceTheme: PIECE_THEME,
            draggable: false
        });
    }

    function onDragStart(source, piece) {
        if (game.game_over()) return false;
        // Only allow current side to move
        if ((game.turn() === 'w' && piece.search(/^b/) !== -1) ||
            (game.turn() === 'b' && piece.search(/^w/) !== -1)) {
            return false;
        }
    }

    function onDrop(source, target, exercise) {
        const uciMove = source + target;
        const correctUci = exercise.correctMove;

        // Try the move
        const move = game.move({ from: source, to: target, promotion: 'q' });
        if (move === null) return 'snapback';

        // Check if correct
        if (uciMove === correctUci) {
            showFeedback('✅ Correct!', 'success');
            disableDrag();
            setTimeout(() => {
                if (currentStepIndex < currentLesson.steps.length - 1) {
                    currentStepIndex++;
                    loadStep();
                } else {
                    finishLesson();
                }
            }, 800);
        } else {
            showFeedback('❌ Try again!', 'error');
            game.undo();
            return 'snapback';
        }
    }

    function onSnapEnd() {
        board.position(game.fen());
    }

    function showFeedback(msg, type) {
        const fb = document.getElementById('lessonFeedback');
        if (fb) {
            fb.textContent = msg;
            fb.className = 'lesson-feedback ' + type;
        }
    }

    function highlightSquares(squares) {
        squares.forEach(sq => {
            const el = document.querySelector(`#lessonBoard .square-${sq}`);
            if (el) el.classList.add('highlight-lesson');
        });
    }

    function clearHighlights() {
        document.querySelectorAll('#lessonBoard .highlight-lesson').forEach(el => {
            el.classList.remove('highlight-lesson');
        });
    }

    function finishLesson() {
        if (currentLesson) {
            markCompleted(currentLesson.id);
        }
        closeLesson();
    }

    function nextStep() {
        if (!currentLesson) return;
        if (currentStepIndex < currentLesson.steps.length - 1) {
            currentStepIndex++;
            loadStep();
        } else {
            finishLesson();
        }
    }

    function prevStep() {
        if (!currentLesson) return;
        if (currentStepIndex > 0) {
            currentStepIndex--;
            loadStep();
        }
    }

    window.initLessonMode = function () {
        if (initialized) return;
        initialized = true;

        if (typeof Chessboard === 'undefined' || typeof Chess === 'undefined') {
            document.getElementById('lessonsList').innerHTML =
                '<p style="color:red;">Chess libraries failed to load. Please check your internet connection and refresh.</p>';
            return;
        }

        // Bind nav buttons
        document.getElementById('backToLessonsBtn').addEventListener('click', closeLesson);
        document.getElementById('lessonNextBtn').addEventListener('click', nextStep);
        document.getElementById('lessonPrevBtn').addEventListener('click', prevStep);

        // Load lessons JSON
        fetch('/data/chess-lessons.json')
            .then(r => r.json())
            .then(data => {
                lessons = data;
                renderLessonCards();
            })
            .catch(err => {
                console.error('Failed to load chess lessons:', err);
                document.getElementById('lessonsList').innerHTML =
                    '<p style="color:red;">Failed to load lessons. Please try again.</p>';
            });
    };

    // Inject highlight CSS
    const style = document.createElement('style');
    style.textContent = `
        #lessonBoard .highlight-lesson {
            box-shadow: inset 0 0 0 4px #ffeb3b !important;
            background-color: rgba(255, 235, 59, 0.4) !important;
        }
        .lesson-card {
            border: 1px solid #444;
            border-radius: 8px;
            padding: 16px;
            cursor: pointer;
            transition: transform 0.15s, box-shadow 0.15s;
            text-align: center;
        }
        .lesson-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        }
        .lesson-card-icon { font-size: 2em; margin-bottom: 8px; }
        .lesson-card h4 { margin: 4px 0; }
        .lesson-card p { font-size: 0.85em; opacity: 0.8; margin: 4px 0 8px; }
        .lesson-badge { color: #4caf50; font-size: 0.85em; font-weight: bold; }
        .lessons-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: 16px;
            padding: 16px 0;
        }
        .lesson-exercise-hint { margin: 8px 0; color: #ffeb3b; }
        .lesson-feedback { margin-top: 8px; font-weight: bold; min-height: 24px; }
        .lesson-feedback.success { color: #4caf50; }
        .lesson-feedback.error { color: #f44336; }
    `;
    document.head.appendChild(style);
})();

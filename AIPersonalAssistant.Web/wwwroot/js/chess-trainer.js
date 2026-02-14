// Chess Trainer - Main Orchestrator
document.addEventListener('DOMContentLoaded', () => {
    // Back button
    document.getElementById('backBtn').addEventListener('click', () => {
        window.location.href = '/tools.html';
    });

    // Tab switching
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;

            tabBtns.forEach(b => b.classList.remove('active'));
            tabContents.forEach(c => c.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(`tab-${tab}`).classList.add('active');

            // Initialize mode if needed
            if (tab === 'play' && typeof initPlayMode === 'function') initPlayMode();
            if (tab === 'puzzles' && typeof initPuzzleMode === 'function') initPuzzleMode();
            if (tab === 'lessons' && typeof initLessonMode === 'function') initLessonMode();
            if (tab === 'analysis' && typeof initAnalysisMode === 'function') initAnalysisMode();
        });
    });

    // Initialize default mode (Play)
    if (typeof initPlayMode === 'function') initPlayMode();
});

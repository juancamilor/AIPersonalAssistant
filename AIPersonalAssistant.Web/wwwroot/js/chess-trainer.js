// Chess Trainer - Main Orchestrator
document.addEventListener('DOMContentLoaded', () => {
    // CDN library check
    if (typeof Chess === 'undefined' || typeof Chessboard === 'undefined') {
        const banner = document.createElement('div');
        banner.style.cssText = 'background:#d32f2f;color:#fff;padding:12px 16px;text-align:center;font-weight:bold;position:fixed;top:0;left:0;right:0;z-index:9999;';
        banner.textContent = 'Chess libraries failed to load. Please check your internet connection and refresh.';
        document.body.prepend(banner);
    }

    // Back button
    document.getElementById('backBtn').addEventListener('click', () => {
        window.location.href = '/tools.html';
    });

    // Tab switching
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    function initTab(tab) {
        try {
            if (tab === 'play' && typeof initPlayMode === 'function') initPlayMode();
            if (tab === 'puzzles' && typeof initPuzzleMode === 'function') initPuzzleMode();
            if (tab === 'lessons' && typeof initLessonMode === 'function') initLessonMode();
            if (tab === 'analysis' && typeof initAnalysisMode === 'function') initAnalysisMode();
        } catch (e) {
            console.error(`Failed to initialize ${tab} mode:`, e);
            const tabEl = document.getElementById(`tab-${tab}`);
            if (tabEl) tabEl.innerHTML = '<p style="color:red;padding:16px;">Failed to load this mode. Please refresh and try again.</p>';
        }
    }

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;

            tabBtns.forEach(b => b.classList.remove('active'));
            tabContents.forEach(c => c.classList.remove('active'));

            btn.classList.add('active');
            document.getElementById(`tab-${tab}`).classList.add('active');

            initTab(tab);
        });
    });

    // Initialize default mode (Play)
    initTab('play');
});

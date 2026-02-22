// Menopause Wellness - Main JavaScript

let appData = { profile: null, checkIns: [], symptoms: [], hotFlashes: [], sleepLogs: [] };
let deleteTarget = { type: null, id: null };
let trendChart = null, symptomChart = null, hotFlashChart = null, sleepChart = null;

// Auth check
async function checkAuth() {
    try {
        const response = await fetch('/api/menopause', { credentials: 'include' });
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return null;
            }
            throw new Error('Failed to load data');
        }
        return await response.json();
    } catch (error) {
        console.error('Auth check failed:', error);
        window.location.href = '/login.html';
        return null;
    }
}

// Load all data
async function loadData() {
    try {
        const response = await fetch('/api/menopause', { credentials: 'include' });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to load data');
        }
        appData = await response.json();
        appData.checkIns = appData.checkIns || [];
        appData.symptoms = appData.symptoms || [];
        appData.hotFlashes = appData.hotFlashes || [];
        appData.sleepLogs = appData.sleepLogs || [];
        renderAll();
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

function renderAll() {
    renderDashboard();
    renderSymptomTab();
    renderHotFlashSleepTab();
}

// ===== HELPERS =====

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatShortDate(dateStr) {
    const d = new Date(dateStr + 'T00:00:00');
    return (d.getMonth() + 1) + '/' + d.getDate();
}

function todayStr() {
    return new Date().toISOString().substring(0, 10);
}

function yesterdayStr() {
    const d = new Date();
    d.setDate(d.getDate() - 1);
    return d.toISOString().substring(0, 10);
}

function nowTimeStr() {
    return new Date().toTimeString().substring(0, 5);
}

function getLast30Days() {
    const days = [];
    for (let i = 29; i >= 0; i--) {
        const d = new Date(); d.setDate(d.getDate() - i);
        days.push(d.toISOString().substring(0, 10));
    }
    return days;
}

function showToast(message) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.style.display = 'block';
    setTimeout(() => { toast.style.display = 'none'; }, 3000);
}

function getSelectedValue(groupEl) {
    const sel = groupEl.querySelector('.selected');
    return sel ? parseInt(sel.dataset.value) : 0;
}

// ===== TAB SWITCHING =====

function setupTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('tab-' + btn.dataset.tab).classList.add('active');
        });
    });
}

// ===== EMOJI / SEVERITY BUTTON SETUP =====

function setupButtonGroups() {
    // Emoji buttons
    document.querySelectorAll('.emoji-group').forEach(group => {
        group.querySelectorAll('.emoji-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                group.querySelectorAll('.emoji-btn').forEach(b => b.classList.remove('selected'));
                btn.classList.add('selected');
            });
        });
    });

    // Severity buttons
    document.querySelectorAll('.severity-group').forEach(group => {
        group.querySelectorAll('.sev-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                group.querySelectorAll('.sev-btn').forEach(b => b.classList.remove('selected'));
                btn.classList.add('selected');
            });
        });
    });
}

// ===== DASHBOARD =====

function renderDashboard() {
    renderWeeklyScore();
    renderTips();
    renderTrendChart();
    renderRecentCheckins();
}

function renderWeeklyScore() {
    const now = new Date();
    const d7 = new Date(now); d7.setDate(d7.getDate() - 7);
    const d14 = new Date(now); d14.setDate(d14.getDate() - 14);

    const thisWeek = appData.checkIns.filter(c => new Date(c.date) >= d7);
    const lastWeek = appData.checkIns.filter(c => { const d = new Date(c.date); return d >= d14 && d < d7; });

    const avg = (arr) => {
        if (!arr.length) return 0;
        return arr.reduce((s, c) => s + ((c.mood + c.energy + c.sleepQuality + c.overall) / 4), 0) / arr.length;
    };

    const thisAvg = avg(thisWeek);
    const lastAvg = avg(lastWeek);
    const scoreEl = document.getElementById('weeklyScore');
    const trendEl = document.getElementById('scoreTrend');

    scoreEl.textContent = thisWeek.length ? thisAvg.toFixed(1) : '--';

    if (thisWeek.length && lastWeek.length) {
        const diff = thisAvg - lastAvg;
        if (diff > 0.1) { trendEl.textContent = '↑'; trendEl.className = 'score-trend up'; }
        else if (diff < -0.1) { trendEl.textContent = '↓'; trendEl.className = 'score-trend down'; }
        else { trendEl.textContent = '→'; trendEl.className = 'score-trend same'; }
    } else {
        trendEl.textContent = '';
    }
}

function renderTips() {
    const d7 = new Date(); d7.setDate(d7.getDate() - 7);
    const recent = appData.checkIns.filter(c => new Date(c.date) >= d7);
    const list = document.getElementById('tipsList');

    const sleepTips = ['Keep bedroom 60-67°F for better sleep', 'Avoid caffeine after 2pm', 'Stick to a consistent sleep schedule'];
    const moodTips = ['30 min daily walk improves mood', '5 min deep breathing helps anxiety', 'Talk to your doctor about mood changes'];
    const energyTips = ['Stay hydrated — aim for 8 glasses a day', 'Short 10-min walks boost energy', 'Iron-rich foods help fight fatigue'];
    const defaultTips = ['Tracking symptoms helps identify patterns', 'Share your data with your doctor for better care'];

    if (!recent.length) {
        list.innerHTML = defaultTips.map(t => `<li>${escapeHtml(t)}</li>`).join('');
        return;
    }

    const avgMood = recent.reduce((s, c) => s + c.mood, 0) / recent.length;
    const avgEnergy = recent.reduce((s, c) => s + c.energy, 0) / recent.length;
    const avgSleep = recent.reduce((s, c) => s + c.sleepQuality, 0) / recent.length;

    let tips = [];
    const lowest = Math.min(avgMood, avgEnergy, avgSleep);
    if (lowest === avgSleep || avgSleep < 3) tips.push(...sleepTips.slice(0, 2));
    if (lowest === avgMood || avgMood < 3) tips.push(...moodTips.slice(0, 2));
    if (lowest === avgEnergy || avgEnergy < 3) tips.push(...energyTips.slice(0, 2));
    if (!tips.length) tips = defaultTips;
    tips = tips.slice(0, 3);

    list.innerHTML = tips.map(t => `<li>${escapeHtml(t)}</li>`).join('');
}

function renderTrendChart() {
    const days = getLast30Days();
    const ciMap = {};
    appData.checkIns.forEach(c => { ciMap[c.date.substring(0, 10)] = c; });

    const labels = days.map(d => formatShortDate(d));
    const moodData = days.map(d => ciMap[d] ? ciMap[d].mood : null);
    const energyData = days.map(d => ciMap[d] ? ciMap[d].energy : null);
    const sleepData = days.map(d => ciMap[d] ? ciMap[d].sleepQuality : null);
    const overallData = days.map(d => ciMap[d] ? ciMap[d].overall : null);

    if (trendChart) trendChart.destroy();
    trendChart = new Chart(document.getElementById('trendChart'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                { label: 'Mood', data: moodData, borderColor: '#6366f1', backgroundColor: 'rgba(99,102,241,0.1)', tension: 0.3, spanGaps: true, fill: false },
                { label: 'Energy', data: energyData, borderColor: '#f59e0b', backgroundColor: 'rgba(245,158,11,0.1)', tension: 0.3, spanGaps: true, fill: false },
                { label: 'Sleep', data: sleepData, borderColor: '#10b981', backgroundColor: 'rgba(16,185,129,0.1)', tension: 0.3, spanGaps: true, fill: false },
                { label: 'Overall', data: overallData, borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,0.1)', tension: 0.3, spanGaps: true, fill: false }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: { y: { min: 0, max: 5, ticks: { stepSize: 1 } } },
            plugins: { legend: { position: 'bottom' } }
        }
    });
}

function renderRecentCheckins() {
    const el = document.getElementById('recentCheckins');
    const sorted = [...appData.checkIns].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 7);

    if (!sorted.length) {
        el.innerHTML = '<div class="entry-empty">No check-ins yet. Start by logging how you feel today!</div>';
        return;
    }

    el.innerHTML = sorted.map(c => `
        <div class="entry-item">
            <div class="entry-info">
                <div class="entry-date">${formatDate(c.date)}</div>
                <div class="entry-detail">
                    Mood: ${c.mood}/5 · Energy: ${c.energy}/5 · Sleep: ${c.sleepQuality}/5 · Overall: ${c.overall}/5
                    ${c.notes ? ' · ' + escapeHtml(c.notes) : ''}
                </div>
            </div>
            <div class="entry-actions">
                <button class="btn-small btn-edit" onclick="editCheckin('${c.id}')">✏️</button>
                <button class="btn-small btn-del" onclick="openDeleteModal('checkin','${c.id}')">🗑️</button>
            </div>
        </div>
    `).join('');
}

// Save check-in
async function saveCheckin() {
    const mood = getSelectedValue(document.querySelector('[data-field="mood"]'));
    const energy = getSelectedValue(document.querySelector('[data-field="energy"]'));
    const sleepQuality = getSelectedValue(document.querySelector('[data-field="sleepQuality"]'));
    const overall = getSelectedValue(document.querySelector('[data-field="overall"]'));
    const notes = document.getElementById('checkinNotes').value.trim();
    const editId = document.getElementById('checkinEditId').value;

    if (!mood || !energy || !sleepQuality || !overall) {
        showToast('Please rate all categories');
        return;
    }

    const data = { date: todayStr(), mood, energy, sleepQuality, overall, notes };
    const btn = document.getElementById('saveCheckinBtn');
    btn.textContent = 'Saving...';
    btn.disabled = true;

    try {
        const url = editId ? `/api/menopause/checkins/${editId}` : '/api/menopause/checkins';
        const method = editId ? 'PUT' : 'POST';
        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to save');
        }
        resetCheckinForm();
        await loadData();
        showToast('Check-in saved!');
    } catch (error) {
        console.error('Error saving check-in:', error);
        alert('Failed to save check-in. Please try again.');
    } finally {
        btn.textContent = 'Save Check-in';
        btn.disabled = false;
    }
}

function resetCheckinForm() {
    document.querySelectorAll('.emoji-group .emoji-btn').forEach(b => b.classList.remove('selected'));
    document.getElementById('checkinNotes').value = '';
    document.getElementById('checkinEditId').value = '';
}

function editCheckin(id) {
    const c = appData.checkIns.find(x => x.id === id);
    if (!c) return;

    document.getElementById('checkinEditId').value = c.id;
    document.getElementById('checkinNotes').value = c.notes || '';

    // Set emoji selections
    const fields = { mood: c.mood, energy: c.energy, sleepQuality: c.sleepQuality, overall: c.overall };
    for (const [field, val] of Object.entries(fields)) {
        const group = document.querySelector(`[data-field="${field}"]`);
        group.querySelectorAll('.emoji-btn').forEach(b => {
            b.classList.toggle('selected', parseInt(b.dataset.value) === val);
        });
    }

    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
    showToast('Editing check-in — update and save');
}

// ===== SYMPTOM JOURNAL =====

function renderSymptomTab() {
    renderSymptomTimeline();
    renderSymptomChart();
}

function renderSymptomTimeline() {
    const el = document.getElementById('symptomTimeline');
    const filter = document.getElementById('symptomFilter').value;
    let sorted = [...appData.symptoms].sort((a, b) => b.date.localeCompare(a.date));
    if (filter) sorted = sorted.filter(s => s.symptomType === filter);

    if (!sorted.length) {
        el.innerHTML = '<div class="entry-empty">No symptoms logged yet.</div>';
        return;
    }

    el.innerHTML = sorted.map(s => `
        <div class="entry-item">
            <div class="entry-info">
                <div class="entry-date">${formatDate(s.date)}${s.time ? ' · ' + s.time : ''}</div>
                <div class="entry-detail">
                    <strong>${escapeHtml(s.symptomType)}</strong>
                    <span class="sev-badge sev-${s.severity}">${s.severity}/5</span>
                    ${s.trigger ? ' · Trigger: ' + escapeHtml(s.trigger) : ''}
                    ${s.notes ? ' · ' + escapeHtml(s.notes) : ''}
                </div>
            </div>
            <div class="entry-actions">
                <button class="btn-small btn-edit" onclick="editSymptom('${s.id}')">✏️</button>
                <button class="btn-small btn-del" onclick="openDeleteModal('symptom','${s.id}')">🗑️</button>
            </div>
        </div>
    `).join('');
}

function renderSymptomChart() {
    const d30 = new Date(); d30.setDate(d30.getDate() - 30);
    const recent = appData.symptoms.filter(s => new Date(s.date) >= d30);
    const counts = {};
    recent.forEach(s => { counts[s.symptomType] = (counts[s.symptomType] || 0) + 1; });

    const labels = Object.keys(counts);
    const data = labels.map(l => counts[l]);

    if (symptomChart) symptomChart.destroy();
    symptomChart = new Chart(document.getElementById('symptomChart'), {
        type: 'bar',
        data: {
            labels,
            datasets: [{ label: 'Count', data, backgroundColor: '#8b5cf6' }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'y',
            plugins: { legend: { display: false } },
            scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
}

async function saveSymptom(e) {
    e.preventDefault();

    const symptomType = document.getElementById('symptomType').value;
    const severity = getSelectedValue(document.getElementById('symptomSeverity'));
    const date = document.getElementById('symptomDate').value;
    const time = document.getElementById('symptomTime').value || null;
    const trigger = document.getElementById('symptomTrigger').value.trim() || null;
    const notes = document.getElementById('symptomNotes').value.trim() || null;
    const editId = document.getElementById('symptomEditId').value;

    if (!symptomType || !severity || !date) {
        showToast('Please fill in symptom, severity, and date');
        return;
    }

    const data = { symptomType, severity, date, time, trigger, notes };
    const btn = e.target.querySelector('button[type="submit"]');
    btn.textContent = 'Saving...';
    btn.disabled = true;

    try {
        const url = editId ? `/api/menopause/symptoms/${editId}` : '/api/menopause/symptoms';
        const method = editId ? 'PUT' : 'POST';
        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to save');
        }
        resetSymptomForm();
        await loadData();
        showToast('Symptom logged!');
    } catch (error) {
        console.error('Error saving symptom:', error);
        alert('Failed to save symptom. Please try again.');
    } finally {
        btn.textContent = 'Log Symptom';
        btn.disabled = false;
    }
}

function resetSymptomForm() {
    document.getElementById('symptomType').value = '';
    document.getElementById('symptomSeverity').querySelectorAll('.sev-btn').forEach(b => b.classList.remove('selected'));
    document.getElementById('symptomDate').value = todayStr();
    document.getElementById('symptomTime').value = '';
    document.getElementById('symptomTrigger').value = '';
    document.getElementById('symptomNotes').value = '';
    document.getElementById('symptomEditId').value = '';
}

function editSymptom(id) {
    const s = appData.symptoms.find(x => x.id === id);
    if (!s) return;

    document.getElementById('symptomEditId').value = s.id;
    document.getElementById('symptomType').value = s.symptomType;
    document.getElementById('symptomDate').value = s.date.substring(0, 10);
    document.getElementById('symptomTime').value = s.time || '';
    document.getElementById('symptomTrigger').value = s.trigger || '';
    document.getElementById('symptomNotes').value = s.notes || '';

    document.getElementById('symptomSeverity').querySelectorAll('.sev-btn').forEach(b => {
        b.classList.toggle('selected', parseInt(b.dataset.value) === s.severity);
    });

    // Switch to symptom tab and scroll up
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    document.querySelector('[data-tab="symptoms"]').classList.add('active');
    document.getElementById('tab-symptoms').classList.add('active');
    window.scrollTo({ top: 0, behavior: 'smooth' });
    showToast('Editing symptom — update and save');
}

// ===== HOT FLASH & SLEEP =====

function renderHotFlashSleepTab() {
    renderTodayHotFlashes();
    renderHotFlashChart();
    renderSleepChart();
    renderRecentSleepLogs();
}

function renderTodayHotFlashes() {
    const el = document.getElementById('todayHotFlashes');
    const today = todayStr();
    const todayFlashes = appData.hotFlashes
        .filter(h => h.date.substring(0, 10) === today)
        .sort((a, b) => (b.time || '').localeCompare(a.time || ''));

    if (!todayFlashes.length) {
        el.innerHTML = '<div class="entry-empty">No hot flashes logged today.</div>';
        return;
    }

    el.innerHTML = todayFlashes.map(h => `
        <div class="entry-item">
            <div class="entry-info">
                <div class="entry-date">${h.time || ''}</div>
                <div class="entry-detail">
                    <span class="sev-badge sev-${h.severity}">${h.severity}/5</span>
                    ${h.durationMinutes ? ' · ' + h.durationMinutes + ' min' : ''}
                    ${h.trigger ? ' · ' + escapeHtml(h.trigger) : ''}
                    ${h.notes ? ' · ' + escapeHtml(h.notes) : ''}
                </div>
            </div>
            <div class="entry-actions">
                <button class="btn-small btn-edit" onclick="editHotFlash('${h.id}')">✏️</button>
                <button class="btn-small btn-del" onclick="openDeleteModal('hotflash','${h.id}')">🗑️</button>
            </div>
        </div>
    `).join('');
}

function renderHotFlashChart() {
    const days = getLast30Days();
    const hfMap = {};
    appData.hotFlashes.forEach(h => {
        const d = h.date.substring(0, 10);
        hfMap[d] = (hfMap[d] || 0) + 1;
    });

    const labels = days.map(d => formatShortDate(d));
    const data = days.map(d => hfMap[d] || 0);

    if (hotFlashChart) hotFlashChart.destroy();
    hotFlashChart = new Chart(document.getElementById('hotFlashChart'), {
        type: 'bar',
        data: {
            labels,
            datasets: [{ label: 'Hot Flashes', data, backgroundColor: '#ef4444' }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
}

function renderSleepChart() {
    const days = getLast30Days();
    const slMap = {};
    appData.sleepLogs.forEach(s => { slMap[s.date.substring(0, 10)] = s; });

    const labels = days.map(d => formatShortDate(d));
    const qualityData = days.map(d => slMap[d] ? slMap[d].quality : null);
    const sweatsData = days.map(d => slMap[d] ? slMap[d].nightSweats : null);

    if (sleepChart) sleepChart.destroy();
    sleepChart = new Chart(document.getElementById('sleepChart'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                { label: 'Sleep Quality', data: qualityData, borderColor: '#6366f1', backgroundColor: 'rgba(99,102,241,0.1)', tension: 0.3, spanGaps: true, fill: false, yAxisID: 'y' },
                { label: 'Night Sweats', data: sweatsData, type: 'bar', backgroundColor: 'rgba(245,158,11,0.4)', borderColor: '#f59e0b', yAxisID: 'y1' }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom' } },
            scales: {
                y: { type: 'linear', position: 'left', min: 0, max: 5, title: { display: true, text: 'Quality' } },
                y1: { type: 'linear', position: 'right', min: 0, title: { display: true, text: 'Night Sweats' }, grid: { drawOnChartArea: false } }
            }
        }
    });
}

function renderRecentSleepLogs() {
    const el = document.getElementById('recentSleepLogs');
    const sorted = [...appData.sleepLogs].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 7);

    if (!sorted.length) {
        el.innerHTML = '<div class="entry-empty">No sleep logs yet.</div>';
        return;
    }

    el.innerHTML = sorted.map(s => `
        <div class="entry-item">
            <div class="entry-info">
                <div class="entry-date">${formatDate(s.date)}</div>
                <div class="entry-detail">
                    Quality: <span class="sev-badge sev-${s.quality}">${s.quality}/5</span>
                    ${s.bedTime ? ' · Bed: ' + s.bedTime : ''}
                    ${s.wakeTime ? ' · Wake: ' + s.wakeTime : ''}
                    · Night Sweats: ${s.nightSweats || 0}
                    · Interruptions: ${s.interruptions || 0}
                    ${s.notes ? ' · ' + escapeHtml(s.notes) : ''}
                </div>
            </div>
            <div class="entry-actions">
                <button class="btn-small btn-edit" onclick="editSleep('${s.id}')">✏️</button>
                <button class="btn-small btn-del" onclick="openDeleteModal('sleep','${s.id}')">🗑️</button>
            </div>
        </div>
    `).join('');
}

// Quick hot flash log
function quickHotFlash() {
    document.getElementById('hotFlashDate').value = todayStr();
    document.getElementById('hotFlashTime').value = nowTimeStr();
    // Pre-select severity 3
    document.getElementById('hotFlashSeverity').querySelectorAll('.sev-btn').forEach(b => {
        b.classList.toggle('selected', b.dataset.value === '3');
    });
    document.getElementById('hotFlashDuration').focus();
    showToast('Time auto-filled — set severity and save');
}

async function saveHotFlash(e) {
    e.preventDefault();

    const severity = getSelectedValue(document.getElementById('hotFlashSeverity'));
    const durationMinutes = parseInt(document.getElementById('hotFlashDuration').value) || null;
    const trigger = document.getElementById('hotFlashTrigger').value || null;
    const notes = document.getElementById('hotFlashNotes').value.trim() || null;
    const date = document.getElementById('hotFlashDate').value || todayStr();
    const time = document.getElementById('hotFlashTime').value || nowTimeStr();
    const editId = document.getElementById('hotFlashEditId').value;

    if (!severity) {
        showToast('Please select severity');
        return;
    }

    const data = { date, time, severity, durationMinutes, trigger, notes };
    const btn = e.target.querySelector('button[type="submit"]');
    btn.textContent = 'Saving...';
    btn.disabled = true;

    try {
        const url = editId ? `/api/menopause/hotflashes/${editId}` : '/api/menopause/hotflashes';
        const method = editId ? 'PUT' : 'POST';
        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to save');
        }
        resetHotFlashForm();
        await loadData();
        showToast('Hot flash logged!');
    } catch (error) {
        console.error('Error saving hot flash:', error);
        alert('Failed to save hot flash. Please try again.');
    } finally {
        btn.textContent = 'Save Hot Flash';
        btn.disabled = false;
    }
}

function resetHotFlashForm() {
    document.getElementById('hotFlashSeverity').querySelectorAll('.sev-btn').forEach(b => b.classList.remove('selected'));
    document.getElementById('hotFlashDuration').value = '';
    document.getElementById('hotFlashTrigger').value = '';
    document.getElementById('hotFlashNotes').value = '';
    document.getElementById('hotFlashDate').value = '';
    document.getElementById('hotFlashTime').value = '';
    document.getElementById('hotFlashEditId').value = '';
}

function editHotFlash(id) {
    const h = appData.hotFlashes.find(x => x.id === id);
    if (!h) return;

    document.getElementById('hotFlashEditId').value = h.id;
    document.getElementById('hotFlashDate').value = h.date.substring(0, 10);
    document.getElementById('hotFlashTime').value = h.time || '';
    document.getElementById('hotFlashDuration').value = h.durationMinutes || '';
    document.getElementById('hotFlashTrigger').value = h.trigger || '';
    document.getElementById('hotFlashNotes').value = h.notes || '';

    document.getElementById('hotFlashSeverity').querySelectorAll('.sev-btn').forEach(b => {
        b.classList.toggle('selected', parseInt(b.dataset.value) === h.severity);
    });

    window.scrollTo({ top: 0, behavior: 'smooth' });
    showToast('Editing hot flash — update and save');
}

async function saveSleep(e) {
    e.preventDefault();

    const date = document.getElementById('sleepDate').value;
    const bedTime = document.getElementById('sleepBedTime').value || null;
    const wakeTime = document.getElementById('sleepWakeTime').value || null;
    const quality = getSelectedValue(document.getElementById('sleepQualityGroup'));
    const nightSweats = parseInt(document.getElementById('sleepNightSweats').value) || 0;
    const interruptions = parseInt(document.getElementById('sleepInterruptions').value) || 0;
    const notes = document.getElementById('sleepNotes').value.trim() || null;
    const editId = document.getElementById('sleepEditId').value;

    if (!date || !quality) {
        showToast('Please fill in date and quality');
        return;
    }

    const data = { date, bedTime, wakeTime, quality, nightSweats, interruptions, notes };
    const btn = e.target.querySelector('button[type="submit"]');
    btn.textContent = 'Saving...';
    btn.disabled = true;

    try {
        const url = editId ? `/api/menopause/sleep/${editId}` : '/api/menopause/sleep';
        const method = editId ? 'PUT' : 'POST';
        const response = await fetch(url, {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to save');
        }
        resetSleepForm();
        await loadData();
        showToast('Sleep log saved!');
    } catch (error) {
        console.error('Error saving sleep log:', error);
        alert('Failed to save sleep log. Please try again.');
    } finally {
        btn.textContent = 'Save Sleep Log';
        btn.disabled = false;
    }
}

function resetSleepForm() {
    document.getElementById('sleepDate').value = yesterdayStr();
    document.getElementById('sleepBedTime').value = '';
    document.getElementById('sleepWakeTime').value = '';
    document.getElementById('sleepQualityGroup').querySelectorAll('.sev-btn').forEach(b => b.classList.remove('selected'));
    document.getElementById('sleepNightSweats').value = '0';
    document.getElementById('sleepInterruptions').value = '0';
    document.getElementById('sleepNotes').value = '';
    document.getElementById('sleepEditId').value = '';
}

function editSleep(id) {
    const s = appData.sleepLogs.find(x => x.id === id);
    if (!s) return;

    document.getElementById('sleepEditId').value = s.id;
    document.getElementById('sleepDate').value = s.date.substring(0, 10);
    document.getElementById('sleepBedTime').value = s.bedTime || '';
    document.getElementById('sleepWakeTime').value = s.wakeTime || '';
    document.getElementById('sleepNightSweats').value = s.nightSweats || 0;
    document.getElementById('sleepInterruptions').value = s.interruptions || 0;
    document.getElementById('sleepNotes').value = s.notes || '';

    document.getElementById('sleepQualityGroup').querySelectorAll('.sev-btn').forEach(b => {
        b.classList.toggle('selected', parseInt(b.dataset.value) === s.quality);
    });

    // Switch to hot flash & sleep tab
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    document.querySelector('[data-tab="hotflash-sleep"]').classList.add('active');
    document.getElementById('tab-hotflash-sleep').classList.add('active');
    window.scrollTo({ top: document.querySelector('.sleep-card').offsetTop - 20, behavior: 'smooth' });
    showToast('Editing sleep log — update and save');
}

// ===== DELETE =====

function openDeleteModal(type, id) {
    deleteTarget = { type, id };
    document.getElementById('deleteModal').style.display = 'flex';
}

function closeDeleteModal() {
    deleteTarget = { type: null, id: null };
    document.getElementById('deleteModal').style.display = 'none';
}

async function confirmDelete() {
    if (!deleteTarget.type || !deleteTarget.id) return;

    const endpoints = {
        checkin: 'checkins',
        symptom: 'symptoms',
        hotflash: 'hotflashes',
        sleep: 'sleep'
    };

    const btn = document.getElementById('confirmDeleteBtn');
    btn.textContent = 'Deleting...';
    btn.disabled = true;

    try {
        const response = await fetch(`/api/menopause/${endpoints[deleteTarget.type]}/${deleteTarget.id}`, {
            method: 'DELETE',
            credentials: 'include'
        });
        if (!response.ok) {
            if (response.status === 401) { window.location.href = '/login.html'; return; }
            throw new Error('Failed to delete');
        }
        closeDeleteModal();
        await loadData();
        showToast('Entry deleted');
    } catch (error) {
        console.error('Error deleting:', error);
        alert('Failed to delete. Please try again.');
    } finally {
        btn.textContent = 'Delete';
        btn.disabled = false;
    }
}

// ===== SHARE =====

async function shareData() {
    if (!appData.profile || !appData.profile.shareToken) {
        showToast('No share token available');
        return;
    }
    const url = `${window.location.origin}/shared-menopause.html?token=${appData.profile.shareToken}`;
    try {
        await navigator.clipboard.writeText(url);
        showToast('Share link copied to clipboard!');
    } catch {
        const input = document.createElement('input');
        input.value = url;
        document.body.appendChild(input);
        input.select();
        document.execCommand('copy');
        document.body.removeChild(input);
        showToast('Share link copied to clipboard!');
    }
}

// ===== INIT =====

document.addEventListener('DOMContentLoaded', async () => {
    setupTabs();
    setupButtonGroups();

    // Set default dates
    document.getElementById('symptomDate').value = todayStr();
    document.getElementById('sleepDate').value = yesterdayStr();

    // Event listeners
    document.getElementById('backBtn').addEventListener('click', () => { window.location.href = '/tools.html'; });
    document.getElementById('shareBtn').addEventListener('click', shareData);
    document.getElementById('saveCheckinBtn').addEventListener('click', saveCheckin);
    document.getElementById('symptomForm').addEventListener('submit', saveSymptom);
    document.getElementById('hotFlashForm').addEventListener('submit', saveHotFlash);
    document.getElementById('sleepForm').addEventListener('submit', saveSleep);
    document.getElementById('quickHotFlashBtn').addEventListener('click', quickHotFlash);
    document.getElementById('symptomFilter').addEventListener('change', renderSymptomTimeline);

    // Auth check and load data
    const data = await checkAuth();
    if (data) {
        appData = data;
        appData.checkIns = appData.checkIns || [];
        appData.symptoms = appData.symptoms || [];
        appData.hotFlashes = appData.hotFlashes || [];
        appData.sleepLogs = appData.sleepLogs || [];
        renderAll();
    }
});

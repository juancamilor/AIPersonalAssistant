// Back button handler
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

// Form submission handler
document.getElementById('exchangeForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const date = new Date().toISOString().split('T')[0];
    const fromCurrency = document.getElementById('fromCurrency').value;
    const amount = parseFloat(document.getElementById('amount').value) || 1;
    const toCurrencyCheckboxes = document.querySelectorAll('input[name="toCurrency"]:checked');
    const toCurrencies = Array.from(toCurrencyCheckboxes).map(cb => cb.value);
    
    // Hide previous results/errors
    document.getElementById('resultsSection').style.display = 'none';
    document.getElementById('errorSection').style.display = 'none';
    
    // Validation
    if (!fromCurrency) {
        showError('Please select a "From Currency"');
        return;
    }

    if (amount <= 0) {
        showError('Amount must be greater than 0');
        return;
    }
    
    if (toCurrencies.length === 0) {
        showError('Please select at least one "To Currency"');
        return;
    }
    
    if (toCurrencies.includes(fromCurrency)) {
        showError('Cannot convert a currency to itself. Please uncheck the "From Currency" in the "To Currency" selection.');
        return;
    }
    
    // Call API
    try {
        const response = await fetch('/api/rateexchange/convert', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                date: date,
                fromCurrency: fromCurrency,
                amount: amount,
                toCurrencies: toCurrencies
            })
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                window.location.href = '/login.html';
                return;
            }
            const error = await response.json();
            showError(error.error || 'Failed to get exchange rates');
            return;
        }
        
        const data = await response.json();
        displayResults(data);
        loadHistoryChart(fromCurrency, toCurrencies);
    } catch (error) {
        console.error('Error:', error);
        showError('An error occurred while fetching exchange rates. Please try again.');
    }
});

function displayResults(data) {
    const resultsContent = document.getElementById('resultsContent');
    const fromCurrencyName = getCurrencyName(data.fromCurrency);
    const fromCurrencyCode = getCurrencyCode(data.fromCurrency);
    const amount = data.amount || 1;
    
    let html = `
        <div class="results-header">
            <p><strong>From:</strong> ${fromCurrencyName}</p>
            <p><strong>Amount:</strong> ${formatNumber(amount)} ${fromCurrencyCode}</p>
        </div>
        <table class="results-table">
            <thead>
                <tr>
                    <th>To Currency</th>
                    <th>Average Rate</th>
                    <th>Converted Amount</th>
                    <th>Details</th>
                </tr>
            </thead>
            <tbody>
    `;
    
    data.conversions.forEach((conversion, index) => {
        const toCurrencyName = getCurrencyName(conversion.toCurrency);
        const toCurrencyCode = getCurrencyCode(conversion.toCurrency);
        const convertedAmount = conversion.averageRate * amount;
        
        html += `
            <tr>
                <td>${toCurrencyName}</td>
                <td><strong>${conversion.averageRate.toFixed(4)}</strong></td>
                <td><strong>${formatNumber(amount)} ${fromCurrencyCode} = ${formatNumber(convertedAmount)} ${toCurrencyCode}</strong></td>
                <td>
                    <button class="details-btn" onclick="toggleDetails(${index})">
                        <span id="details-arrow-${index}">▼</span> View Details
                    </button>
                </td>
            </tr>
            <tr id="details-${index}" class="details-row" style="display: none;">
                <td colspan="4">
                    <div class="details-content">
                        <h4>Source Breakdown:</h4>
                        <ul class="source-list">
    `;
        
        conversion.sources.forEach(source => {
            const statusIcon = source.success ? '✓' : '✗';
            const statusClass = source.success ? 'success' : 'failed';
            const rateText = source.success ? `${source.rate.toFixed(4)} ${toCurrencyCode}` : source.errorMessage || 'Failed';
            const convertedText = source.success ? `${formatNumber(amount)} ${fromCurrencyCode} = ${formatNumber(source.rate * amount)} ${toCurrencyCode}` : '';
            
            html += `
                <li class="source-item ${statusClass}">
                    <div>
                        <div class="source-header">
                            <span class="source-name">${source.source}</span>
                            <span class="source-status">${statusIcon}</span>
                        </div>
                        <div class="source-rate">${rateText}</div>
                        ${convertedText ? `<div class="source-converted">${convertedText}</div>` : ''}
                    </div>
                </li>
            `;
        });
        
        const totalConverted = conversion.averageRate * amount;
        
        html += `
                        </ul>
                        <p class="calculation-method">
                            <strong>${conversion.calculationMethod}</strong>
                        </p>
                        <p class="example">
                            <strong>Average: ${formatNumber(amount)} ${fromCurrencyCode} = ${formatNumber(totalConverted)} ${toCurrencyCode}</strong>
                        </p>
                    </div>
                </td>
            </tr>
        `;
    });
    
    html += `
            </tbody>
        </table>
    `;
    
    resultsContent.innerHTML = html;
    document.getElementById('resultsSection').style.display = 'block';
}

function toggleDetails(index) {
    const detailsRow = document.getElementById(`details-${index}`);
    const arrow = document.getElementById(`details-arrow-${index}`);
    
    if (detailsRow.style.display === 'none') {
        detailsRow.style.display = 'table-row';
        arrow.textContent = '▲';
    } else {
        detailsRow.style.display = 'none';
        arrow.textContent = '▼';
    }
}

function showError(message) {
    document.getElementById('errorMessage').textContent = message;
    document.getElementById('errorSection').style.display = 'block';
}

function getCurrencyName(code) {
    const names = {
        'US': 'United States (USD)',
        'CAD': 'Canada (CAD)',
        'MX': 'Mexico (MXN)',
        'CO': 'Colombia (COP)'
    };
    return names[code] || code;
}

function getCurrencyCode(code) {
    const codes = {
        'US': 'USD',
        'CAD': 'CAD',
        'MX': 'MXN',
        'CO': 'COP'
    };
    return codes[code] || code;
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
}

function formatNumber(num) {
    return new Intl.NumberFormat('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(num);
}

let historyChartInstance = null;

const chartColors = ['#4e79a7', '#f28e2b', '#e15759', '#76b7b2', '#59a14f'];

async function loadHistoryChart(fromCurrency, toCurrencies) {
    const chartSection = document.getElementById('historyChartSection');
    const ctx = document.getElementById('historyChart').getContext('2d');

    const datasets = [];
    let labels = [];

    try {
        const results = await Promise.all(
            toCurrencies.map(to =>
                fetch(`/api/rateexchange/history?from=${encodeURIComponent(fromCurrency)}&to=${encodeURIComponent(to)}`, { credentials: 'include' })
                    .then(r => r.ok ? r.json() : null)
            )
        );

        results.forEach((data, i) => {
            if (!data || !data.rates) return;
            const to = toCurrencies[i];
            const dates = data.rates.map(r => r.date);
            const rates = data.rates.map(r => r.rate);
            if (dates.length > labels.length) labels = dates;
            datasets.push({
                label: `${getCurrencyCode(fromCurrency)} → ${getCurrencyCode(to)}`,
                data: rates,
                borderColor: chartColors[i % chartColors.length],
                backgroundColor: chartColors[i % chartColors.length] + '33',
                fill: false,
                tension: 0.3
            });
        });

        if (datasets.length === 0) {
            chartSection.style.display = 'none';
            return;
        }

        if (historyChartInstance) {
            historyChartInstance.destroy();
        }

        historyChartInstance = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'top' } },
                scales: {
                    x: { title: { display: true, text: 'Date' }, ticks: { maxTicksLimit: 10 } },
                    y: { title: { display: true, text: 'Exchange Rate' }, beginAtZero: false }
                }
            }
        });

        chartSection.style.display = 'block';
    } catch (err) {
        console.error('Failed to load history chart:', err);
        chartSection.style.display = 'none';
    }
}

// Historical Time Series Chart
let timeSeriesChartInstance = null;

function initTimeSeriesDefaults() {
    const end = new Date();
    const start = new Date();
    start.setMonth(start.getMonth() - 3);
    document.getElementById('tsStartDate').value = start.toISOString().split('T')[0];
    document.getElementById('tsEndDate').value = end.toISOString().split('T')[0];
}

async function loadTimeSeries() {
    const checkboxes = document.querySelectorAll('#tsCheckboxGroup input[type="checkbox"]:checked');
    const currencies = Array.from(checkboxes).map(cb => cb.value);
    const startDate = document.getElementById('tsStartDate').value;
    const endDate = document.getElementById('tsEndDate').value;
    const btn = document.getElementById('tsLoadBtn');
    const container = document.getElementById('tsChartContainer');
    const noData = document.getElementById('tsNoData');
    const note = document.getElementById('tsNote');
    const colors = ['#6366f1', '#ec4899', '#f59e0b', '#10b981', '#ef4444', '#3b82f6'];

    if (!startDate || !endDate) {
        alert('Please select start and end dates.');
        return;
    }

    if (currencies.length === 0) {
        alert('Please select at least one currency.');
        return;
    }

    btn.textContent = 'Loading...';
    btn.disabled = true;
    container.style.display = 'none';
    noData.style.display = 'none';
    note.style.display = 'none';

    try {
        const results = await Promise.all(
            currencies.map(currency =>
                fetch(`/api/rateexchange/timeseries?from=USD&to=${currency}&startDate=${startDate}&endDate=${endDate}`, { credentials: 'include' })
                    .then(r => r.ok ? r.json() : null)
                    .catch(() => null)
            )
        );

        const datasets = [];
        let labels = [];

        results.forEach((data, i) => {
            if (!data || !data.series) return;
            for (const [currency, points] of Object.entries(data.series)) {
                if (points && points.length > 0) {
                    const dates = points.map(p => p.date);
                    if (dates.length > labels.length) labels = dates;
                    datasets.push({
                        label: `USD → ${currency}`,
                        data: points.map(p => p.rate),
                        borderColor: colors[i % colors.length],
                        backgroundColor: colors[i % colors.length] + '20',
                        fill: false,
                        tension: 0.1,
                        pointRadius: dates.length > 90 ? 0 : 2
                    });
                }
            }
        });

        if (datasets.length === 0) {
            noData.style.display = '';
            return;
        }

        if (timeSeriesChartInstance) {
            timeSeriesChartInstance.destroy();
            timeSeriesChartInstance = null;
        }

        container.style.display = '';
        const canvas = document.getElementById('timeSeriesChart');
        const ctx = canvas.getContext('2d');
        timeSeriesChartInstance = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: `Exchange Rates vs USD (${startDate} to ${endDate})`
                    },
                    legend: { display: true, position: 'top' }
                },
                scales: {
                    x: {
                        title: { display: true, text: 'Date' },
                        ticks: { maxTicksLimit: 12 }
                    },
                    y: {
                        title: { display: true, text: 'Rate' },
                        beginAtZero: false
                    }
                }
            }
        });
    } catch (error) {
        console.error('Time series error:', error);
        alert('Failed to load time series data: ' + error.message);
    } finally {
        btn.textContent = 'Show History';
        btn.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    initTimeSeriesDefaults();
    document.getElementById('tsLoadBtn').addEventListener('click', loadTimeSeries);
});

// Dynamically disable/enable checkboxes based on from currency selection
document.getElementById('fromCurrency').addEventListener('change', (e) => {
    const selectedFrom = e.target.value;
    const checkboxes = document.querySelectorAll('input[name="toCurrency"]');
    
    checkboxes.forEach(cb => {
        if (cb.value === selectedFrom) {
            cb.disabled = true;
            cb.checked = false;
        } else {
            cb.disabled = false;
            cb.checked = true;
        }
    });
});

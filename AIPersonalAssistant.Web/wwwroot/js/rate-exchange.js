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
    const from = document.getElementById('tsFromCurrency').value;
    const to = document.getElementById('tsToCurrency').value;
    const startDate = document.getElementById('tsStartDate').value;
    const endDate = document.getElementById('tsEndDate').value;
    const btn = document.getElementById('tsLoadBtn');
    const container = document.getElementById('tsChartContainer');
    const noData = document.getElementById('tsNoData');
    const note = document.getElementById('tsNote');

    if (!startDate || !endDate) {
        alert('Please select start and end dates.');
        return;
    }

    if (from === to) {
        alert('From and To currencies must be different.');
        return;
    }

    btn.textContent = 'Loading...';
    btn.disabled = true;
    container.style.display = 'none';
    noData.style.display = 'none';
    note.style.display = 'none';

    try {
        const response = await fetch(`/api/rateexchange/timeseries?from=${from}&to=${to}&startDate=${startDate}&endDate=${endDate}`, {
            credentials: 'include'
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.error || 'Failed to fetch time series');
        }

        const data = await response.json();

        const hasData = Object.values(data.series).some(points => points && points.length > 0);

        if (!hasData) {
            noData.style.display = '';
            if (to === 'CO' || from === 'CO') {
                note.textContent = '⚠️ Historical data for COP (Colombian Peso) has limited availability via free APIs.';
                note.style.display = '';
            }
            return;
        }

        const datasets = [];
        const colors = ['#007bff', '#28a745', '#dc3545', '#ffc107', '#17a2b8', '#6f42c1'];
        let colorIdx = 0;
        let labels = data.dates || [];

        for (const [currency, points] of Object.entries(data.series)) {
            if (points && points.length > 0) {
                if (labels.length === 0) {
                    labels = points.map(p => p.date);
                }
                datasets.push({
                    label: currency,
                    data: points.map(p => p.rate),
                    borderColor: colors[colorIdx % colors.length],
                    backgroundColor: colors[colorIdx % colors.length] + '20',
                    fill: false,
                    tension: 0.1,
                    pointRadius: labels.length > 90 ? 0 : 2
                });
                colorIdx++;
            }
        }

        if (to === 'CO' || from === 'CO') {
            const copSeries = data.series['COP'];
            if (!copSeries || copSeries.length === 0) {
                note.textContent = '⚠️ Historical data for COP (Colombian Peso) has limited availability via free APIs.';
                note.style.display = '';
            }
        }

        if (timeSeriesChartInstance) {
            timeSeriesChartInstance.destroy();
        }

        container.style.display = '';
        const ctx = document.getElementById('timeSeriesChart').getContext('2d');
        timeSeriesChartInstance = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: `Exchange Rate: ${from} → ${to} (${startDate} to ${endDate})`
                    },
                    legend: { display: datasets.length > 1 }
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
        noData.textContent = error.message;
        noData.style.display = '';
    } finally {
        btn.textContent = 'Show History';
        btn.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', initTimeSeriesDefaults);

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

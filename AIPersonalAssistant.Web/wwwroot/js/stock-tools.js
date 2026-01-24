let stockChart = null;

// Set default dates (last 30 days)
const setDefaultDates = () => {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);

    document.getElementById('endDate').value = endDate.toISOString().split('T')[0];
    document.getElementById('startDate').value = startDate.toISOString().split('T')[0];

    // Set max date to today
    document.getElementById('endDate').max = endDate.toISOString().split('T')[0];
    document.getElementById('startDate').max = endDate.toISOString().split('T')[0];
};

// Format currency
const formatCurrency = (value) => {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(value);
};

// Format percentage
const formatPercent = (value) => {
    const sign = value >= 0 ? '+' : '';
    return sign + value.toFixed(2) + '%';
};

// Show/hide sections
const showLoading = () => {
    document.getElementById('loadingSection').style.display = 'block';
    document.getElementById('resultsSection').style.display = 'none';
    document.getElementById('errorSection').style.display = 'none';
};

const showResults = () => {
    document.getElementById('loadingSection').style.display = 'none';
    document.getElementById('resultsSection').style.display = 'block';
    document.getElementById('errorSection').style.display = 'none';
};

const showError = (message) => {
    document.getElementById('loadingSection').style.display = 'none';
    document.getElementById('resultsSection').style.display = 'none';
    document.getElementById('errorSection').style.display = 'block';
    document.getElementById('errorMessage').textContent = message;
};

const hideAllResults = () => {
    document.getElementById('loadingSection').style.display = 'none';
    document.getElementById('resultsSection').style.display = 'none';
    document.getElementById('errorSection').style.display = 'none';
};

// Create/update chart
const renderChart = (data) => {
    const ctx = document.getElementById('stockChart').getContext('2d');

    const labels = data.dataPoints.map(p => new Date(p.date).toLocaleDateString());
    const prices = data.dataPoints.map(p => p.close);

    if (stockChart) {
        stockChart.destroy();
    }

    stockChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: `${data.symbol} Closing Price`,
                data: prices,
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                borderWidth: 2,
                fill: true,
                tension: 0.1,
                pointRadius: prices.length > 50 ? 0 : 3,
                pointHoverRadius: 5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => formatCurrency(context.parsed.y)
                    }
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    ticks: {
                        maxTicksLimit: 10
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Price (USD)'
                    },
                    ticks: {
                        callback: (value) => formatCurrency(value)
                    }
                }
            }
        }
    });
};

// Render summary statistics
const renderSummary = (data) => {
    const prices = data.dataPoints.map(p => p.close);
    
    if (prices.length === 0) {
        document.getElementById('stockSummary').innerHTML = '<p>No data available for the selected date range.</p>';
        return;
    }

    const startPrice = prices[0];
    const endPrice = prices[prices.length - 1];
    const highPrice = Math.max(...prices);
    const lowPrice = Math.min(...prices);
    const change = endPrice - startPrice;
    const changePercent = ((endPrice - startPrice) / startPrice) * 100;

    const changeClass = change >= 0 ? 'positive' : 'negative';

    document.getElementById('stockSummary').innerHTML = `
        <div class="summary-grid">
            <div class="summary-item">
                <div class="summary-label">Start Price</div>
                <div class="summary-value">${formatCurrency(startPrice)}</div>
            </div>
            <div class="summary-item">
                <div class="summary-label">End Price</div>
                <div class="summary-value">${formatCurrency(endPrice)}</div>
            </div>
            <div class="summary-item">
                <div class="summary-label">High</div>
                <div class="summary-value">${formatCurrency(highPrice)}</div>
            </div>
            <div class="summary-item">
                <div class="summary-label">Low</div>
                <div class="summary-value">${formatCurrency(lowPrice)}</div>
            </div>
            <div class="summary-item">
                <div class="summary-label">Change</div>
                <div class="summary-value ${changeClass}">${formatCurrency(change)}</div>
            </div>
            <div class="summary-item">
                <div class="summary-label">Change %</div>
                <div class="summary-value ${changeClass}">${formatPercent(changePercent)}</div>
            </div>
        </div>
    `;
};

// Fetch stock data
const fetchStockData = async (symbol, startDate, endDate) => {
    const response = await fetch('/api/stock/data', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        credentials: 'include',
        body: JSON.stringify({
            symbol: symbol,
            startDate: startDate,
            endDate: endDate
        })
    });

    if (!response.ok) {
        if (response.status === 401) {
            window.location.href = '/login.html';
            return null;
        }
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to fetch stock data');
    }

    return await response.json();
};

// Form submit handler
document.getElementById('stockForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const symbol = document.getElementById('stockSymbol').value;
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;

    if (!symbol || !startDate || !endDate) {
        showError('Please fill in all fields.');
        return;
    }

    if (new Date(startDate) > new Date(endDate)) {
        showError('Start date must be before end date.');
        return;
    }

    showLoading();

    try {
        const data = await fetchStockData(symbol, startDate, endDate);

        if (!data) return;

        if (!data.success) {
            showError(data.errorMessage || 'Failed to fetch stock data.');
            return;
        }

        if (data.dataPoints.length === 0) {
            showError('No data available for the selected date range. Try expanding the date range.');
            return;
        }

        document.getElementById('chartTitle').textContent = `${data.companyName} (${data.symbol}) Performance`;
        renderChart(data);
        renderSummary(data);
        showResults();
    } catch (error) {
        console.error('Error fetching stock data:', error);
        showError(error.message || 'Failed to fetch stock data. Please try again.');
    }
});

// Back button handler
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

// Initialize
setDefaultDates();

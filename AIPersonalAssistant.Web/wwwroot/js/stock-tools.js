let stockChart = null;
let selectedStocks = [];

// Stock colors for chart
const stockColors = [
    { border: '#667eea', background: 'rgba(102, 126, 234, 0.1)' },
    { border: '#f56565', background: 'rgba(245, 101, 101, 0.1)' },
    { border: '#48bb78', background: 'rgba(72, 187, 120, 0.1)' },
    { border: '#ed8936', background: 'rgba(237, 137, 54, 0.1)' },
    { border: '#9f7aea', background: 'rgba(159, 122, 234, 0.1)' },
    { border: '#38b2ac', background: 'rgba(56, 178, 172, 0.1)' },
    { border: '#e53e3e', background: 'rgba(229, 62, 62, 0.1)' },
    { border: '#3182ce', background: 'rgba(49, 130, 206, 0.1)' }
];

// Stock names mapping
const stockNames = {
    'MSFT': 'Microsoft Corporation',
    'META': 'Meta Platforms, Inc.',
    'GOOGL': 'Alphabet Inc.'
};

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

// Render selected stocks
const renderSelectedStocks = () => {
    const container = document.getElementById('selectedStocks');
    if (selectedStocks.length === 0) {
        container.innerHTML = '<p class="no-stocks">No stocks selected. Add stocks to compare.</p>';
        return;
    }
    
    container.innerHTML = selectedStocks.map((symbol, index) => `
        <div class="stock-tag" style="border-left: 4px solid ${stockColors[index % stockColors.length].border}">
            <span>${symbol} - ${stockNames[symbol] || symbol}</span>
            <button type="button" class="remove-stock" onclick="removeStock('${symbol}')">&times;</button>
        </div>
    `).join('');
};

// Add stock
const addStock = () => {
    const select = document.getElementById('stockSymbol');
    const symbol = select.value;
    
    if (!symbol) return;
    
    if (selectedStocks.includes(symbol)) {
        return; // Already added
    }
    
    selectedStocks.push(symbol);
    select.value = '';
    renderSelectedStocks();
};

// Remove stock
window.removeStock = (symbol) => {
    selectedStocks = selectedStocks.filter(s => s !== symbol);
    renderSelectedStocks();
};

// Create/update chart with multiple stocks
const renderChart = (stockDataArray) => {
    const ctx = document.getElementById('stockChart').getContext('2d');

    if (stockChart) {
        stockChart.destroy();
    }

    // Get all unique dates across all stocks
    const allDates = new Set();
    stockDataArray.forEach(data => {
        data.dataPoints.forEach(p => allDates.add(p.date.split('T')[0]));
    });
    const sortedDates = Array.from(allDates).sort();
    const labels = sortedDates.map(d => new Date(d).toLocaleDateString());

    // Create datasets for each stock
    const datasets = stockDataArray.map((data, index) => {
        const color = stockColors[index % stockColors.length];
        
        // Map data points to the common date axis
        const dateMap = {};
        data.dataPoints.forEach(p => {
            dateMap[p.date.split('T')[0]] = p.close;
        });
        
        const prices = sortedDates.map(d => dateMap[d] || null);
        
        return {
            label: `${data.symbol}`,
            data: prices,
            borderColor: color.border,
            backgroundColor: color.background,
            borderWidth: 2,
            fill: false,
            tension: 0.1,
            pointRadius: prices.length > 50 ? 0 : 3,
            pointHoverRadius: 5,
            spanGaps: true
        };
    });

    stockChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: (context) => `${context.dataset.label}: ${formatCurrency(context.parsed.y)}`
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

// Render summary statistics for multiple stocks
const renderSummary = (stockDataArray) => {
    if (stockDataArray.length === 0) {
        document.getElementById('stockSummary').innerHTML = '<p>No data available.</p>';
        return;
    }

    let summaryHtml = '<div class="multi-stock-summary">';
    
    stockDataArray.forEach((data, index) => {
        const prices = data.dataPoints.map(p => p.close);
        
        if (prices.length === 0) {
            summaryHtml += `<div class="stock-summary-card"><h3>${data.symbol}</h3><p>No data available</p></div>`;
            return;
        }

        const startPrice = prices[0];
        const endPrice = prices[prices.length - 1];
        const highPrice = Math.max(...prices);
        const lowPrice = Math.min(...prices);
        const change = endPrice - startPrice;
        const changePercent = ((endPrice - startPrice) / startPrice) * 100;
        const changeClass = change >= 0 ? 'positive' : 'negative';
        const color = stockColors[index % stockColors.length].border;

        summaryHtml += `
            <div class="stock-summary-card" style="border-top: 4px solid ${color}">
                <h3>${data.companyName} (${data.symbol})</h3>
                <div class="summary-grid">
                    <div class="summary-item">
                        <div class="summary-label">Start</div>
                        <div class="summary-value">${formatCurrency(startPrice)}</div>
                    </div>
                    <div class="summary-item">
                        <div class="summary-label">End</div>
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
                        <div class="summary-value ${changeClass}">${formatPercent(changePercent)}</div>
                    </div>
                </div>
            </div>
        `;
    });
    
    summaryHtml += '</div>';
    document.getElementById('stockSummary').innerHTML = summaryHtml;
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

    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;

    if (selectedStocks.length === 0) {
        showError('Please add at least one stock to analyze.');
        return;
    }

    if (!startDate || !endDate) {
        showError('Please select start and end dates.');
        return;
    }

    if (new Date(startDate) > new Date(endDate)) {
        showError('Start date must be before end date.');
        return;
    }

    showLoading();

    try {
        // Fetch data for all selected stocks in parallel
        const fetchPromises = selectedStocks.map(symbol => fetchStockData(symbol, startDate, endDate));
        const results = await Promise.all(fetchPromises);

        // Filter out null results and failed fetches
        const validResults = results.filter(r => r && r.success && r.dataPoints.length > 0);

        if (validResults.length === 0) {
            showError('No data available for the selected stocks and date range.');
            return;
        }

        const title = selectedStocks.length === 1 
            ? `${validResults[0].companyName} (${validResults[0].symbol}) Performance`
            : 'Stock Performance Comparison';
        document.getElementById('chartTitle').textContent = title;
        
        renderChart(validResults);
        renderSummary(validResults);
        showResults();
    } catch (error) {
        console.error('Error fetching stock data:', error);
        showError(error.message || 'Failed to fetch stock data. Please try again.');
    }
});

// Add stock button handler
document.getElementById('addStockBtn').addEventListener('click', addStock);

// Allow adding stock with Enter key
document.getElementById('stockSymbol').addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        e.preventDefault();
        addStock();
    }
});

// Back button handler
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

// Initialize
setDefaultDates();
selectedStocks.push('MSFT');
renderSelectedStocks();
document.getElementById('stockForm').dispatchEvent(new Event('submit'));

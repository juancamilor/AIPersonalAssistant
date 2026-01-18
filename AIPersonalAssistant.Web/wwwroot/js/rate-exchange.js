// Back button handler
document.getElementById('backBtn').addEventListener('click', () => {
    window.location.href = '/tools.html';
});

// Form submission handler
document.getElementById('exchangeForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const date = document.getElementById('date').value;
    const fromCurrency = document.getElementById('fromCurrency').value;
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
    } catch (error) {
        console.error('Error:', error);
        showError('An error occurred while fetching exchange rates. Please try again.');
    }
});

// Set today's date as default
const today = new Date().toISOString().split('T')[0];
document.getElementById('date').value = today;

function displayResults(data) {
    const resultsContent = document.getElementById('resultsContent');
    const fromCurrencyName = getCurrencyName(data.fromCurrency);
    
    let html = `
        <div class="results-header">
            <p><strong>Date:</strong> ${formatDate(data.date)}</p>
            <p><strong>From:</strong> ${fromCurrencyName}</p>
        </div>
        <table class="results-table">
            <thead>
                <tr>
                    <th>To Currency</th>
                    <th>Exchange Rate</th>
                    <th>Example (1 ${getCurrencyCode(data.fromCurrency)})</th>
                </tr>
            </thead>
            <tbody>
    `;
    
    data.conversions.forEach(conversion => {
        const toCurrencyName = getCurrencyName(conversion.toCurrency);
        const toCurrencyCode = getCurrencyCode(conversion.toCurrency);
        html += `
            <tr>
                <td>${toCurrencyName}</td>
                <td>${conversion.rate.toFixed(4)}</td>
                <td>1 ${getCurrencyCode(data.fromCurrency)} = ${conversion.rate.toFixed(4)} ${toCurrencyCode}</td>
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
        }
    });
});

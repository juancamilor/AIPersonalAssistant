(function () {
    'use strict';

    let w2Data = null;
    let stockSalesFromExcel = [];

    // --- Back Button ---
    document.getElementById('backBtn').addEventListener('click', () => {
        window.location.href = '/tools.html';
    });

    // --- Tab Switching ---
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            btn.classList.add('active');
            document.getElementById('tab-' + btn.dataset.tab).classList.add('active');
        });
    });

    // --- W2 Upload ---
    const w2Area = document.getElementById('w2UploadArea');
    const w2Input = document.getElementById('w2FileInput');

    w2Area.addEventListener('click', () => w2Input.click());
    w2Area.addEventListener('dragenter', e => { e.preventDefault(); w2Area.classList.add('drag-over'); });
    w2Area.addEventListener('dragover', e => { e.preventDefault(); w2Area.classList.add('drag-over'); });
    w2Area.addEventListener('dragleave', () => w2Area.classList.remove('drag-over'));
    w2Area.addEventListener('drop', e => {
        e.preventDefault();
        w2Area.classList.remove('drag-over');
        if (e.dataTransfer.files.length) uploadW2(e.dataTransfer.files[0]);
    });
    w2Input.addEventListener('change', () => {
        if (w2Input.files.length) uploadW2(w2Input.files[0]);
    });

    async function uploadW2(file) {
        const allowed = ['.jpg', '.jpeg', '.png', '.pdf'];
        const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
        if (!allowed.includes(ext)) {
            const status = document.getElementById('w2Status');
            status.style.display = 'block';
            status.className = 'upload-status error';
            status.textContent = '❌ Unsupported file type. Please upload JPG, PNG, or PDF.';
            return;
        }

        const status = document.getElementById('w2Status');
        status.style.display = 'block';
        status.className = 'upload-status loading';
        status.textContent = 'Analyzing W2... This may take a moment.';

        const formData = new FormData();
        formData.append('file', file);

        try {
            const res = await fetch('/api/taxes/analyze-w2', { method: 'POST', body: formData });
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.error || 'Failed to analyze W2');
            }
            w2Data = await res.json();
            status.className = 'upload-status success';
            status.textContent = '✅ W2 loaded — ' + (w2Data.employerName || 'Employer') +
                ' | Wages: $' + fmt(w2Data.wagesTipsOtherCompensation);
            enableCalculate();
        } catch (err) {
            status.className = 'upload-status error';
            status.textContent = '❌ ' + err.message;
        }
    }

    // --- Excel Upload ---
    const excelArea = document.getElementById('excelUploadArea');
    const excelInput = document.getElementById('excelFileInput');

    excelArea.addEventListener('click', () => excelInput.click());
    excelArea.addEventListener('dragenter', e => { e.preventDefault(); excelArea.classList.add('drag-over'); });
    excelArea.addEventListener('dragover', e => { e.preventDefault(); excelArea.classList.add('drag-over'); });
    excelArea.addEventListener('dragleave', () => excelArea.classList.remove('drag-over'));
    excelArea.addEventListener('drop', e => {
        e.preventDefault();
        excelArea.classList.remove('drag-over');
        if (e.dataTransfer.files.length) uploadExcel(e.dataTransfer.files[0]);
    });
    excelInput.addEventListener('change', () => {
        if (excelInput.files.length) uploadExcel(excelInput.files[0]);
    });

    async function uploadExcel(file) {
        const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
        if (ext !== '.xlsx') {
            const status = document.getElementById('excelStatus');
            status.style.display = 'block';
            status.className = 'upload-status error';
            status.textContent = '❌ Unsupported file type. Please upload an .xlsx Excel file.';
            return;
        }

        const status = document.getElementById('excelStatus');
        status.style.display = 'block';
        status.className = 'upload-status loading';
        status.textContent = 'Parsing Excel file...';

        const formData = new FormData();
        formData.append('file', file);

        try {
            const res = await fetch('/api/taxes/parse-stock-sales', { method: 'POST', body: formData });
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.error || 'Failed to parse Excel');
            }
            stockSalesFromExcel = await res.json();
            status.className = 'upload-status success';
            status.textContent = '✅ Loaded ' + stockSalesFromExcel.length + ' stock sale(s) from Excel.';
            renderStockPreview(stockSalesFromExcel);
        } catch (err) {
            status.className = 'upload-status error';
            status.textContent = '❌ ' + err.message;
        }
    }

    function renderStockPreview(sales) {
        const container = document.getElementById('stockSalesPreview');
        const content = document.getElementById('stockSalesPreviewContent');
        if (!sales || sales.length === 0) {
            container.style.display = 'none';
            return;
        }
        container.style.display = 'block';
        let html = '<table class="preview-table"><thead><tr>' +
            '<th>Symbol</th><th>Buy Date</th><th>Sell Date</th><th>Qty</th><th>Proceeds</th><th>Cost Basis</th><th>Gain/Loss</th><th>Type</th>' +
            '</tr></thead><tbody>';
        sales.forEach(s => {
            const gl = (s.sellPrice * s.quantity) - (s.buyPrice * s.quantity);
            html += '<tr>' +
                '<td>' + esc(s.symbol) + '</td>' +
                '<td>' + fmtDate(s.buyDate) + '</td>' +
                '<td>' + fmtDate(s.sellDate) + '</td>' +
                '<td>' + s.quantity + '</td>' +
                '<td>$' + fmt(s.sellPrice * s.quantity) + '</td>' +
                '<td>$' + fmt(s.buyPrice * s.quantity) + '</td>' +
                '<td class="' + (gl >= 0 ? 'gain-positive' : 'gain-negative') + '">$' + fmt(gl) + '</td>' +
                '<td>' + (s.holdingType === 'LongTerm' ? 'Long' : 'Short') + '</td>' +
                '</tr>';
        });
        html += '</tbody></table>';
        content.innerHTML = html;
    }

    // --- Manual Entry ---
    document.getElementById('addRowBtn').addEventListener('click', addManualRow);
    addManualRow(); // start with one row

    function addManualRow() {
        const tbody = document.getElementById('stockSalesBody');
        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td><input type="text" placeholder="MSFT" class="m-symbol"></td>' +
            '<td><input type="date" class="m-buydate"></td>' +
            '<td><input type="number" step="0.01" placeholder="0.00" class="m-buyprice"></td>' +
            '<td><input type="date" class="m-selldate"></td>' +
            '<td><input type="number" step="0.01" placeholder="0.00" class="m-sellprice"></td>' +
            '<td><input type="number" min="1" placeholder="1" class="m-qty"></td>' +
            '<td><select class="m-type"><option value="ShortTerm">Short-Term</option><option value="LongTerm">Long-Term</option></select></td>' +
            '<td><button type="button" class="remove-row-btn" title="Remove">×</button></td>';
        tr.querySelector('.remove-row-btn').addEventListener('click', () => tr.remove());
        tbody.appendChild(tr);
    }

    function getManualSales() {
        const rows = document.querySelectorAll('#stockSalesBody tr');
        const sales = [];
        rows.forEach(row => {
            const symbol = row.querySelector('.m-symbol').value.trim();
            const buyDate = row.querySelector('.m-buydate').value;
            const buyPrice = parseFloat(row.querySelector('.m-buyprice').value);
            const sellDate = row.querySelector('.m-selldate').value;
            const sellPrice = parseFloat(row.querySelector('.m-sellprice').value);
            const quantity = parseInt(row.querySelector('.m-qty').value, 10);
            const holdingType = row.querySelector('.m-type').value;
            if (symbol && buyDate && !isNaN(buyPrice) && sellDate && !isNaN(sellPrice) && quantity > 0) {
                sales.push({ symbol, buyDate, buyPrice, sellDate, sellPrice, quantity, holdingType });
            }
        });
        return sales;
    }

    // --- Calculate ---
    function enableCalculate() {
        const btn = document.getElementById('calculateBtn');
        btn.disabled = false;
        document.getElementById('calculateHint').textContent = 'Ready to calculate your tax estimate.';
    }

    document.getElementById('calculateBtn').addEventListener('click', async () => {
        if (!w2Data) return;

        // Determine which stock sales source to use
        const activeTab = document.querySelector('.tab-btn.active').dataset.tab;
        let stockSales = [];
        if (activeTab === 'upload') {
            stockSales = stockSalesFromExcel;
        } else {
            stockSales = getManualSales();
        }

        document.getElementById('resultsSection').style.display = 'none';
        document.getElementById('errorSection').style.display = 'none';
        document.getElementById('loadingSection').style.display = 'block';

        try {
            const res = await fetch('/api/taxes/estimate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ w2Data, stockSales })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.error || 'Failed to calculate estimate');
            }
            const result = await res.json();
            renderResults(result);
        } catch (err) {
            showError(err.message);
        } finally {
            document.getElementById('loadingSection').style.display = 'none';
        }
    });

    // --- Render Results ---
    function renderResults(result) {
        // W2 Summary
        const w = result.w2Data || w2Data;
        document.getElementById('w2SummaryContent').innerHTML =
            '<div class="summary-grid">' +
            summaryRow('Employer', w.employerName || '—') +
            summaryRow('Employee', w.employeeName || '—') +
            summaryRow('Wages (Box 1)', '$' + fmt(w.wagesTipsOtherCompensation)) +
            summaryRow('Federal Tax Withheld (Box 2)', '$' + fmt(w.federalIncomeTaxWithheld)) +
            summaryRow('SS Wages (Box 3)', '$' + fmt(w.socialSecurityWages)) +
            summaryRow('SS Tax (Box 4)', '$' + fmt(w.socialSecurityTaxWithheld)) +
            summaryRow('Medicare Wages (Box 5)', '$' + fmt(w.medicareWagesAndTips)) +
            summaryRow('Medicare Tax (Box 6)', '$' + fmt(w.medicareTaxWithheld)) +
            summaryRow('State', w.state || '—') +
            summaryRow('State Tax Withheld (Box 17)', '$' + fmt(w.stateIncomeTaxWithheld)) +
            '</div>';

        // Stock Sales Summary
        const ss = result.stockSalesSummary;
        const stockCard = document.getElementById('stockSummaryCard');
        if (ss && ss.sales && ss.sales.length > 0) {
            stockCard.style.display = 'block';
            let html = '<div class="summary-grid">' +
                summaryRow('Short-Term Gain/Loss', '$' + fmt(ss.totalShortTermGain), ss.totalShortTermGain) +
                summaryRow('Long-Term Gain/Loss', '$' + fmt(ss.totalLongTermGain), ss.totalLongTermGain) +
                summaryRow('Net Capital Gain/Loss', '$' + fmt(ss.netCapitalGain), ss.netCapitalGain, true) +
                '</div>';
            html += '<table class="preview-table" style="margin-top:1rem;"><thead><tr>' +
                '<th>Symbol</th><th>Proceeds</th><th>Cost Basis</th><th>Gain/Loss</th><th>Type</th>' +
                '</tr></thead><tbody>';
            ss.sales.forEach(s => {
                const gl = s.gainLoss != null ? s.gainLoss : (s.proceeds - s.costBasis);
                html += '<tr><td>' + esc(s.symbol) + '</td>' +
                    '<td>$' + fmt(s.proceeds) + '</td>' +
                    '<td>$' + fmt(s.costBasis) + '</td>' +
                    '<td class="' + (gl >= 0 ? 'gain-positive' : 'gain-negative') + '">$' + fmt(gl) + '</td>' +
                    '<td>' + (s.holdingType === 'LongTerm' ? 'Long' : 'Short') + '</td></tr>';
            });
            html += '</tbody></table>';
            document.getElementById('stockSummaryContent').innerHTML = html;
        } else {
            stockCard.style.display = 'none';
        }

        // Tax Estimate
        const refundOrOwed = result.estimatedRefundOrOwed;
        const isRefund = refundOrOwed >= 0;
        document.getElementById('taxEstimateContent').innerHTML =
            '<div class="summary-grid">' +
            summaryRow('Filing Status', result.filingStatus) +
            summaryRow('Taxable Income', '$' + fmt(result.taxableIncome)) +
            summaryRow('Ordinary Income Tax', '$' + fmt(result.ordinaryIncomeTax)) +
            summaryRow('Capital Gains Tax', '$' + fmt(result.capitalGainsTax)) +
            summaryRow('Total Tax Owed', '$' + fmt(result.totalTaxOwed)) +
            summaryRow('Total Withheld', '$' + fmt(result.totalWithheld)) +
            '</div>' +
            '<div class="result-highlight">' +
            '<span class="label">' + (isRefund ? 'Estimated Refund' : 'Estimated Amount Owed') + '</span>' +
            '<span class="amount ' + (isRefund ? 'refund' : 'owed') + '">$' + fmt(Math.abs(refundOrOwed)) + '</span>' +
            '</div>';

        // Filing Cheat Sheet
        const cs = result.filingCheatSheet;
        const csCard = document.getElementById('cheatSheetCard');
        if (cs) {
            csCard.style.display = 'block';
            let html = '';

            // W2 entries
            if (cs.w2Entries && cs.w2Entries.length) {
                html += '<div class="cheat-sheet-section"><h3>W2 Information</h3>';
                cs.w2Entries.forEach(e => {
                    html += '<div class="cheat-entry"><span class="label">' + esc(e.label) + '</span><span class="value">' + esc(e.value) + '</span></div>';
                });
                html += '</div>';
            }

            // Schedule D
            if (cs.scheduleDEntries && cs.scheduleDEntries.length) {
                html += '<div class="cheat-sheet-section"><h3>Schedule D — Capital Gains</h3>';
                html += '<table class="preview-table"><thead><tr><th>Description</th><th>Acquired</th><th>Sold</th><th>Proceeds</th><th>Cost Basis</th><th>Gain/Loss</th></tr></thead><tbody>';
                cs.scheduleDEntries.forEach(e => {
                    html += '<tr><td>' + esc(e.description) + '</td><td>' + esc(e.dateAcquired) + '</td><td>' + esc(e.dateSold) + '</td>' +
                        '<td>$' + fmt(e.proceeds) + '</td><td>$' + fmt(e.costBasis) + '</td>' +
                        '<td class="' + (e.gainLoss >= 0 ? 'gain-positive' : 'gain-negative') + '">$' + fmt(e.gainLoss) + '</td></tr>';
                });
                html += '</tbody></table></div>';
            }

            // Summary
            html += '<div class="cheat-sheet-section"><h3>Summary</h3>' +
                '<div class="cheat-entry"><span class="label">Deduction Type</span><span class="value">' + esc(cs.deductionType) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Deduction Amount</span><span class="value">$' + fmt(cs.deductionAmount) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Total Income</span><span class="value">$' + fmt(cs.totalIncome) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">AGI</span><span class="value">$' + fmt(cs.adjustedGrossIncome) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Taxable Income</span><span class="value">$' + fmt(cs.taxableIncome) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Total Tax</span><span class="value">$' + fmt(cs.totalTax) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Total Withheld</span><span class="value">$' + fmt(cs.totalWithheld) + '</span></div>' +
                '<div class="cheat-entry"><span class="label">Refund / Owed</span><span class="value ' +
                (cs.refundOrOwed >= 0 ? 'gain-positive' : 'gain-negative') + '">$' + fmt(cs.refundOrOwed) + '</span></div>' +
                '</div>';

            document.getElementById('cheatSheetContent').innerHTML = html;
        } else {
            csCard.style.display = 'none';
        }

        // Disclaimer
        if (result.disclaimer) {
            document.getElementById('disclaimerText').textContent = result.disclaimer;
        }

        document.getElementById('resultsSection').style.display = 'block';
        document.getElementById('resultsSection').scrollIntoView({ behavior: 'smooth' });
    }

    // --- Copy Cheat Sheet ---
    document.getElementById('copyCheatSheetBtn').addEventListener('click', () => {
        const el = document.getElementById('cheatSheetContent');
        const text = el.innerText;
        navigator.clipboard.writeText(text).then(() => {
            const btn = document.getElementById('copyCheatSheetBtn');
            btn.textContent = '✅ Copied!';
            btn.classList.add('copied');
            setTimeout(() => {
                btn.textContent = '📋 Copy to Clipboard';
                btn.classList.remove('copied');
            }, 2000);
        }).catch(() => {
            alert('Failed to copy to clipboard.');
        });
    });

    // --- Helpers ---
    function showError(message) {
        document.getElementById('errorMessage').textContent = message;
        document.getElementById('errorSection').style.display = 'block';
    }

    function summaryRow(label, value, numVal, fullWidth) {
        const cls = fullWidth ? ' full-width' : '';
        let valueClass = 'summary-value';
        if (typeof numVal === 'number') {
            valueClass += numVal >= 0 ? ' gain-positive' : ' gain-negative';
        }
        return '<div class="summary-item' + cls + '"><span class="summary-label">' + esc(label) +
            '</span><span class="' + valueClass + '">' + esc(value) + '</span></div>';
    }

    function fmt(n) {
        if (n == null || isNaN(n)) return '0.00';
        return Number(n).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function fmtDate(d) {
        if (!d) return '—';
        const dt = new Date(d);
        if (isNaN(dt)) return d;
        return dt.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    function esc(s) {
        if (s == null) return '';
        const el = document.createElement('span');
        el.textContent = String(s);
        return el.innerHTML;
    }
})();

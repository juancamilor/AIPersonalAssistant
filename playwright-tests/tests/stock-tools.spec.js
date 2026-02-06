// Smoke tests for the Stock Tools page
const { test, expect } = require('@playwright/test');

const STOCK_TOOLS_URL = 'https://localhost:7028/stock-tools.html';

test.describe('Stock Tools - Smoke Tests', () => {

  test('should navigate to Stock Tools page', async ({ page }) => {
    await page.goto(STOCK_TOOLS_URL);
    await expect(page).toHaveTitle(/Stock Tools/);
  });

  test('MSFT should be pre-selected in the stocks list', async ({ page }) => {
    await page.goto(STOCK_TOOLS_URL);
    await page.waitForSelector('#selectedStocks');

    const msftTag = page.locator('#selectedStocks', { hasText: 'MSFT' });
    await expect(msftTag).toBeVisible();
  });

  test('date inputs should be present and have values', async ({ page }) => {
    await page.goto(STOCK_TOOLS_URL);

    const startDate = page.locator('#startDate');
    const endDate = page.locator('#endDate');

    await expect(startDate).toBeVisible();
    await expect(endDate).toBeVisible();

    // The JS sets default dates on load
    await expect(startDate).not.toHaveValue('');
    await expect(endDate).not.toHaveValue('');
  });

  test('chart section/container should exist', async ({ page }) => {
    await page.goto(STOCK_TOOLS_URL);

    const chartContainer = page.locator('.chart-container');
    await expect(chartContainer).toHaveCount(1);

    const canvas = page.locator('#stockChart');
    await expect(canvas).toHaveCount(1);
  });

  test('stock search input should be present', async ({ page }) => {
    await page.goto(STOCK_TOOLS_URL);

    const stockSymbol = page.locator('#stockSymbol');
    await expect(stockSymbol).toBeVisible();

    // Verify it has stock options
    const options = stockSymbol.locator('option');
    await expect(options).toHaveCount(4); // placeholder + 3 stocks
  });

});

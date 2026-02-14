// Smoke tests for Rate Exchange page structure
const { test, expect } = require('@playwright/test');

test.describe('Rate Exchange - Page Structure', () => {

  test('Page loads and currency selection area exists', async ({ page }) => {
    await page.goto('/rate-exchange.html');

    const fromCurrency = page.locator('#fromCurrency');
    await expect(fromCurrency).toBeVisible();

    const checkboxGroup = page.locator('.checkbox-group');
    await expect(checkboxGroup).toBeVisible();

    await page.screenshot({ path: 'screenshots/rate-exchange-currency.png', fullPage: false });
  });

  test('Convert button is present', async ({ page }) => {
    await page.goto('/rate-exchange.html');

    const calculateBtn = page.locator('.calculate-btn');
    await expect(calculateBtn).toBeVisible();
    await expect(calculateBtn).toHaveText('Calculate Exchange Rates');
  });

  test('History chart section element exists', async ({ page }) => {
    await page.goto('/rate-exchange.html');

    const historySection = page.locator('#historyChartSection');
    await expect(historySection).toBeAttached();

    const historyCanvas = page.locator('#historyChart');
    await expect(historyCanvas).toBeAttached();
  });

});


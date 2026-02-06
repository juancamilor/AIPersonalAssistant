// Smoke tests for Rate Exchange page structure
const { test, expect } = require('@playwright/test');

test.describe('Rate Exchange - Page Structure', () => {

  test('Page loads and currency selection area exists', async ({ page }) => {
    await page.goto('https://localhost:7028/rate-exchange.html');

    // Verify from-currency dropdown exists
    const fromCurrency = page.locator('#fromCurrency');
    await expect(fromCurrency).toBeVisible();

    // Verify to-currency checkboxes exist
    const checkboxGroup = page.locator('.checkbox-group');
    await expect(checkboxGroup).toBeVisible();

    await page.screenshot({ path: 'screenshots/rate-exchange-currency.png', fullPage: false });
  });

  test('Convert button is present', async ({ page }) => {
    await page.goto('https://localhost:7028/rate-exchange.html');

    const calculateBtn = page.locator('.calculate-btn');
    await expect(calculateBtn).toBeVisible();
    await expect(calculateBtn).toHaveText('Calculate Exchange Rates');
  });

  test('History chart section element exists', async ({ page }) => {
    await page.goto('https://localhost:7028/rate-exchange.html');

    // Section exists in DOM (hidden by default until a conversion is done)
    const historySection = page.locator('#historyChartSection');
    await expect(historySection).toBeAttached();

    const historyCanvas = page.locator('#historyChart');
    await expect(historyCanvas).toBeAttached();
  });

  test('Date input is present', async ({ page }) => {
    await page.goto('https://localhost:7028/rate-exchange.html');

    const dateInput = page.locator('#date');
    await expect(dateInput).toBeVisible();
    await expect(dateInput).toHaveAttribute('type', 'date');
  });

});

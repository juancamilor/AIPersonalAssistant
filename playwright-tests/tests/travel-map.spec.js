// Smoke tests for the Travel Map page
const { test, expect } = require('@playwright/test');

test.describe('Travel Map Smoke Tests', () => {

  test.beforeEach(async ({ page }) => {
    // Intercept the pins API to prevent auth redirect during page load
    await page.route('**/api/travel/pins', route =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
    );
    await page.goto('/travel-map.html');
  });

  test('map container element is present', async ({ page }) => {
    const mapDiv = page.locator('#map');
    await expect(mapDiv).toBeVisible();
  });

  test('map uses CartoDB Voyager tiles', async ({ page }) => {
    // Leaflet renders tile images; verify at least one tile src contains the Voyager URL
    const tile = page.locator('img.leaflet-tile[src*="basemaps.cartocdn.com/rastertiles/voyager"]').first();
    await expect(tile).toBeAttached({ timeout: 10000 });
  });

  test('pin modal elements exist', async ({ page }) => {
    const modal = page.locator('#pinModal');
    await expect(modal).toBeAttached();

    // Modal should be hidden by default
    await expect(modal).toBeHidden();

    // Verify key form fields inside the modal
    await expect(page.locator('#placeName')).toBeAttached();
    await expect(page.locator('#dateVisited')).toBeAttached();
    await expect(page.locator('#notes')).toBeAttached();
    await expect(page.locator('#saveBtn')).toBeAttached();
    await expect(page.locator('#cancelBtn')).toBeAttached();
    await expect(page.locator('#deleteBtn')).toBeAttached();
  });

  test('drop zone element exists in the modal', async ({ page }) => {
    const dropZone = page.locator('#dropZone');
    await expect(dropZone).toBeAttached();

    // Drop zone should contain a file input
    await expect(page.locator('#dropZone #fileInput')).toBeAttached();

    // Drop zone should have descriptive text
    await expect(dropZone).toContainText('Drag & drop');
  });

  test('back button navigates to tools page', async ({ page }) => {
    const backBtn = page.locator('#backBtn');
    await expect(backBtn).toBeVisible();
    await expect(backBtn).toContainText('Back to Tools');
  });

});

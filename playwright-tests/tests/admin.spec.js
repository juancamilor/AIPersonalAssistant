// Smoke tests for Admin Panel page structure
const { test, expect } = require('@playwright/test');

test.describe('Admin Panel', () => {

  test.beforeEach(async ({ page }) => {
    // Mock auth endpoint to return an admin user so the page doesn't redirect
    await page.route('**/api/auth/user', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ email: 'test@example.com', isAdmin: true }),
      })
    );

    // Mock users list endpoint to avoid network errors
    await page.route('**/api/admin/users', route =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    );

    await page.goto('/admin.html');
  });

  test('Admin panel header exists', async ({ page }) => {
    const header = page.locator('.header h1');
    await expect(header).toBeVisible();
    await expect(header).toHaveText('Admin Panel');
  });

  test('Add user form exists with email input and add button', async ({ page }) => {
    const form = page.locator('#addUserForm');
    await expect(form).toBeVisible();

    const emailInput = page.locator('#emailInput');
    await expect(emailInput).toBeVisible();

    const addBtn = page.locator('#addUserBtn');
    await expect(addBtn).toBeVisible();
    await expect(addBtn).toHaveText('Add User');
  });

  test('User list table exists', async ({ page }) => {
    const table = page.locator('#usersTable');
    await expect(table).toBeAttached();
  });

  test('Back button exists', async ({ page }) => {
    const backBtn = page.locator('#backBtn');
    await expect(backBtn).toBeVisible();
  });

});

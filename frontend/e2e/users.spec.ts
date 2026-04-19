import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { UsersPage } from './pages/UsersPage';

/**
 * E2E tests for the users page.
 * Verifies the full data flow: React → BFF (with JWT) → Backend API → EF InMemory DB.
 */
test.describe('Users page', () => {
  // Log in before each test
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin@demo.com', 'Admin123!');
    await expect(page).toHaveURL(/\/users/);
  });

  test('shows the users table', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await expect(usersPage.table).toBeVisible();
  });

  test('shows the logged-in user name in the header', async ({ page }) => {
    // Scope to <header> to avoid ambiguity with the users table row
    await expect(page.locator('header').getByText(/Admin Demo/i)).toBeVisible();
  });

  test('table contains seed users from the backend', async ({ page }) => {
    const usersPage = new UsersPage(page);
    const emails = await usersPage.getEmails();

    expect(emails).toContain('admin@demo.com');
    expect(emails).toContain('user@demo.com');
  });

});

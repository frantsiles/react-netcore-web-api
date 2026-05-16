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

  test('seed users show their assigned roles', async ({ page }) => {
    // Admin user row should display "Admin" role badge
    const adminRow = page.locator('tr', { has: page.getByText('admin@demo.com') });
    await expect(adminRow.getByText('Admin')).toBeVisible();

    // Viewer user row should display "Viewer" role badge
    const viewerRow = page.locator('tr', { has: page.getByText('user@demo.com') });
    await expect(viewerRow.getByText('Viewer')).toBeVisible();
  });

  test('all seed users show "Activo" status badge', async ({ page }) => {
    const activeBadges = page.getByText('Activo');
    await expect(activeBadges).toHaveCount(2);
  });

  test('header shows navigation buttons for Sesiones and Asistente IA', async ({ page }) => {
    await expect(page.getByRole('button', { name: /sesiones/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /asistente ia/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /salir/i })).toBeVisible();
  });

  test('viewer user can access users page but only sees the table', async ({ page }) => {
    // Logout first
    await page.getByRole('button', { name: /salir/i }).click();
    await expect(page).toHaveURL(/\/login/);

    // Login as viewer
    const loginPage = new LoginPage(page);
    await loginPage.login('user@demo.com', 'User123!');
    await expect(page).toHaveURL(/\/users/);

    const usersPage = new UsersPage(page);
    await expect(usersPage.table).toBeVisible();
    await expect(page.locator('header').getByText(/Demo User/i)).toBeVisible();
  });

  test('navigates to sessions page from header', async ({ page }) => {
    await page.getByRole('button', { name: /sesiones/i }).click();
    await expect(page).toHaveURL(/\/sessions/);
  });

});

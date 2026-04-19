import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { UsersPage } from './pages/UsersPage';

/**
 * E2E tests for the authentication flow.
 *
 * Flow under test:
 *   React Login form → POST /bff/auth/login → BFF → POST /api/auth/login → Backend API
 *   ← JWT token ← BFF ← React stores token ← navigates to /users
 */
test.describe('Authentication flow', () => {
  test('unauthenticated user is redirected to /login', async ({ page }) => {
    await page.goto('/users');
    await expect(page).toHaveURL(/\/login/);
  });

  test('login page shows the form', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    await expect(page.getByRole('heading', { name: /demo app/i })).toBeVisible();
    await expect(loginPage.emailInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.submitButton).toBeVisible();
  });

  test('login with invalid credentials shows error message', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    await loginPage.login('admin@demo.com', 'WrongPassword1!');

    await expect(loginPage.errorMessage).toBeVisible();
    // Should stay on /login
    await expect(page).toHaveURL(/\/login/);
  });

  test('login with valid credentials redirects to /users', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    await loginPage.login('admin@demo.com', 'Admin123!');

    await expect(page).toHaveURL(/\/users/);
    await expect(page.getByRole('heading', { name: /users/i })).toBeVisible();
  });

  test('logout redirects back to /login', async ({ page }) => {
    // Log in first
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin@demo.com', 'Admin123!');
    await expect(page).toHaveURL(/\/users/);

    // Log out
    const usersPage = new UsersPage(page);
    await usersPage.logout();

    await expect(page).toHaveURL(/\/login/);
  });

  test('viewer user can log in and access users page', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();

    await loginPage.login('user@demo.com', 'User123!');

    await expect(page).toHaveURL(/\/users/);
  });
});

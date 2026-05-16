import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { SessionsPage } from './pages/SessionsPage';
import { UsersPage } from './pages/UsersPage';

/**
 * E2E tests for the sessions page (/sessions).
 *
 * Flow under test:
 *   Browser → GET /bff/sessions/my → BFF (JWT validation) → GET /api/sessions/my → Backend API
 *   Revoke: PATCH /bff/sessions/{id}/revoke → BFF → PATCH /api/sessions/{id}/revoke
 */
test.describe('Sessions page', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin@demo.com', 'Admin123!');
    await expect(page).toHaveURL(/\/users/);
  });

  test('navigates to sessions from the users page header', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.navigate();

    await expect(page).toHaveURL(/\/sessions/);
    await expect(sessionsPage.heading).toBeVisible();
  });

  test('shows the sessions table', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.goto();

    await expect(sessionsPage.table).toBeVisible();
  });

  test('current session is marked with the "Esta sesión" badge', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.goto();

    await expect(sessionsPage.currentSessionBadge).toBeVisible();
  });

  test('shows "Cerrar todas las otras sesiones" button', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.goto();

    await expect(sessionsPage.revokeAllButton).toBeVisible();
    await expect(sessionsPage.revokeAllButton).toBeEnabled();
  });

  test('current session does not have a Revocar button', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.goto();

    // Find the row that contains "Esta sesión" badge and confirm it has no Revocar button
    const currentRow = page.locator('tr', { has: page.getByText('Esta sesión') });
    await expect(currentRow.getByRole('button', { name: /revocar/i })).not.toBeVisible();
  });

  test('shows "Salir" button and navigates back to /login on logout', async ({ page }) => {
    const sessionsPage = new SessionsPage(page);
    await sessionsPage.goto();

    await page.getByRole('button', { name: /salir/i }).click();
    await expect(page).toHaveURL(/\/login/);
  });

  test('unauthenticated user is redirected to /login', async ({ page }) => {
    await page.goto('/sessions');
    await expect(page).toHaveURL(/\/login/);
  });
});

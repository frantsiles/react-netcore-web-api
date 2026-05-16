import type { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for the sessions page (/sessions).
 * Covers the "Mis sesiones activas" view with revoke actions.
 */
export class SessionsPage {
  readonly heading: Locator;
  readonly table: Locator;
  readonly revokeAllButton: Locator;
  readonly currentSessionBadge: Locator;

  constructor(private readonly page: Page) {
    this.heading           = page.getByRole('heading', { name: /mis sesiones activas/i });
    this.table             = page.getByRole('table');
    this.revokeAllButton   = page.getByRole('button', { name: /cerrar todas las otras sesiones/i });
    this.currentSessionBadge = page.getByText('Esta sesión');
  }

  async goto() {
    await this.page.goto('/sessions');
  }

  async navigate() {
    await this.page.getByRole('button', { name: /sesiones/i }).click();
  }

  /** Returns the "Revocar" buttons for non-current sessions. */
  revokeButtons(): Locator {
    return this.page.getByRole('button', { name: /revocar/i });
  }

  /** Returns the count of rows in the session table (excluding header). */
  async sessionRowCount(): Promise<number> {
    const rows = await this.table.getByRole('row').all();
    return rows.length - 1; // exclude header
  }
}

import type { Page, Locator } from "@playwright/test";

/**
 * Page Object Model for the users list page.
 */
export class UsersPage {
  readonly heading: Locator;
  readonly table: Locator;
  readonly logoutButton: Locator;
  readonly userFullName: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole("heading", { name: /usuarios del sistema/i });
    this.table = page.getByRole("table");
    this.logoutButton = page.getByRole("button", { name: /salir/i });
    this.userFullName = page.getByText(/hola, /i);
  }

  async goto() {
    await this.page.goto("/users");
  }

  /** Returns the text of all rows in the email column. */
  async getEmails(): Promise<string[]> {
    const rows = await this.table.getByRole("row").all();
    const emails: string[] = [];
    for (const row of rows.slice(1)) {
      // skip header row
      const cells = await row.getByRole("cell").all();
      if (cells[1]) emails.push(await cells[1].innerText());
    }
    return emails;
  }

  async logout() {
    await this.logoutButton.click();
  }
}

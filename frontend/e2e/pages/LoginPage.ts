import type { Page, Locator } from "@playwright/test";

/**
 * Page Object Model for the login page.
 * Encapsulates selectors and actions so tests stay readable and resilient to UI changes.
 */
export class LoginPage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;

  constructor(private readonly page: Page) {
    this.emailInput = page.getByLabel(/email/i);
    this.passwordInput = page.getByLabel(/password|contraseña/i);
    this.submitButton = page.getByRole("button", { name: /ingresar|log in/i });
    this.errorMessage = page.getByText(
      /invalid email or password|email o contraseña incorrectos/i,
    );
  }

  async goto() {
    await this.page.goto("/login");
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async isVisible() {
    return this.page.getByRole("heading", { name: /demo app/i }).isVisible();
  }
}

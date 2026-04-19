import { defineConfig, devices } from '@playwright/test';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, '..');

/**
 * Playwright E2E configuration.
 *
 * Codespaces compatibility:
 * - Chromium runs headless inside the devcontainer — no display needed.
 * - All services are on localhost (Playwright's browser runs IN the container, not the user's machine).
 * - `reuseExistingServer: true` lets you pre-start services and skip the startup wait.
 *
 * Local usage:
 *   npm run test:e2e          — starts all 3 services automatically, runs tests, tears down
 *   npm run test:e2e:ui       — opens the Playwright UI (requires DISPLAY or --headed support)
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,          // Tests share running services — avoid parallel port conflicts
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 30_000,
  reporter: [['html', { open: 'never' }], ['list']],

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    // No sandbox needed in Docker/Codespaces containers
    launchOptions: { args: ['--no-sandbox', '--disable-setuid-sandbox'] },
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], headless: true },
    },
  ],

  // Playwright starts these services before the tests and stops them after.
  // reuseExistingServer: true → if you manually started the services, Playwright won't duplicate them.
  webServer: [
    {
      name: 'Backend API',
      command: `dotnet run --project ${path.join(ROOT, 'src/Api/Api.WebApi')}`,
      url: 'http://localhost:5002/api/health',
      timeout: 120_000,
      reuseExistingServer: true,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      name: 'BFF',
      command: `dotnet run --project ${path.join(ROOT, 'src/BFF/BFF.Api')}`,
      url: 'http://localhost:5001/bff/health',
      timeout: 120_000,
      reuseExistingServer: true,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      name: 'React',
      command: 'npm run dev',
      url: 'http://localhost:5173',
      timeout: 30_000,
      reuseExistingServer: true,
    },
  ],
});

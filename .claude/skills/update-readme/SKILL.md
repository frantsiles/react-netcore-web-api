---
name: update-readme
description: Use when the user asks to update, refresh, sync, or audit the README against the actual project state. Triggers on phrases like "update the readme", "actualiza el readme", "el readme está desactualizado", "sincroniza el readme", "audita el readme".
---

# update-readme

Goal: bring `README.md` into faithful, minimal alignment with the **actual** repo state. Never invent content, never bloat, never rewrite what is already correct.

## Operating principles

1. **Audit before editing.** Read the current `README.md` first, then verify each claim against the source of truth in the repo. Only after the audit do you decide what to change.
2. **Faithful, not creative.** Every fact in the README must trace back to a file you read this session. If you can't cite the source, don't write it. Mark "needs verification" rather than guessing.
3. **Surgical edits.** Use `Edit`, never `Write`, on `README.md`. Touch only the sections that drifted. Keep voice, headings, ordering, and language (Spanish/English) of the existing document.
4. **No bloat.** Don't add sections the project doesn't justify (e.g. don't add a "Roadmap" if there isn't one). Don't list every dependency version unless the README already had a versions table.
5. **Match the repo's language.** Look at the existing README and recent commits — write updates in the same language. Don't switch languages mid-document.

## Process

### Step 1 — Delegate the audit

Spawn an `Explore` subagent with a self-contained prompt that asks it to map the project state and compare it against the current README. Do **not** do this exploration in the main context — the file/grep volume is high and you need a structured fact-sheet, not raw output.

The subagent's prompt must:
- Pass the absolute paths of the repo root and the current README.
- Ask it to read the README first, then verify each claim.
- Enumerate the audit dimensions explicitly (see checklist below).
- Demand a structured fact-sheet, not prose. Cap response at ~800 words.
- Tell it to mark "needs verification" rather than guess.

### Step 2 — Audit dimensions (give these to the subagent)

- **Tech stack & versions:** target frameworks of every `.csproj`, `package.json` deps + scripts, any global config files (`global.json`, `Directory.Build.props`, `.editorconfig`, `NuGet.config`, `tsconfig.*`, `vite.config.*`).
- **Endpoints:** every controller route + minimal API endpoint actually registered, with verbs and auth requirements.
- **Configuration:** values actually read from `appsettings.json` / env files (auth secrets, ports, base URLs, feature flags).
- **Seed data / fixtures:** users, roles, permissions, initial database state.
- **Frontend specifics:** routing, pages, services, state management, UI framework (Tailwind/MUI/etc.), proxy config.
- **Tests:** counts and scope per project, what scenarios E2E covers.
- **Scripts & tooling:** root scripts (`start.sh`, `stop.sh`, etc.), package.json scripts, devcontainer, CI workflows, editor config, Copilot/Claude instruction files.
- **Drift report:** what the README claims that no longer exists (stale), and what exists in the repo that the README doesn't mention (missing).
- **Recent commits:** last 10 — gives hints about what changed and may not be reflected yet.

### Step 3 — Decide what to change

From the fact-sheet, classify each finding:

- **Stale claim** (README says X, repo says Y) → fix.
- **Missing fact that matters** (new top-level script, new major dependency, new architectural piece) → add minimally to the most relevant existing section.
- **Missing fact that doesn't matter** (internal helper file, minor dep version bump) → skip. The README is a getting-started document, not an inventory.
- **Already correct** → leave untouched.

If a section needs more than ~5 line edits, consider whether the section is structurally outdated and propose a focused rewrite of just that section. Never rewrite the whole document.

### Step 4 — Apply edits

- Use `Edit` for each change, with enough surrounding context to make `old_string` unique.
- Preserve markdown style: existing tables stay tables, existing fenced blocks stay fenced.
- For new content, mirror the style of adjacent sections (header level, bullet style, casing).
- Keep links relative when pointing inside the repo.

### Step 5 — Report

End the turn with a tight summary: list the sections you changed, list anything you intentionally skipped (and why), and flag anything the audit marked "needs verification" so the user can confirm.

## Anti-patterns to avoid

- ❌ Re-reading the same files in the main context after the subagent already audited them.
- ❌ Adding a "Recent changes" / "Changelog" section — that belongs in `git log`.
- ❌ Adding badges, screenshots, contribution guidelines, or code of conduct unless the user asked.
- ❌ Switching from Spanish to English (or vice versa) inside the document.
- ❌ Listing exhaustive dependency versions when the README didn't already have a versions table.
- ❌ Using `Write` to overwrite `README.md` — always `Edit`.
- ❌ Asking the user "do you want me to also add X?" before doing the audit. Audit first, then propose.

## When to ask the user

Only ask if:
- The audit reveals a structural change (e.g. the project split into a monorepo) and you genuinely don't know how to represent it.
- A "stale claim" is ambiguous: the repo and README disagree, but you can't tell which is the intended state (e.g. the README mentions a service that doesn't exist — was it removed or never built?).

Otherwise: audit, edit, report.

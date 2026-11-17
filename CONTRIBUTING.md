# Contributing

## Branches

- `master` is the shared, working version. Do not commit to it directly.
- Work on your own branch, then open a pull request into `master`.
- Name branches after the work: `fix/dtr-late-status`, `feature/supervisor-reports`.

## Before you push

```sh
npm run check
```

This runs the linter, builds the web app, and runs the API tests - the same
checks GitHub runs on every push. Fix anything it reports first.

## Commit messages

A good message lets anyone (including you, months later) understand a change
from `git log` alone, and find the commit to undo when something breaks.

**Format**

```
<type>: <what changed, in plain words>

<optional: why, and anything a reviewer should know>
```

- Keep the first line under about 70 characters.
- Write it as an instruction: "Add ...", "Fix ...", not "Added" or "Fixing".
- Say *what* changed, not how you felt about it.

**Types**

| Type | Use it for |
|---|---|
| `feat` | A new feature or screen |
| `fix` | A bug fix |
| `refactor` | Code changes that do not change behavior |
| `style` | Look and layout (CSS, spacing) |
| `test` | Adding or fixing tests |
| `docs` | README, comments, guides |
| `chore` | Packages, config, build, CI |

**Examples**

| Instead of | Write |
|---|---|
| `superman spiderman` | `feat: add supervisor dashboard with department interns` |
| `okay na yung reset password` | `fix: send password reset link instead of showing an error` |
| `step 1 done: steps 2 - 6 nalang` | `feat: add step 1 of the hiring wizard (applicant details)` |
| `sum lyt` | `style: tighten sidebar spacing on small screens` |

One commit should do one thing. If the message needs "and", it is often two commits.

### Use the template (optional)

The repo includes `.gitmessage`, a template git can pre-fill when you commit.
Turn it on once for this repository:

```sh
git config commit.template .gitmessage
```

## Secrets

Never commit passwords, tokens or keys. They belong in `dotnet user-secrets`
(see the README). If one is committed by mistake, tell the team: it must be
replaced, because removing it from the file does not remove it from git history.

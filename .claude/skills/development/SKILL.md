---
name: development
description: Provides development tools and guidance. Use when developing code, debugging, or running anything in the development environment.
---

# Development

## Building the Project

Before running any build command, determine your environment by running `uname`.

Based on the output:

**If you see `MSYS_NT` (Windows CMD):**

```bash
build.bat
```

**If you see `MINGW64` (Windows Git Bash):**

```bash
cmd.exe //c build.bat
```

Note: Git Bash requires exactly two slashes (`//c`) due to path mangling behavior. Never pipe (`|`) this command.

**If you see `Linux`:**

```bash
./build.sh
```

## Version Bumping

When version bumping, update the 4 instances in: `Properties\AssemblyInfo.cs`, `src\NewGamePlus.cs`, and `package.xml`.

## Debugging Approach

When debugging issues, follow this systematic approach to avoid drowning the codebase in unnecessary logging.

### 1. Hypothesize First

Before adding any instrumentation, state 3-5 concrete, testable hypotheses. Good ones are specific and testable (e.g. "the auth token is null at checkout" or "the loop uses `count < 10` instead of `count <= 10`"). Avoid vague statements ("something is broken in the payment flow").

### 2. Instrument to Test Hypotheses

Add `DebugLogger.DebugLog` calls to confirm or reject each hypothesis (typically 2-6 logs total). Log entry/exit, key values at decision points, which branch was taken, and important return values. Don't log every line, redundant data, or things you already know are correct.

### 3. Gather Evidence

Run the code and examine the debug log. For each hypothesis, decide:

- **CONFIRMED** - The logs prove this is the issue
- **REJECTED** - The logs prove this is NOT the issue
- **INCONCLUSIVE** - Need different instrumentation to test this

Only fix issues when you have clear runtime evidence pointing to the cause. Don't guess.

### 4. Fix and Verify

Keep instrumentation in place after a fix, run a verification test, and only remove debug logs once the fix is confirmed. That avoids "fixed one thing, broke another."

## Using DebugLogger.DebugLog

Invoke `DebugLogger.DebugLog` for instrumentation. Signature:

```csharp
DebugLogger.DebugLog(
    string location,           // e.g. "UserService.cs:142"
    string message,            // e.g. "Auth token retrieved"
    string hypothesisId = null, // e.g. "A", "B"
    Dictionary<string, object> data = null
)
```

**Output:** Writes to `S:\Steam\steamapps\common\Elin\NewGamePlus\.cursor\debug.log` (under the Steam game path). You can read the file as `.cursor/debug.log` from this workspace.

**Format:** One JSON object per line (NDJSON). Each line has `id`, `timestamp` (ms since Unix epoch UTC), `location`, `message`, `data` (or `{}`), and optionally `hypothesisId` and `runId`.

**Data:** Primitives, nested dictionaries, and collections serialize to JSON; numbers use invariant culture; other types become escaped strings. On write failure, errors are swallowed so logging never affects app behavior.

### Example Usage

Instrumentation writes NDJSON (one JSON object per line) to `.cursor/debug.log`. For Cursor debugger to show entries, each line must include: **id**, **timestamp**, **location**, **message**, **data** (object). Optional: **runId**, **hypothesisId**, **sessionId**.

```csharp
// #region agent log
DebugLogger.DebugLog(
    "src/CheckoutService.cs:89",
    "Checking authentication before payment",
    "A",
    new Dictionary<string, object> {
        { "userId", currentUser?.Id },
        { "isAuthenticated", currentUser != null },
        { "token", authToken }
    }
);
// #endregion

// #region agent log
DebugLogger.DebugLog(
    "src/CheckoutService.cs:112",
    "Comparing cart total to payment amount",
    "B",
    new Dictionary<string, object> {
        { "cartTotal", cart.Total },
        { "paymentAmount", paymentRequest.Amount },
        { "matches", cart.Total == paymentRequest.Amount }
    }
);
// #endregion
```

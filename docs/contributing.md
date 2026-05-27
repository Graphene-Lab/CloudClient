# Solution – Contributing Guidelines

Thank you for considering a contribution to this project! Please read these guidelines before submitting a pull request or opening an issue.

## Scope

These guidelines apply to all projects in the **Trustless Private Cloud** solution:
`Cloud`, `CloudClient`, `CloudBox`, `CloudSync`, `EncryptedMessaging`, `CommunicationChannel`, `FullDuplexStreamSupport`, `UISupportBlazor`, `UISupportGeneric`, `SecureStorage`, `SystemExtra`, `AppSync`, `AntiGitLibrary`, `DataRedundancy`, `BackupLibrary`.

---

## Code of Conduct

Be respectful and constructive. Harassment, offensive language, and personal attacks are not tolerated.

## How to Contribute

### Reporting Bugs

1. Search existing issues before opening a new one.
2. Include: affected project/version, steps to reproduce, expected vs. actual behaviour, OS and .NET version.

### Suggesting Features

Open a GitHub issue tagged `enhancement`. Describe the problem you want to solve, not just the solution.

### Submitting Pull Requests

1. **Fork** the relevant repository.
2. Create a **feature branch**: `git checkout -b feature/my-feature`.
3. Follow the coding standards below.
4. Ensure the project **builds without warnings**: `dotnet build`.
5. If you add public API, add XML doc-comments (`<summary>`, `<param>`, `<returns>`).
6. Open a PR against the `master` branch with a clear description.

## Coding Standards

- **Language**: C# with nullable reference types enabled.
- **Style**: follow the conventions already present in the file you are editing.
- **Comments**: English only. Use XML doc-comments on all public members.
- **Encryption / Security**: any change to cryptographic code must be reviewed by the core team before merging.
- **No breaking changes** to public API without a version bump and changelog entry.
- **No new dependencies** without prior discussion in an issue.

## Security Vulnerabilities

**Do not** open a public issue for security vulnerabilities. Contact the maintainer privately (see repository contacts). Allow 90 days for a fix before public disclosure.

## Commit Messages

Use imperative mood, present tense:
- ✅ `Fix reconnection timeout in CommunicationChannel`
- ❌ `Fixed the reconnection`

## Licensing

By contributing you agree that your contributions are licensed under the same license as the project (see `LICENSE.md`).

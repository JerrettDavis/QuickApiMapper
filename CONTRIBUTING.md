# Contributing to QuickApiMapper

Thank you for your interest in contributing to QuickApiMapper! This guide will help you understand our development workflow and conventions.

## Table of Contents

- [Development Workflow](#development-workflow)
- [Conventional Commits](#conventional-commits)
- [Pull Requests](#pull-requests)
- [Testing](#testing)
- [Code Style](#code-style)

## Development Workflow

### 1. Fork and Clone

```bash
# Fork the repository on GitHub
# Clone your fork
git clone https://github.com/YOUR-USERNAME/QuickApiMapper.git
cd QuickApiMapper

# Add upstream remote
git remote add upstream https://github.com/jerrettdavis/QuickApiMapper.git
```

### 2. Create a Feature Branch

```bash
# Update your local main branch
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
```

### 3. Make Changes and Commit

Use **conventional commits** for all commit messages. See [Conventional Commits](#conventional-commits) section below.

```bash
# Stage your changes
git add .

# Commit with conventional commit message
git commit -m "feat: add new integration feature"
```

### 4. Push and Create Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name

# Create a pull request on GitHub
# The PR will be automatically validated with dry-run packaging
```

## Conventional Commits

We use **Conventional Commits** for automatic versioning and release management. All commits to main must follow this format.

### Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Version Impact | Release Triggered |
|------|-------------|----------------|-------------------|
| **feat** | New feature | MINOR (0.1.0 → 0.2.0) | Yes |
| **fix** | Bug fix | PATCH (0.1.0 → 0.1.1) | Yes |
| **perf** | Performance improvement | PATCH (0.1.0 → 0.1.1) | Yes |
| **release** | Release preparation | MINOR (0.1.0 → 0.2.0) | Yes |
| **docs** | Documentation only | None | No |
| **style** | Code style/formatting | None | No |
| **refactor** | Code refactoring | None | No |
| **test** | Adding/updating tests | None | No |
| **chore** | Maintenance tasks | None | No |
| **ci** | CI/CD changes | None | No |

### Breaking Changes

For breaking changes that require a MAJOR version bump:

**Option 1**: Add `!` after type
```bash
git commit -m "feat!: redesign API with breaking changes"
```

**Option 2**: Include `BREAKING CHANGE:` in footer
```bash
git commit -m "feat: redesign API

BREAKING CHANGE: The /api/v1 endpoints have been removed. Use /api/v2 instead."
```

### Examples

**Good Commits** :

```bash
# Feature (triggers minor release)
feat: add message capture functionality
feat(api): add new endpoint for integration testing
feat(transformers): support custom transformation plugins

# Bug fix (triggers patch release)
fix: resolve null reference in mapping engine
fix(persistence): correct SQLite connection string handling

# Performance (triggers patch release)
perf: optimize transformation pipeline
perf(cache): reduce memory usage in configuration cache

# Breaking change (triggers major release)
feat!: redesign API with new authentication model
feat(api)!: remove deprecated v1 endpoints

# Non-release commits (no version bump)
docs: update API documentation
docs(readme): add installation instructions
chore: update dependencies to latest versions
test: add integration tests for SOAP handler
refactor: extract helper methods for readability
style: format code according to editorconfig
ci: update GitHub Actions workflows
```

**Bad Commits** :

```bash
# Too vague
update stuff
fixed bug
improvements

# No prefix
added new feature
updated the API
bug fix for login

# Wrong prefix
feat: fix typo in README # Should be: docs:
fix: add authentication feature # Should be: feat:
docs: fix critical bug # Should be: fix:
```

### Scopes (Optional)

You can add a scope to provide more context:

- `api` - REST API changes
- `core` - Core mapping engine
- `transformers` - Transformer implementations
- `persistence` - Database/storage layer
- `behaviors` - Behavior pipeline
- `ui` - Web UI (Designer)
- `extensions` - Protocol extensions (gRPC, RabbitMQ, Service Bus)

Example:
```bash
feat(api): add health check endpoints
fix(core): resolve mapping engine memory leak
docs(transformers): add custom transformer guide
```

## Pull Requests

### PR Title

Use conventional commit format for PR titles:

```
feat: add message capture functionality
fix: resolve null reference in mapping engine
docs: update contributing guidelines
```

### PR Description

Use the provided PR template. Include:

- **Description**: What does this PR do?
- **Type of Change**: Feature, bug fix, documentation, etc.
- **Testing**: How was it tested?
- **Checklist**: Complete the PR checklist

### PR Validation

All PRs undergo automatic validation:

1. **Build and Test**: Full test suite must pass
2. **Code Coverage**: Coverage reports are generated
3. **Dry-run Packaging**: Validates NuGet packaging
4. **Dry-run Publishing**: Validates executable publishing
5. **Security Scan**: CodeQL analysis
6. **Dependency Review**: Checks for vulnerable dependencies

**Do not merge** until all checks pass 

### Squash Merging

When merging PRs to main:

1. Use **Squash and Merge**
2. Ensure the squashed commit message follows conventional commits
3. The squashed commit message determines version bumping

Example:
```
PR Title: Add message capture functionality
Squash Commit: feat: add message capture functionality
Result: Triggers minor version bump (0.1.0 → 0.2.0)
```

## Testing

### Running Tests Locally

```bash
# Restore dependencies
dotnet restore

# Run unit tests
dotnet test tests/QuickApiMapper.UnitTests

# Run integration tests
dotnet test tests/QuickApiMapper.IntegrationTests

# Run all tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Requirements

- All new features must include unit tests
- Bug fixes should include regression tests
- Integration tests for new protocols/extensions
- Maintain or improve code coverage

## Code Style

### .NET Conventions

We follow standard .NET coding conventions:

- Use **PascalCase** for public members
- Use **camelCase** for private fields (with `_` prefix)
- Use **async/await** for asynchronous operations
- Use **nullable reference types** (`#nullable enable`)
- Follow **SOLID principles**

### EditorConfig

The repository includes `.editorconfig`. Ensure your IDE respects these settings.

### Code Analysis

- **IDisposableAnalyzers**: Ensures proper disposal patterns
- **JetBrains.Annotations**: Code annotations for ReSharper
- **NUnit Analyzers**: Test best practices

## Release Process

Releases are **fully automated**:

1. **Merge to Main**: PR merged with conventional commit message
2. **CI Workflow**: Runs build, tests, and artifact creation
3. **Auto-Tag**: CI analyzes commits and creates tag (e.g., `v0.2.0`)
4. **Release Workflow**: Tag triggers full release pipeline
5. **Publishing**: NuGet packages and executables published automatically

**You don't need to**:
- Manually create tags
- Manually trigger releases
- Manually publish packages
- Manually update version numbers

**Just use conventional commits**, and the automation handles everything! 

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/jerrettdavis/QuickApiMapper/issues)
- **Discussions**: [GitHub Discussions](https://github.com/jerrettdavis/QuickApiMapper/discussions)
- **Documentation**: See `/docs` directory and workflow README

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

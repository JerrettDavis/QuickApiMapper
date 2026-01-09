# GitHub Workflows

This directory contains the CI/CD workflows for QuickApiMapper.

## Workflows

### PR Validation (`pr-validation.yml`)

Runs on all pull requests to validate changes before merging.

**Features**:
- Builds solution with deterministic builds
- Runs unit and integration tests
- Generates code coverage reports
- **Dry-runs the entire release pipeline**:
 - Detects all NuGet packages
 - Detects all executables
 - Performs dry-run NuGet packaging
 - Performs dry-run executable publishing
- Posts validation summary as PR comment
- Uploads dry-run artifacts for inspection

**Triggers**:
- Pull requests to `main` or `release/**` branches
- Excludes changes to docs and markdown files

### Continuous Integration (`ci.yml`)

Runs on pushes to main and release branches.

**Features**:
- Version management with Nerdbank.GitVersioning
- Full build and test suite
- Creates NuGet packages
- Publishes executables for multiple platforms (Linux, Windows)
- Uploads artifacts for later use
- Code coverage reporting
- **Automatic tag creation** based on conventional commits (main branch only)

**Triggers**:
- Pushes to `main` or `release/**` branches
- Manual workflow dispatch

**Automatic Tagging**:

The CI workflow automatically analyzes commits since the last tag and creates a new tag when conventional commits are detected:

- **`feat:`** - New features → **MINOR** version bump (0.1.0 → 0.2.0)
- **`fix:`** - Bug fixes → **PATCH** version bump (0.1.0 → 0.1.1)
- **`perf:`** - Performance improvements → **PATCH** version bump
- **`release:`** - Release commits → **MINOR** version bump
- **`feat!:`** or **`BREAKING CHANGE:`** - Breaking changes → **MAJOR** version bump (0.1.0 → 1.0.0)

When a tag is created, it automatically triggers the release workflow.

**Example**:
```bash
# This commit will trigger a minor version bump and create a tag
git commit -m "feat: add message capture functionality"

# This commit will trigger a patch version bump
git commit -m "fix: resolve null reference in mapping engine"

# This commit will trigger a major version bump
git commit -m "feat!: redesign API with breaking changes"
```

### Release (`release.yml`)

Handles the complete release process.

**Features**:
- Multi-stage release pipeline:
 1. **Validate Release** - Version validation and pre-flight checks
 2. **Build and Test** - Full test suite execution
 3. **Package NuGet** - Create NuGet packages with proper metadata
 4. **Publish Executables** - Self-contained executables for 6 platforms:
 - Linux (x64, ARM64)
 - Windows (x64, ARM64)
 - macOS (x64, ARM64)
 5. **Publish NuGet** - Push to NuGet.org
 6. **Create GitHub Release** - Automated release notes and asset uploads
 7. **Publish GitHub Packages** - Push to GitHub Packages

**Triggers**:
- Git tags matching `v*` (e.g., `v0.1.0`)
- Manual workflow dispatch with version input

**Outputs**:
- NuGet packages on NuGet.org
- NuGet packages on GitHub Packages
- GitHub Release with executables
- Automated changelog

### CodeQL (`codeql.yml`)

Security analysis using GitHub CodeQL.

**Features**:
- Security vulnerability detection
- Code quality analysis
- Weekly scheduled scans
- C# language analysis

**Triggers**:
- Pushes to `main` or `release/**`
- Pull requests
- Weekly on Mondays at 6 AM UTC

### ️ Dependency Review (`dependency-review.yml`)

Reviews dependency changes in pull requests.

**Features**:
- Detects vulnerable dependencies
- Checks license compliance
- Blocks GPL licenses
- Fails on moderate+ severity vulnerabilities

**Triggers**:
- Pull requests to `main` or `release/**`

### Update Packages (`update-packages.yml`)

Automatically checks for and updates NuGet packages.

**Features**:
- Uses `dotnet-outdated-tool`
- Only updates within major version
- Creates PR with update details
- Runs weekly on Sundays

**Triggers**:
- Weekly schedule (Sundays at midnight UTC)
- Manual workflow dispatch

### ️ Auto Label (`labeler.yml`)

Automatically labels pull requests and issues.

**Features**:
- Labels PRs based on changed files
- Labels PRs by size (XS, S, M, L, XL)
- Area labels (core, behaviors, transformers, etc.)

**Triggers**:
- PR opened, synchronized, or reopened
- Issues opened or reopened

### Stale Issues (`stale.yml`)

Manages stale issues and pull requests.

**Features**:
- Marks inactive issues as stale after 60 days
- Closes stale issues after 7 days
- Marks inactive PRs as stale after 30 days
- Closes stale PRs after 14 days
- Exempts pinned, security, and in-progress items

**Triggers**:
- Daily at midnight UTC
- Manual workflow dispatch

## Secrets Required

To fully utilize all workflows, configure these secrets in repository settings:

### Required Secrets

- `GITHUB_TOKEN` - Automatically provided by GitHub

### Optional Secrets

- `NUGET_API_KEY` - For publishing to NuGet.org (release workflow)
- `CODECOV_TOKEN` - For code coverage reports (recommended)

## Setting Up Secrets

1. Go to repository **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Add secrets as needed

## Workflow Dependencies

```
PR Validation
 ├─ Build solution
 ├─ Run tests
 ├─ Dry-run packaging
 └─ Dry-run publishing

Continuous Integration
 ├─ Build and Test
 ├─ Publish Executables
 │ ├─ Linux (x64)
 │ └─ Windows (x64)
 └─ Auto Tag (main branch only)
 ├─ Analyze conventional commits
 ├─ Create tag if needed
 └─ Triggers → Release workflow

Release (triggered by tag creation)
 ├─ Validate Release
 ├─ Build and Test
 ├─ Package NuGet
 ├─ Publish Executables (6 platforms)
 ├─ Publish NuGet
 ├─ Create GitHub Release
 └─ Publish GitHub Packages
```

## Versioning

Versioning is managed by **Nerdbank.GitVersioning** with **Conventional Commits**:

- Base version configured in `version.json` (root directory): **0.1**
- Automatically calculates version from git history
- Supports semantic versioning (SemVer 2.0)
- Pre-release builds get alpha/beta tags
- **Automatic version bumps** based on conventional commit messages

### Conventional Commit Format

Use these prefixes in commit messages to control versioning:

| Commit Prefix | Example | Version Impact | Release Trigger |
|---------------|---------|----------------|-----------------|
| `feat:` | `feat: add user authentication` | 0.1.0 → **0.2.0** (MINOR) | Yes |
| `fix:` | `fix: resolve login bug` | 0.1.0 → **0.1.1** (PATCH) | Yes |
| `perf:` | `perf: optimize database queries` | 0.1.0 → **0.1.1** (PATCH) | Yes |
| `release:` | `release: prepare for v1.0` | 0.1.0 → **0.2.0** (MINOR) | Yes |
| `feat!:` | `feat!: redesign API` | 0.1.0 → **1.0.0** (MAJOR) | Yes |
| `BREAKING CHANGE:` | with breaking change footer | 0.1.0 → **1.0.0** (MAJOR) | Yes |
| `docs:` | `docs: update README` | No change | No |
| `chore:` | `chore: update dependencies` | No change | No |
| `style:` | `style: format code` | No change | No |
| `refactor:` | `refactor: extract helper method` | No change | No |
| `test:` | `test: add unit tests` | No change | No |

### Version Calculation

1. **Base Version**: Defined in `version.json` (currently 0.1)
2. **Version Height**: Number of commits since version change
3. **Final Version**: Base version + automated increments based on commits
4. **Tag Format**: `v{Major}.{Minor}.{Patch}` (e.g., v0.1.0, v0.2.0, v1.0.0)

**Current version**: 0.1.x (will auto-increment on conventional commits to main)

## Branch Strategy

- **main** - Main development branch
- **release/vX.Y** - Release branches for version X.Y
- **feature/** - Feature branches
- **fix/** - Bug fix branches

## Tags

### Automatic Tag Creation (Recommended)

Tags are **automatically created** by the CI workflow when conventional commits are pushed to main:

```bash
# Make changes and commit with conventional commit message
git add .
git commit -m "feat: add new integration feature"
git push origin main

# CI workflow will:
# 1. Detect the "feat:" prefix
# 2. Calculate new version with nbgv
# 3. Create tag automatically (e.g., v0.2.0)
# 4. Trigger release workflow
```

### Manual Tag Creation (Not Recommended)

You can still manually create tags if needed, but this bypasses the conventional commit workflow:

```bash
git tag v0.1.0
git push origin v0.1.0
```

**Note**: Manual tags will trigger the release workflow immediately without the safeguards of CI validation.

## Manual Releases

To trigger a manual release:

1. Go to **Actions** → **Release** workflow
2. Click **Run workflow**
3. Enter version (e.g., `0.1.0`)
4. Check **Mark as pre-release** if needed
5. Click **Run workflow**

## Monitoring Workflows

- View all workflow runs: **Actions** tab
- Check individual run details for logs
- Download artifacts from completed runs
- Review test results and coverage reports

## Troubleshooting

### Workflow Fails to Start

- Check trigger conditions match
- Verify required secrets are set
- Check workflow syntax with `yamllint`

### Build Failures

- Review build logs in Actions tab
- Check for dependency issues
- Verify .NET SDK version compatibility

### Test Failures

- Review test logs for details
- Check for environment-specific issues
- Ensure test data is properly seeded

### Publishing Failures

- Verify NUGET_API_KEY is set correctly
- Check NuGet.org service status
- Ensure version doesn't already exist

## Best Practices

1. **Always let PR validation complete** before merging
2. **Review dry-run artifacts** to catch packaging issues early
3. **Use conventional commits** for automatic versioning and releases
4. **Choose the right commit prefix**:
 - Use `feat:` for new features that users will notice
 - Use `fix:` for bug fixes
 - Use `perf:` for performance improvements
 - Use `docs:`, `chore:`, `style:`, `refactor:`, `test:` for non-release changes
 - Use `feat!:` or include `BREAKING CHANGE:` in commit body for breaking changes
5. **Keep workflows updated** with Dependabot
6. **Monitor security alerts** from CodeQL
7. **Review dependency updates** from automated PRs
8. **Avoid force-pushing to main** - it can break version history
9. **Squash merge PRs** with conventional commit messages for cleaner history

### Conventional Commit Examples

**Good commits that trigger releases**:
```bash
feat: add message capture and replay functionality
fix: resolve null reference in mapping engine
perf: optimize transformation pipeline
feat(api)!: redesign REST endpoints with breaking changes
```

**Good commits that don't trigger releases**:
```bash
docs: update API documentation
chore: update dependencies to latest versions
test: add integration tests for SOAP transformations
refactor: extract helper methods for readability
style: format code according to style guide
```

**Bad commits** (avoid these):
```bash
# Too vague
update stuff

# No prefix
added new feature

# Wrong prefix usage
feat: fix typo in README # Should be docs:
fix: add new feature # Should be feat:
```

## Contributing

When modifying workflows:

1. Test changes in a fork first
2. Use workflow validation tools
3. Document significant changes
4. Update this README if needed
5. Follow existing patterns and conventions

## Support

For workflow issues:

1. Check workflow logs in Actions tab
2. Review this documentation
3. Open an issue with workflow logs attached
4. Tag with `ci/cd` label

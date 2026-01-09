# Contributing to QuickApiMapper

Thank you for your interest in contributing to QuickApiMapper! This guide will help you get started.

## Code of Conduct

Be respectful, inclusive, and professional in all interactions.

## Getting Started

### Prerequisites

- **.NET 10 SDK** or later
- **Git** version control
- **Visual Studio 2022**, **VS Code**, or **Rider**
- **Docker** (optional, for running dependencies)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork:
 ```bash
 git clone https://github.com/YOUR-USERNAME/QuickApiMapper.git
 cd QuickApiMapper
 ```
3. Add upstream remote:
 ```bash
 git remote add upstream https://github.com/jerrettdavis/QuickApiMapper.git
 ```

### Build the Solution

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

### Run the Application

**Option 1: With Aspire** (Recommended)
```bash
dotnet run --project src/QuickApiMapper.Host.AppHost
```

**Option 2: Individual Services**
```bash
# Terminal 1: Management API
dotnet run --project src/QuickApiMapper.Management.Api

# Terminal 2: Runtime Web API
dotnet run --project src/QuickApiMapper.Web

# Terminal 3: Web Designer
dotnet run --project src/QuickApiMapper.Designer.Web
```

## Project Structure

```
QuickApiMapper/
├── src/ # Source code
│ ├── QuickApiMapper.Contracts/ # Core interfaces
│ ├── QuickApiMapper.Application/ # Mapping engine
│ ├── QuickApiMapper.Behaviors/ # Built-in behaviors
│ ├── QuickApiMapper.Persistence.*/ # Database providers
│ ├── QuickApiMapper.Extensions.*/ # Protocol extensions
│ ├── QuickApiMapper.MessageCapture.*/ # Message capture
│ ├── QuickApiMapper.Web/ # Runtime API
│ ├── QuickApiMapper.Management.Api/ # Management API
│ └── QuickApiMapper.Designer.Web/ # Web UI
├── tests/ # Test projects
│ ├── QuickApiMapper.UnitTests/
│ └── QuickApiMapper.IntegrationTests/
├── docs/ # Documentation
└── QuickApiMapper.sln # Solution file
```

## How to Contribute

### Reporting Bugs

1. **Search existing issues** - Check if the bug is already reported
2. **Create a new issue** with:
 - Clear, descriptive title
 - Steps to reproduce
 - Expected behavior
 - Actual behavior
 - Environment (OS, .NET version, etc.)
 - Screenshots (if applicable)

**Bug Report Template**:
```markdown
**Description**
A clear description of the bug.

**To Reproduce**
1. Go to '...'
2. Click on '....'
3. See error

**Expected Behavior**
What you expected to happen.

**Actual Behavior**
What actually happened.

**Environment**
- OS: [e.g., Windows 11]
- .NET Version: [e.g., 10.0]
- QuickApiMapper Version: [e.g., 1.0.0]

**Screenshots**
If applicable, add screenshots.
```

### Suggesting Features

1. **Search existing issues** - Check if the feature is already requested
2. **Create a new issue** with:
 - Clear, descriptive title
 - Use case description
 - Proposed solution (if any)
 - Alternative solutions considered
 - Additional context

**Feature Request Template**:
```markdown
**Is your feature request related to a problem?**
A clear description of the problem.

**Describe the solution you'd like**
A clear description of what you want to happen.

**Describe alternatives you've considered**
Alternative solutions or features you've considered.

**Additional context**
Any other context or screenshots.
```

### Submitting Pull Requests

1. **Create a branch**:
 ```bash
 git checkout -b feature/your-feature-name
 ```

2. **Make your changes**:
 - Follow coding standards (see below)
 - Write tests for new features
 - Update documentation

3. **Commit your changes**:
 ```bash
 git add .
 git commit -m "feat: add new transformer for date formatting"
 ```

4. **Push to your fork**:
 ```bash
 git push origin feature/your-feature-name
 ```

5. **Create Pull Request**:
 - Go to GitHub and create a PR
 - Fill in the PR template
 - Link related issues
 - Request review

**Pull Request Template**:
```markdown
## Description
Brief description of changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Fixes #123

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings introduced
- [ ] Tests pass locally
```

## Coding Standards

### C# Style Guidelines

Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

**Naming**:
- `PascalCase` for classes, methods, properties
- `camelCase` for local variables, parameters
- `_camelCase` for private fields
- `UPPER_CASE` for constants

**Example**:
```csharp
public class CustomerTransformer : Transformer
{
 private readonly ILogger<CustomerTransformer> _logger;
 private const int MAX_RETRIES = 3;

 public CustomerTransformer(ILogger<CustomerTransformer> logger)
 {
 _logger = logger;
 }

 public override string Transform(string input, MappingContext context)
 {
 if (string.IsNullOrEmpty(input))
 return input;

 var result = ProcessInput(input);
 return result;
 }

 private string ProcessInput(string input)
 {
 // Implementation
 return input.ToUpper();
 }
}
```

### Code Organization

- One class per file
- Organize using statements
- Group related members
- Use regions sparingly

### Comments

- Use XML documentation for public APIs
- Add inline comments for complex logic
- Avoid obvious comments

**Good**:
```csharp
/// <summary>
/// Transforms phone numbers to standard format (555) 123-4567.
/// </summary>
/// <param name="input">Phone number in any format</param>
/// <param name="context">Mapping context</param>
/// <returns>Formatted phone number</returns>
public override string Transform(string input, MappingContext context)
{
 // Remove all non-numeric characters
 var digits = Regex.Replace(input, @"\D", "");

 // Format as (XXX) XXX-XXXX
 return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";
}
```

**Bad**:
```csharp
// This method transforms phone numbers
public override string Transform(string input, MappingContext context)
{
 var digits = Regex.Replace(input, @"\D", ""); // Remove non-digits
 return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}"; // Format
}
```

### Error Handling

- Use specific exception types
- Provide meaningful error messages
- Log errors with context

**Example**:
```csharp
public async Task<MappingResult> ExecuteAsync(MappingContext context)
{
 try
 {
 ValidateContext(context);
 return await ProcessMapping(context);
 }
 catch (ValidationException ex)
 {
 _logger.LogWarning(ex, "Validation failed for integration {IntegrationId}", context.IntegrationId);
 return new MappingResult
 {
 IsSuccess = false,
 ErrorMessage = $"Validation failed: {ex.Message}"
 };
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Unexpected error in integration {IntegrationId}", context.IntegrationId);
 throw;
 }
}
```

## Testing

### Unit Tests

Write unit tests for all new functionality.

**Test Structure**:
```csharp
[Test]
public void Transform_ValidPhoneNumber_ReturnsFormattedNumber()
{
 // Arrange
 var transformer = new FormatPhoneTransformer();
 var context = new MappingContext();
 var input = "5551234567";

 // Act
 var result = transformer.Transform(input, context);

 // Assert
 Assert.AreEqual("(555) 123-4567", result);
}
```

**Test Naming**: `MethodName_Scenario_ExpectedResult`

**Coverage**: Aim for > 80% code coverage

### Integration Tests

Test end-to-end scenarios:

```csharp
[Test]
public async Task CustomerIntegration_ValidInput_ReturnsTransformedOutput()
{
 // Arrange
 var integration = await CreateTestIntegration();
 var input = LoadSampleInput("customer.json");

 // Act
 var result = await _mappingEngine.ApplyMappingAsync(integration, input);

 // Assert
 Assert.IsTrue(result.IsSuccess);
 Assert.IsNotNull(result.TransformedPayload);
 StringAssert.Contains("<FirstName>John</FirstName>", result.TransformedPayload);
}
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/QuickApiMapper.UnitTests

# With coverage
dotnet test /p:CollectCoverage=true
```

## Documentation

### Code Documentation

Use XML documentation comments:

```csharp
/// <summary>
/// Transforms field values during the mapping process.
/// </summary>
public interface ITransformer
{
 /// <summary>
 /// Transforms the input value.
 /// </summary>
 /// <param name="input">The value to transform</param>
 /// <param name="context">The mapping context containing source data and configuration</param>
 /// <returns>The transformed value</returns>
 string Transform(string input, MappingContext context);
}
```

### Documentation Updates

When adding features, update:

1. **README.md** - If adding major features
2. **API Documentation** - For new public APIs
3. **User Guide** - For user-facing features
4. **Migration Guide** - For breaking changes

### Writing Documentation

Follow the existing style:
- Use clear, concise language
- Provide code examples
- Include use cases
- Add troubleshooting sections

## Commit Message Guidelines

Follow [Conventional Commits](https://www.conventionalcommits.org/):

**Format**:
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation changes
- `style` - Code style changes (formatting, etc.)
- `refactor` - Code refactoring
- `test` - Adding tests
- `chore` - Build process or auxiliary tool changes

**Examples**:
```
feat(transformers): add currency conversion transformer

Adds a new transformer that converts between currencies using
exchange rate API.

Closes #123
```

```
fix(soap): correct XML namespace handling

Fixed issue where custom namespaces were not properly applied
to SOAP envelopes.

Fixes #456
```

```
docs(getting-started): update installation instructions

Updated installation guide with Docker Compose setup and
troubleshooting section.
```

## Release Process

1. **Version Bump** - Update version in `Directory.Build.props`
2. **Changelog** - Update CHANGELOG.md
3. **Tag** - Create Git tag: `v1.2.3`
4. **Build** - Create release builds
5. **Publish** - Publish NuGet packages
6. **Release Notes** - Create GitHub release

## Community

- **GitHub Discussions** - Ask questions, share ideas
- **GitHub Issues** - Report bugs, request features
- **Pull Requests** - Contribute code

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

If you have questions, feel free to:
- Open a GitHub Discussion
- Create an issue
- Contact the maintainers

Thank you for contributing to QuickApiMapper! 

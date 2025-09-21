# Contributing to Temenos Service Management Platform

We welcome contributions to the Temenos Service Management Platform! This guide will help you get started with contributing to the project.

## Development Environment Setup

### Prerequisites
- .NET 8 SDK
- PowerShell 7+
- Git
- Visual Studio 2022 or VS Code
- SQL Server or SQLite for development

### Quick Start with GitHub Codespaces
1. Open the repository in GitHub Codespaces
2. Wait for the development environment to initialize
3. The solution will be automatically restored and built
4. Start developing immediately!

### Local Development Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/asembereng/Temenos-Scripts.git
   cd Temenos-Scripts
   ```

2. Restore and build:
   ```bash
   dotnet restore TemenosAlertManager.sln
   dotnet build TemenosAlertManager.sln
   ```

3. Run the application:
   ```bash
   cd src/TemenosAlertManager.Api
   dotnet run
   ```

## Code Standards

### C# Coding Standards
- Follow Microsoft C# coding conventions
- Use meaningful names for variables, methods, and classes
- Add XML documentation for public APIs
- Use async/await for I/O operations
- Implement proper error handling and logging

### PowerShell Standards
- Use approved verbs for function names
- Include parameter validation
- Add comprehensive help documentation
- Use Write-Verbose for detailed logging
- Follow PowerShell best practices

### Code Style
```csharp
// Good example
public async Task<OperationResult> StartSODAsync(SODRequest request, string userId)
{
    _logger.LogInformation("Starting SOD operation for environment {Environment} by user {UserId}", 
        request.Environment, userId);
    
    try
    {
        // Implementation
        return new OperationResult { Success = true };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to start SOD operation");
        throw;
    }
}
```

## Testing Guidelines

### Unit Tests
- Write unit tests for all business logic
- Use xUnit for C# testing
- Mock external dependencies
- Aim for 80%+ code coverage

### Integration Tests
- Test API endpoints end-to-end
- Use TestHost for in-memory testing
- Test PowerShell module integration
- Validate database operations

### Testing Framework
```csharp
[Fact]
public async Task StartSOD_WithValidRequest_ReturnsSuccess()
{
    // Arrange
    var request = new SODRequest { Environment = "TEST" };
    
    // Act
    var result = await _service.StartSODAsync(request, "testuser");
    
    // Assert
    Assert.True(result.Success);
}
```

## Pull Request Process

### Before Submitting
1. Ensure all tests pass
2. Update documentation as needed
3. Follow the branching strategy
4. Add appropriate labels

### Branch Naming Convention
- `feature/description` - New features
- `bugfix/description` - Bug fixes
- `hotfix/description` - Critical fixes
- `docs/description` - Documentation updates

### Commit Message Format
```
type(scope): description

- feat: new feature
- fix: bug fix
- docs: documentation
- style: formatting
- refactor: code restructuring
- test: adding tests
- chore: maintenance
```

Example:
```
feat(sod): add dependency validation for SOD operations

- Implement pre-validation checks for service dependencies
- Add comprehensive error handling for validation failures
- Update tests for new validation logic
```

### Pull Request Template
1. **Description**: Clear description of changes
2. **Type**: Feature, Bug Fix, Documentation, etc.
3. **Testing**: How the changes were tested
4. **Breaking Changes**: Any breaking changes
5. **Checklist**: Pre-submission checklist

## Documentation Requirements

### Code Documentation
- XML documentation for all public APIs
- README updates for new features
- PowerShell help documentation
- Architecture decision records (ADRs)

### User Documentation
- Update relevant user guides
- API documentation updates
- Configuration examples
- Troubleshooting guides

## Security Guidelines

### Security Best Practices
- Never commit secrets or credentials
- Use secure coding practices
- Validate all inputs
- Implement proper authentication and authorization
- Follow OWASP guidelines

### Sensitive Data
- Use configuration for sensitive data
- Implement proper encryption
- Follow principle of least privilege
- Regular security reviews

## Performance Guidelines

### Performance Best Practices
- Use async/await for I/O operations
- Implement proper caching strategies
- Optimize database queries
- Monitor resource usage
- Profile performance regularly

### Database Guidelines
- Use Entity Framework best practices
- Implement proper indexing
- Use connection pooling
- Avoid N+1 queries

## Release Process

### Versioning
We follow Semantic Versioning (SemVer):
- MAJOR: Breaking changes
- MINOR: New features, backward compatible
- PATCH: Bug fixes, backward compatible

### Release Notes
- Clear description of changes
- Breaking changes highlighted
- Migration guides if needed
- Known issues

## Getting Help

### Community Support
- GitHub Discussions for questions
- GitHub Issues for bugs and features
- Code reviews for learning

### Development Resources
- [Architecture Documentation](docs/IMPLEMENTATION_SPECIFICATION.md)
- [API Documentation](README.md#api-reference)
- [PowerShell Guides](docs/POWERSHELL_GUIDE.md)
- [Deployment Guides](docs/PRODUCTION_DEPLOYMENT_GUIDE.md)

## Code of Conduct

### Our Standards
- Be respectful and inclusive
- Welcome newcomers and help them learn
- Focus on constructive feedback
- Collaborate effectively

### Enforcement
Violations of the code of conduct should be reported to the project maintainers.

## Recognition

Contributors will be recognized in:
- Release notes
- Contributors section
- GitHub acknowledgments

Thank you for contributing to the Temenos Service Management Platform!
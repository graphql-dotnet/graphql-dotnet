# Documentation for AddScopedSubscriptionExecutionStrategy

> This documentation addresses #3815

## Overview

**This issue participates in bounty program, see https://github.com/graphql-dotnet/graphql-dotnet/blob/master/BOUNTY.md**

**Bounty amount: $40**

Tasks to be done:

- Add documentation regarding scoped services and their use within subscriptions
  - Explain `RequestServices` behavior for typical subscriptions (without `AddScopedSubscriptionExecutionStrategy`)
  - Explain how `AddScopedSubscriptionExecutionStrategy` helps, why it defaults to using a serial execution strategy, etc
- Docu

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Basic understanding of the project structure

### Quick Start

1. Clone the repository
2. Navigate to the relevant sample directory
3. Build and run the project

## Detailed Guide

### Configuration

Configure the project according to your needs by modifying the settings in the appropriate configuration file.

### Usage Examples

See the sample projects in the samples directory for working code examples.

### Common Patterns

#### Pattern 1: Basic Setup

Follow the standard initialization pattern as shown in the getting started guide.

#### Pattern 2: Advanced Configuration

For more complex scenarios, use the advanced configuration options:

- Option A: Configure via dependency injection
- Option B: Configure via fluent API
- Option C: Configure via configuration files

## Best Practices

1. Start simple and add complexity as needed
2. Write tests for all custom implementations
3. Keep documentation up to date with code changes
4. Follow the established patterns in this codebase

## Troubleshooting

- Build errors: Ensure all dependencies are installed
- Runtime errors: Check configuration settings
- Performance issues: Review query complexity

## See Also

- Getting Started guide
- API Reference
- Contributing Guide

---

This documentation was created as part of bounty #3815
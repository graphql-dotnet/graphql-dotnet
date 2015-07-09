# GraphQL for .NET

[![Join the chat at https://gitter.im/joemcbride/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/joemcbride/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a work-in-progress implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

This project uses [Antlr4](https://github.com/tunnelvisionlabs/antlr4cs) for the GraphQL grammar definition.

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL/).

`PM> Install-Package GraphQL`

## Roadmap

### Grammar / AST
- Grammar and AST for the GraphQL language should be complete.

### Operation Execution
- [x] Scalars
- [x] Objects
- [x] Lists of objects/interfaces
- [x] Interfaces
- [ ] Arguments
- [ ] Variables (evaluation partialially finished)
- [ ] Fragments
- [ ] Directives
- [ ] Unions
- [ ] Async execution

### Validation
- Not started

### Schema Introspection
- Not started

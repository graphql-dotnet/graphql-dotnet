# GraphQL.NET – Schema-First Demo

This sample demonstrates the **Schema-First** (SDL-First) approach supported by GraphQL.NET.

## What is Schema-First?

In the Schema-First approach you write the entire API surface as a
[GraphQL Schema Definition Language](https://graphql.org/learn/schema/) (SDL) string and then
bind .NET resolver classes to the parsed type graph. This contrasts with the Code-First approach
where .NET types *generate* the SDL.


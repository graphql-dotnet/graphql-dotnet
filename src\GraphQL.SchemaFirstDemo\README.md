# GraphQL.NET – Schema-First Demo

This sample demonstrates the **Schema-First** (SDL-First) approach supported by GraphQL.NET.

## What is Schema-First?

In the Schema-First approach you define the entire API surface using the
[GraphQL Schema Definition Language](https://graphql.org/learn/schema/) (SDL) and then wire
.NET resolver classes onto the parsed type graph. This is the opposite of the more common
Code-First approach, where you define .NET classes that *generate* the SDL.


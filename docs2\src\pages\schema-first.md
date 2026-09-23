---
title: Schema-First Approach
order: 7
---

# Schema-First Approach

The Schema-First (or _SDL-First_) approach lets you define your entire API shape using the
[GraphQL Schema Definition Language](https://graphql.org/learn/schema/) (SDL) and then map
.NET resolver classes onto those types at runtime.

## Defining the schema

Write the SDL as a string literal, a `const`, or load it from a `.graphql` file:


#!/bin/bash

echo Generating documentation for Version $1

yarn gatsby build

echo Publishing

gh-pages -d public -b master -r git@github.com:graphql-dotnet/graphql-dotnet.github.io.git -m "Documentation update for $1"

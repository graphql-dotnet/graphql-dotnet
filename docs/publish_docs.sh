#!/bin/bash

echo Generating documentation for Version $1

target=doc-target

rm -rf $target
git clone git@github.com:graphql-dotnet/graphql-dotnet.github.io.git $target

dotnet stdocs export $target Website --version $1 --directory ./src

cd $target
git add --all
git commit -a -m "Documentation update for $1" --allow-empty
git push origin master

#!/bin/bash

if [ -z "$1" ]
then

  echo
  echo ERROR: Please provide a version
  echo
  echo ex: yarn deploy 2.0.0
  echo

else

  echo Generating documentation for Version $1

  yarn gatsby build

  echo Publishing

  gh-pages -d public -b master -r git@github.com:graphql-dotnet/graphql-dotnet.github.io.git -m "Documentation update for $1"

fi

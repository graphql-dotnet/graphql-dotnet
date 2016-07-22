import fs from 'fs';
import make from './make';
import {
  artifacts,
  compile,
  fixie,
  nuget,
  nuspec,
  restore,
  settings,
  setVersion,
  appVeyorVersion,
  version
} from './tasks';

const args = process.argv.slice(2);
const notes = fs.readFileSync('./release-notes.md').toString().trim();

function buildNuspec() {
  return nuspec({
    file: './nuget/GraphQL.nuspec',
    id: 'GraphQL',
    description: 'GraphQL for .NET',
    version: settings.version,
    authors: 'Joseph T. McBride',
    owners: 'Joseph T. McBride',
    licenseUrl: 'https://github.com/graphql-dotnet/graphql-dotnet/blob/master/LICENSE.md',
    projectUrl: 'https://github.com/graphql-dotnet/graphql-dotnet',
    tags: 'GraphQL json api',
    releaseNotes: notes
  });
}

const tasks = {
  artifacts: ['nuspec', 'nuget', artifacts],
  compile: ['restore', compile],
  test: fixie,
  appVeyorVersion,
  version: [version, appVeyorVersion],
  nuspec: buildNuspec,
  nuget,
  restore,
  setVersion: () => setVersion(args[1]),
  'default': 'compile test',
  ci: 'version default artifacts'
};

make(tasks);

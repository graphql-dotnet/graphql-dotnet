import updateFile from './updateFile';

export default function setVersion(version) {
  return updateFile(
    version,
    'Updating package.json version',
    './package.json',
    data => data.replace(/"version": "(.*)"/, `"version": "${version}"`)
  ).then(updateFile(
    version,
    'Updating appveyor.yml version',
    './appveyor.yml',
    data => data.replace(/version: (.*)\./, `version: ${version}.`)
  )).then(updateFile(
    version,
    'Updating GraphQL.csproj version',
    './src/GraphQL/GraphQL.csproj',
    data => data.replace(/<VersionPrefix>(.*)<\/VersionPrefix>/, `<VersionPrefix>${version}<\/VersionPrefix>`)
  ));
}

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
    'Updating GraphQL.project.json version',
    './src/GraphQL/project.json',
    data => data.replace(/"version": "(.*)"/, `"version": "${version}"`)
  ));
}

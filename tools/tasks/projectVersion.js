import updateFile from './updateFile';
import settings from './settings';

export default function projectVersion() {
  return updateFile(
    settings.nugetVersion,
    'Updating GraphQL.project.json version',
    './src/GraphQL/project.json',
    data => data.replace(/"version": "(.*)"/, `"version": "${settings.nugetVersion}"`)
  );
}

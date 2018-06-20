import exec from './exec'
import Deferred from './Deferred';
import settings from './settings';

function test(project) {
  const platform = process.platform === 'darwin'
    ? '-f netcoreapp2.0'
    : '';
  const cmd = `dotnet test ${platform} "${project}" -c ${settings.target}`
  return exec(cmd)
}

export default function testDotnet() {
  return Promise.all([
    test('./src/GraphQL.Tests'),
    test('./src/GraphQL.DataLoader.Tests'),
    test('./src/GraphQL.Harness.Tests')
  ])
}

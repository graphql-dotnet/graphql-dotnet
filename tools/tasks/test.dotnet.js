import promiseSerial from '../promiseSerial'
import exec from './exec'

function test(target, project) {
  return () => {
    const platform = process.platform === 'darwin'
      ? '-f netcoreapp2.2'
      : ''
    const cmd = `dotnet test ${platform} "${project}" -c ${target} --no-restore`
    return exec(cmd)
  }
}

export default function testDotnet(settings) {
  return promiseSerial({
    tasks: [
      test('Debug',         './src/GraphQL.ApiTests'),
      test(settings.target, './src/GraphQL.Tests'),
      test(settings.target, './src/GraphQL.DataLoader.Tests'),
      test(settings.target, './src/GraphQL.Harness.Tests')
    ],
    settings,
    taskTimeout: settings.taskTimeout
  })
}

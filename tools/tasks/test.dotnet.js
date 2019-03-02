import promiseSerial from '../promiseSerial'
import exec from './exec'

function test(settings, project) {
  return () => {
    const platform = process.platform === 'darwin'
      ? '-f netcoreapp2.2'
      : ''
    const cmd = `dotnet test ${platform} "${project}" -c ${settings.target} --no-restore`
    return exec(cmd)
  }
}

export default function testDotnet(settings) {
  return promiseSerial({
    tasks: [
      test(settings, './src/GraphQL.Tests'),
      test(settings, './src/GraphQL.DataLoader.Tests'),
      // excluding for now, need to fix Alba integration tests
      // test(settings, './src/GraphQL.Harness.Tests')
    ],
    settings,
    taskTimeout: settings.taskTimeout
  })
}

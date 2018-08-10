import promiseSerial from '../promiseSerial'
import exec from './exec'

function test(settings, project) {
  return () => {
    const platform = process.platform === 'darwin'
      ? '-f netcoreapp2.1'
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
      test(settings, './src/GraphQL.Harness.Tests')
    ],
    settings,
    taskTimeout: settings.taskTimeout
  })
}

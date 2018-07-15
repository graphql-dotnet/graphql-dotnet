import exec from './exec'

export default function compile(settings) {
  return exec(`dotnet build ${settings.sourcePath} -c ${settings.target} --no-restore`)
}

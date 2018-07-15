import exec from './exec'

export default function compile(settings) {

  let versionSuffix = ''
  if(settings.versionSuffix.length > 0) {
    versionSuffix = `--version-suffix ${settings.versionSuffix}${settings.revision}`
  }

  const cmd = `dotnet pack src/GraphQL -o ${settings.artifacts} -c ${settings.target} ${versionSuffix} --include-symbols --include-source`
  return exec(cmd)
}

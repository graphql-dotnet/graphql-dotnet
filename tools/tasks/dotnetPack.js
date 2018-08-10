import exec from './exec'
import git from './utils/git'

export default async function compile(settings) {

  const { branch } = await git({ CI: settings.CI })

  let versions = []

  if (branch !== 'master' && branch.length > 0) {
    versions.push(branch)
  }

  if (settings.versionSuffix.length > 0) {
    versions.push(settings.versionSuffix)
  }

  versions.push(settings.revision)

  let sep = versions.length > 1 ? '-' : '.'

  versions.unshift(settings.version)

  const version = versions.join(sep)

  const cmd = `dotnet pack src/GraphQL -o ${settings.artifacts} -c ${settings.target} --include-symbols --include-source /p:PackageVersion=${version}`
  return exec(cmd)
}

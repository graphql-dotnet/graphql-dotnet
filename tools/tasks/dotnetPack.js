import exec from './exec'
import git from './utils/git'

export default async function compile(settings) {

  const { branch } = await git({ CI: settings.CI })

  let versions = []

  if (branch !== 'master' && branch.length > 0) {
    let safeBranch = branch.replace(/[^a-zA-Z0-9]/gi, '')
    versions.push(safeBranch)
  }

  if (settings.includeRevision && settings.versionSuffix.length > 0) {
    versions.push(settings.versionSuffix)
  }

  if (settings.includeRevision || versions.length > 0) {
    versions.push(settings.revision)
  }

  let sep = versions.length > 1 ? '-' : '.'

  versions.unshift(settings.version)

  const version = versions.join(sep)

  const cmd = `dotnet pack src -o ${settings.artifacts} -c ${settings.target} --include-symbols --include-source /p:PackageVersion=${version}`
  return exec(cmd)
}

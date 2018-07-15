import exec from './exec'

export default function nugetRestore() {
  return exec('dotnet restore src')
}

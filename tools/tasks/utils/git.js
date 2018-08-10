import { exec } from 'shelljs'

export default function git(settings = { CI: false }) {
  return new Promise((resolve) => {
    const branch = settings.CI
      ? process.env.APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH || 'master'
      : exec('git rev-parse --abbrev-ref HEAD', { silent: true }).stdout
    const sha = exec('git rev-parse --short HEAD', { silent: true }).stdout

    resolve({
      branch: (branch || '').trim(),
      sha: (sha || '').trim()
    })
  })
}

import { exec } from 'shelljs'
import Deferred from 'simple-make/lib/Deferred'
import { log } from 'simple-make/lib/logUtils'

const defaultOptions = {
  echo: true,
}

export default function exec2(cmd, options) {
  const { echo } = { ...defaultOptions, ...options }

  if (echo) {
    log(cmd)
  }

  const deferred = new Deferred()
  exec(cmd, (code, stdout, stderr)=> {
    if (code === 0) {
      deferred.resolve(stdout)
    } else {
      deferred.reject(stderr)
    }
  })
  return deferred.promise
}

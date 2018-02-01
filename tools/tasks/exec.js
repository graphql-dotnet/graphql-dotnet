import { exec } from 'shelljs'
import Deferred from './Deferred'

const defaultOptions = {
  echo: true,
}

export default function exec2(cmd, options) {
  const { echo } = { ...defaultOptions, ...options }

  if (echo) {
    console.log(cmd)
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

import chalk from 'chalk'
import fs from 'fs'
import request from 'superagent'
import Deferred from 'simple-make/lib/Deferred'
import { log } from 'simple-make/lib/logUtils'
import settings from './settings'

export default function appVeyorResults() {
  const deferred = new Deferred()

  if(!settings.CI) {
    log(chalk.yellow('Not on CI, skipping test output upload.'))
    deferred.resolve()
    return deferred.promise
  }

  if (!fs.existsSync(settings.testOutput)) {
    log(chalk.yellow('Test output file not available, skipping upload.'))
    deferred.resolve()
    return deferred.promise
  }

  request
    .post(`https://ci.appveyor.com/api/testresults/xunit/${settings.appVeyorJobId}`)
    .attach('file', settings.testOutput)
    .end((error, res) => {
      if(!error) {
        log('Test output upload completed.')
        deferred.resolve()
      } else {
        error(chalk.red('Test output upload error.'), chalk.red(error))
        deferred.reject(error)
      }
    })

  return deferred.promise
}

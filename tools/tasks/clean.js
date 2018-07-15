import { rm } from 'shelljs'
import { log } from 'simple-make/lib/logUtils'

export default function clean(settings) {

  const paths = settings.cleanPaths

  for(let i=0; i<paths.length; i++) {
    log(`cleaning ${paths[i]}`)
    rm('-rf', paths[i])
  }

  return Promise.resolve()
}

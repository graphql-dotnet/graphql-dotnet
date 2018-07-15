import fs from 'fs'
import path from 'path'
import { log } from 'simple-make/lib/logUtils'

export default function updateFile(note, fileName, replacer) {
  return new Promise((resolve, reject) => {
    log(note)

    const targetFile = path.resolve(fileName)
    fs.readFile(targetFile, (readError, data) => {

      if(readError) {
        reject(readError)
        return
      }

      let updated = data.toString()
      updated = replacer(updated)

      fs.writeFile(targetFile, updated, writeError => {
        if (writeError) {
          reject(writeError)
        } else {
          resolve()
        }
      })
    })
  })
}

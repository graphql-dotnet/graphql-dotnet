import fs from 'fs';
import path from 'path';

function updateFile(version, note, fileName, replacer) {
  return new Promise((resolve, reject) => {
    console.log(note);

    const targetFile = path.resolve(fileName);
    fs.readFile(targetFile, (readError, data) => {

      if(readError) {
        reject(readError);
        return;
      }

      let updated = data.toString();
      updated = replacer(updated);

      fs.writeFile(targetFile, updated, writeError => {
        if (writeError) {
          reject(writeError);
        } else {
          resolve();
        }
      });
    });
  });
}

export default function setVersion(version) {
  return updateFile(
    version,
    'Updating package version',
    './package.json',
    data => data.replace(/"version": "(.*)"/, `"version": "${version}"`)
  ).then(updateFile(
    version,
    'Updating appveyor.yml version',
    './appveyor.yml',
    data => data.replace(/version: (.*)\./, `version: ${version}.`)
  ));
}

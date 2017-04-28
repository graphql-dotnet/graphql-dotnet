import chalk from 'chalk';
import fs from 'fs';
import path from 'path';
import { exec } from 'child-process-promise';
import Deferred from './Deferred';
import settings from './settings';

function walkSync(currentDirPath, callback) {
    fs.readdirSync(currentDirPath).forEach(function (name) {
        var filePath = path.join(currentDirPath, name);
        var stat = fs.statSync(filePath);
        if (stat.isFile()) {
            callback(filePath, stat);
        } else if (stat.isDirectory()) {
            walkSync(filePath, callback);
        }
    });
}

export default function nugetRestore() {
  const deferred = new Deferred();

  const files = [];

  walkSync('./artifacts', file => files.push(file));

  files.forEach(f => {
    const command = `appveyor PushArtifact ${f}`;
    console.log(command);
  });

  if(!settings.CI) {
    console.log(chalk.yellow('Not on CI, skipping artifact upload.'));
    deferred.resolve();
    return deferred.promise;
  }

  if (!fs.existsSync(nuget)) {
    console.log(chalk.yellow('Nuget package not available, skipping artifact upload.'));
    deferred.resolve();
    return deferred.promise;
  }

  exec(command)
    .then(function(result) {
      console.log(result.stdout);
      deferred.resolve();
    })
    .fail(function (err) {
        console.error(chalk.red('ERROR: '), chalk.red(err));
        deferred.reject(err);
    });

  return deferred.promise;
}

import chalk from 'chalk';
import fs from 'fs';
import path from 'path';
import { exec } from 'child-process-promise';
import Deferred from './Deferred';
import settings from './settings';

export default function nugetRestore() {
  const deferred = new Deferred();

  const nuget = path.resolve(`./nuget/GraphQL.${settings.version}.nupkg`);
  const command = `appveyor PushArtifact ${nuget}`;
  console.log(command);

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

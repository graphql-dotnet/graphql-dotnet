import chalk from 'chalk';
import fs from 'fs';
import request from 'superagent';
import settings from './settings';
import Deferred from './Deferred';

export default function appVeyorResults() {
  const deferred = new Deferred();

  if(!settings.CI) {
    console.log(chalk.yellow('Not on CI, skipping test output upload.'));
    deferred.resolve();
    return deferred.promise;
  }

  if (!fs.existsSync(settings.testOutput)) {
    console.log(chalk.yellow('Test output file not available, skipping upload.'));
    deferred.resolve();
    return deferred.promise;
  }

  request
    .post(`https://ci.appveyor.com/api/testresults/xunit/${settings.appVeyorJobId}`)
    .attach('file', settings.testOutput)
    .end((error, res) => {
      if(!error) {
        console.log('Test output upload completed.');
        deferred.resolve();
      } else {
        console.error(chalk.red('Test output upload error.'), chalk.red(error));
        deferred.reject(error);
      }
    });

  return deferred.promise;
}

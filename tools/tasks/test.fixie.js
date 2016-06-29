import chalk from 'chalk';
import path from 'path';
import settings from './settings';
import { exec } from 'child-process-promise';
import Deferred from './Deferred';
import appVeyorResults from './appVeyorResults';

export default function fixie() {
  const runner = path.resolve('./packages/Fixie.1.0.0.33/lib/net45/fixie.console.exe');
  const params = `./src/GraphQL.Tests/bin/${settings.target}/GraphQL.Tests.dll --xUnitXml ${settings.testOutput}`;

  const deferred = new Deferred();

  exec(`${runner} ${params}`)
    .then(function (result) {
        console.log(chalk.green(result.stdout));
        appVeyorResults().then(() => {
          deferred.resolve();
        }, (error) => {
          deferred.reject(error);
        });
    })
    .fail(function (err) {
        console.error(chalk.red(err.stdout));
        appVeyorResults().then(() => {
          deferred.reject(err);
        }, (error) => {
          deferred.reject(error);
        });
    });

  return deferred.promise;
}

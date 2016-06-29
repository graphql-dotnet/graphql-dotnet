import chalk from 'chalk';
import path from 'path';
import { exec } from 'child-process-promise';
import Deferred from './Deferred';

const nuget = path.resolve('./nuget.exe');
const solution = path.resolve('./GraphQL.sln');

export default function nugetRestore() {
  const deferred = new Deferred();
  const command = `${nuget} restore ${solution}`;
  console.log(command);
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

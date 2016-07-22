import chalk from 'chalk';
import { exec } from 'child-process-promise';
import settings from './settings';
import Deferred from './Deferred';

export default function compile() {
  const deferred = new Deferred();
  const msBuild = '"C:\\Program Files (x86)\\MSBuild\\14.0\\Bin\\msbuild.exe"';
  exec(`${msBuild} GraphQL.sln /property:Configuration=${settings.target} /v:m /t:rebuild /nr:false /maxcpucount:2`)
    .then(function (result) {
        console.log(chalk.green(result.stdout));
        deferred.resolve(result);
    })
    .fail(function (err) {
        deferred.reject(err);
    });

  return deferred.promise;
}

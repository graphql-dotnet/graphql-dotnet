import { exec } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

function run(cmd) {
  console.log(cmd);

  const deferred = new Deferred();
  exec(cmd, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });

  return deferred.promise
}

export default function compile() {

  let versionSuffix = ''
  if(settings.versionSuffix.length > 0) {
    versionSuffix = `--version-suffix ${settings.versionSuffix}${settings.revision}`;
  }

  const cmd = `dotnet pack src/GraphQL -o ${settings.artifacts} -c ${settings.target} ${versionSuffix} --include-symbols --include-source`
  const one = run(cmd)

  return Promise.all[one]
}

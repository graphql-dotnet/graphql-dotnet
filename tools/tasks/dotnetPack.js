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

  const cmd = `dotnet pack src/GraphQL -o ${settings.artifacts} -c ${settings.target} ${versionSuffix} --include-symbols`
  const one = run(cmd)

  const cmd2 = `dotnet pack src/GraphQL.StarWars -o ${settings.artifacts} -c ${settings.target}`
  const two = run(cmd2)

  return Promise.all[one, two]
}

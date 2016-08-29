import { exec } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function compile() {
  const deferred = new Deferred();
  exec(`dotnet pack src/GraphQL -o nuget -c ${settings.target} --version-suffix ${settings.revision}`, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });
  return deferred.promise;
}

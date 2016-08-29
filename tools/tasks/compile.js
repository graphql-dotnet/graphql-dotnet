import { exec, rm } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function compile() {
  const deferred = new Deferred();
  rm('-rf', `src/GraphQL.Tests/obj`);
  rm('-rf', `src/GraphQL.Tests/bin`);
  exec(`dotnet build src/GraphQL.Tests -c ${settings.target}`, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });
  return deferred.promise;
}

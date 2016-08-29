import { exec } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function testDotnet() {
  const deferred = new Deferred();
  exec(`dotnet test src/GraphQL.Tests -c ${settings.target}`, {async:true}, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });

  return deferred.promise;
}

import { exec } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function testDotnet() {
  const deferred = new Deferred();

  const platform = process.platform === 'darwin'
    ? '-f netcoreapp1.0'
    : '';
  const test = `dotnet test src/GraphQL.Tests ${platform} -c ${settings.target}`;
  console.log(test)

  exec(test, {async:true}, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });

  return deferred.promise;
}

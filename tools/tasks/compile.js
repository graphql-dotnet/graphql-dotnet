import { exec, rm } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function compile() {
  const deferred = new Deferred();
  rm('-rf', `src/GraphQL.Tests/obj`);
  rm('-rf', `src/GraphQL.Tests/bin`);

  const platform = process.platform === 'darwin'
    ? '-f netcoreapp1.0'
    : '';
  const build = `dotnet build src/GraphQL.Tests ${platform} -c ${settings.target}`;
  console.log(build);

  exec(build, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });
  return deferred.promise;
}

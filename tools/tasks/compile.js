import { exec, pushd, popd } from 'shelljs';
import Deferred from './Deferred';
import settings from './settings';

export default function compile() {
  const deferred = new Deferred();

  const platform = process.platform === 'darwin'
    ? '-f netcoreapp2.0'
    : '';
  const build = `dotnet build ${platform} -c ${settings.target}`;

  pushd('src/GraphQL.Tests');
  console.log(build);

  exec(build, (code, stdout, stderr)=> {
    if(code === 0) {
      deferred.resolve();
    } else {
      deferred.reject(stderr);
    }
  });

  popd();

  return deferred.promise;
}

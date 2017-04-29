import { exec, rm } from 'shelljs';
import Deferred from './Deferred';

export default function clean() {
  const deferred = new Deferred();
  rm('-rf', `src/GraphQL/obj`);
  rm('-rf', `src/GraphQL/bin`);

  rm('-rf', `src/GraphQL.Tests/obj`);
  rm('-rf', `src/GraphQL.Tests/bin`);

  deferred.resolve();

  return deferred.promise;
}

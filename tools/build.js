import make from './make';
import {
  artifacts,
  compile,
  dotnetPack,
  dotnetTest,
  projectVersion,
  restore,
  setVersion,
  version
} from './tasks';

const args = process.argv.slice(2);

const tasks = {
  artifacts: ['nuget', artifacts],
  compile: ['restore', compile],
  test: dotnetTest,
  version: [version, projectVersion],
  nuget: dotnetPack,
  restore,
  setVersion: () => setVersion(args[1]),
  'default': 'compile test',
  ci: 'version default artifacts'
};

make(tasks);

import artifacts from './artifacts';
import compile from './compile';
import fixie from './test.fixie';
import dotnetPack from './dotnetPack';
import dotnetTest from './test.dotnet';
import nuget from './nuget';
import nuspec from './nuspec';
import restore from './restore';
import settings from './settings';
import setVersion from './setVersion';
import version from './version';
import projectVersion from './projectVersion';

export default {
  artifacts,
  compile,
  fixie,
  dotnetPack,
  dotnetTest,
  nuget: () => nuget({ version: settings.version, target: settings.target }),
  nuspec,
  projectVersion,
  restore,
  settings,
  setVersion,
  version
};

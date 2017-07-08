import artifacts from './artifacts';
import compile from './compile';
import clean from './clean';
import dotnetPack from './dotnetPack';
import dotnetTest from './test.dotnet';
import nuget from './nuget';
import nuspec from './nuspec';
import restore from './restore';
import settings from './settings';
import setVersion from './setVersion';
import version from './version';

export default {
  artifacts,
  compile,
  clean,
  dotnetPack,
  dotnetTest,
  nuget: () => nuget({ version: settings.version, target: settings.target }),
  nuspec,
  restore,
  settings,
  setVersion,
  version
};

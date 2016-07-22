import artifacts from './artifacts';
import compile from './compile';
import fixie from './test.fixie';
import nuget from './nuget';
import nuspec from './nuspec';
import restore from './restore';
import settings from './settings';
import setVersion from './setVersion';
import appVeyorVersion from './appVeyorVersion';
import version from './version';

export default {
  artifacts,
  compile,
  fixie,
  nuget: () => nuget({ version: settings.version, target: settings.target }),
  nuspec,
  restore,
  settings,
  setVersion,
  appVeyorVersion,
  version
};

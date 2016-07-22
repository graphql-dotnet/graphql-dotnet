import artifacts from './artifacts';
import compile from './compile';
import fixie from './test.fixie';
import getTasks from './getTasks';
import nuget from './nuget';
import nuspec from './nuspec';
import restore from './restore';
import runSerial from './runSerial';
import settings from './settings';
import setVersion from './setVersion';
import appVeyorVersion from './appVeyorVersion';
import version from './version';

export default {
  artifacts,
  compile,
  getTasks,
  fixie,
  nuget: () => nuget({ version: settings.version, target: settings.target }),
  nuspec,
  restore,
  runSerial,
  settings,
  setVersion,
  appVeyorVersion,
  version
};

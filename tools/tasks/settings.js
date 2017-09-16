import path from 'path';
import moment from 'moment';
import pjson from '../../package.json';

const target = process.env.CONFIGURATION || 'debug';

const buildNumber = process.env.APPVEYOR_BUILD_NUMBER;
let version = pjson.version;
const revision = buildNumber || moment().format('HHmm');
const assemblyVersion = `${version}.${revision}`;
const nugetVersion = `${version}.${revision}`;

const appVeyorJobId = process.env.APPVEYOR_JOB_ID;
const CI = process.env.CI && process.env.CI.toString().toLowerCase() === 'true';

const artifacts = path.resolve('./artifacts');

const versionSuffix = 'alpha-'

export default {
  appVeyorJobId,
  artifacts,
  CI,
  target,
  revision,
  nugetVersion,
  version: assemblyVersion,
  versionSuffix
};

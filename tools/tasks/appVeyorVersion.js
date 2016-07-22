import pjson from '../../package.json';
import updateFile from './updateFile';

export default function appVeyorResults() {
  return updateFile(
    pjson.version,
    'Updating appveyor.yml version',
    './appveyor.yml',
    data => data.replace(/version: (.*)\./, `version: ${pjson.version}.`)
  );
}

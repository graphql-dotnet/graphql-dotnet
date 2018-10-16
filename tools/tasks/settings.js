import path from 'path'
import moment from 'moment'
import pjson from '../../package.json'

const target = process.env.CONFIGURATION || 'Debug'

const buildNumber = process.env.APPVEYOR_BUILD_NUMBER
let version = pjson.version
const revision = buildNumber || moment().format('HHmm')
const includeRevision = false
const assemblyVersion = includeRevision ? `${version}.${revision}` : `${version}.0`

const appVeyorJobId = process.env.APPVEYOR_JOB_ID
const CI = process.env.CI && process.env.CI.toString().toLowerCase() === 'true'

const artifacts = path.resolve('./artifacts')

const versionSuffix = 'preview'

const cleanPaths = [
  'src/GraphQL/obj',
  'src/GraphQL/bin',
  'src/GraphQL.Tests/obj',
  'src/GraphQL.Tests/bin'
]

export default {
  appVeyorJobId,
  artifacts,
  CI,
  cleanPaths,
  slnPath: path.resolve('./src/GraphQL.sln'),
  sourcePath: path.resolve('./src'),
  target,
  version,
  revision,
  includeRevision,
  assemblyVersion,
  versionSuffix
}

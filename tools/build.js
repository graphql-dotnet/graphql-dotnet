import make from 'simple-make/lib/make'
import config from 'simple-make/lib/config'
import {
  compile,
  clean,
  dotnetPack,
  dotnetTest,
  restore,
  settings,
  setVersion,
  version
} from './tasks'

const args = process.argv.slice(2)

config.name = '[graphql-dotnet]'
config.taskTimeout = settings.taskTimeout

const tasks = {
  artifacts: ['nuget'],
  compile: [clean, 'restore', compile],
  test: ['compile', dotnetTest],
  version: [version],
  nuget: dotnetPack,
  restore,
  setVersion: () => setVersion(args[1]),
  'default': 'test',
  ci: 'version default artifacts'
}

make({ tasks, settings })

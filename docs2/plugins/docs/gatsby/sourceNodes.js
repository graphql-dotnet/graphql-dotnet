const fs = require('fs')
const chokidar = require('chokidar')
const yamlParser = require('js-yaml').safeLoad
const createNodeHelpers = require('gatsby-node-helpers').default
const { attachUrlToNavNode } = require('./attach-urls')

const {
  createNodeFactory
} = createNodeHelpers({
  typePrefix: 'Docs'
})

const MenuNode = createNodeFactory('Menu', node => {
  return node
})

const readConfigFile = filePath => {
  const fileContent = fs.readFileSync(filePath, 'utf8')
  const parsedContent = yamlParser(fileContent)
  const res = {
    id: '',
    pages: attachUrlToNavNode(parsedContent)
  }

  console.log('hrm', res)

  return res
}

const createMenuNode = filePath => {
  const pages = readConfigFile(filePath)
  const menu = MenuNode(pages)
  menu.internal.mediaType = 'application/json'

  console.log('menu', menu)

  return menu
}

module.exports = ({ boundActionCreators }, options = {}) => {
  if (!options.config) {
    throw new Error('A configuration file is required!')
  }

  const { createNode } = boundActionCreators

  // watch for file changes
  chokidar.watch(options.config)
    .on('add', configPath => createNode(createMenuNode(configPath)))
    .on('change', configPath => createNode(createMenuNode(configPath)))
    .on('unlink', configPath => {
      throw new Error(`Site configuration file '${configPath}' has been deleted. A configuration file is required.`)
    })
}

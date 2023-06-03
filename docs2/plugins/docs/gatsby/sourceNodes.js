import fs from 'fs'
import chokidar from 'chokidar'
import { load as yamlParser } from 'js-yaml'
import  { createNodeHelpers } from 'gatsby-node-helpers'
import { attachUrlToNavNode } from './attach-urls.js'



const readConfigFile = filePath => {
  const fileContent = fs.readFileSync(filePath, 'utf8')
  const parsedContent = yamlParser(fileContent)
  const res = {
    id: '',
    pages: attachUrlToNavNode(parsedContent)
  }
  return res
}



export default ({ actions, createNodeId, createContentDigest }, options = {}) => {
  if (!options.config) {
    throw new Error('A configuration file is required!')
  }

  const { createNode } = actions
  const {
    createNodeFactory
  } = createNodeHelpers({
    typePrefix: 'Docs',
    createNodeId,
    createContentDigest
  })

  const MenuNode = createNodeFactory('Menu')
  const createMenuNode = filePath => {
    const pages = readConfigFile(filePath)
    const menu = MenuNode(pages)
    menu.internal.mediaType = 'application/json'

    return menu
  }

  // watch for file changes
  chokidar.watch(options.config)
    .on('add', configPath => createNode(createMenuNode(configPath)))
    .on('change', configPath => createNode(createMenuNode(configPath)))
    .on('unlink', configPath => {
      throw new Error(`Site configuration file '${configPath}' has been deleted. A configuration file is required.`)
    })
}

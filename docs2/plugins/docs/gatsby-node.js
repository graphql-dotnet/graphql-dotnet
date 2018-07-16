const fs = require('fs')
const path = require('path')
const chokidar = require('chokidar')
const yamlParser = require('js-yaml').safeLoad
const { createFilePath } = require('gatsby-source-filesystem')
const createNodeHelpers = require('gatsby-node-helpers').default
const menuNodeType = require('./menuNodeType')

const {
  createNodeFactory,
  generateNodeId,
  generateTypeName,
} = createNodeHelpers({
  typePrefix: 'Docs'
})

const MenuNode = createNodeFactory('Menu', node => {
  return node
})

const readConfigFile = filePath => {
  const fileContent = fs.readFileSync(filePath, 'utf8')
  const parsedContent = yamlParser(fileContent)
  return {
    id: '',
    pages: parsedContent
  }
}

const createMenuNode = filePath => {
  const pages = readConfigFile(filePath)
  const menu = MenuNode(pages)
  menu.internal.mediaType = 'application/json'
  console.log('MenuNode', JSON.stringify(menu, null, 2))
  return menu
}

exports.sourceNodes = ({ boundActionCreators }, options = {}) => {
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

exports.onCreateNode = ({ node, boundActionCreators, getNode }, pluginOptions) => {
  if (node.internal.type !== 'MarkdownRemark') {
    return
  }

  console.log(pluginOptions)

  const { createNodeField } = boundActionCreators
  const slug = createFilePath({ node, getNode, basePath: 'docs' })
  createNodeField({
    node,
    name: 'slug',
    value: 'docs'+slug,
  })
}

exports.createPages = ({ graphql, boundActionCreators }) => {
  const { createPage } = boundActionCreators
  return new Promise((resolve, reject) => {
    graphql(`
      {
        allMarkdownRemark {
          edges {
            node {
              fields {
                slug
              }
            }
          }
        }
      }
    `
    ).then(result => {
      result.data.allMarkdownRemark.edges.forEach(({ node }) => {
        createPage({
          path: node.fields.slug,
          component: path.resolve('./src/templates/docs-page.js'),
          context: {
            // Data passed to context is available
            // in page queries as GraphQL variables.
            slug: node.fields.slug,
          },
        })
      })
      resolve()
    })
  })
}

exports.setFieldsOnGraphQLNodeType = actions => {
  return menuNodeType(actions)
}

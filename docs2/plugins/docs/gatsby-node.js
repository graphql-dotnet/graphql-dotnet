const path = require('path')
const sourceNodes = require('./gatsby/sourceNodes')
const menuNodeType = require('./menuNodeType')
const GithubSlugger = require('github-slugger')
const slugger = new GithubSlugger()

exports.sourceNodes = sourceNodes

exports.onCreateNode = ({ node, boundActionCreators, getNode }, options) => {
  if (node.internal.type !== 'MarkdownRemark') {
    return
  }

  const { createNodeField } = boundActionCreators

  const markdownAbsolutePath = getNode(node.parent).absolutePath
  const docsAbsolutePath = path.parse(options.config).dir

  createNodeField({
    node,
    name: 'relativePath',
    value: path.relative(docsAbsolutePath, markdownAbsolutePath)
  })
}

exports.createPages = ({ graphql, boundActionCreators }, options) => {
  const { createPage } = boundActionCreators
  const docsSiteDirectory = path.dirname(options.config)

  slugger.reset()

  return new Promise((resolve, reject) => {
    graphql(`
      {
        docsMenu {
          pages
        }
      }
    `
    ).then(result => {
      result.data.docsMenu.pages.forEach(page => {

        if (page.file && page.file.endsWith('.js')) {
            createPage({
              path: page.url,
              component: path.resolve(path.join(docsSiteDirectory, page.file)),
              context: {
                relativePath: page.url
              }
            })
            return
        }

        if (!page.sidemenu) return

        page.sidemenu.forEach(side => {
          side.items.forEach(item => {
            const pagePath = path.join(page.dir, side.dir)
            const basename = path.basename(item.file, path.extname(item.file))
            const slug = slugger.slug(basename)

            createPage({
              path: path.join(pagePath, slug),
              component: path.resolve('./src/templates/docs-page.js'),
              context: {
                relativePath: path.join(pagePath, item.file)
              }
            })
          })
        })

      })

      resolve()
    })
  })
}

exports.setFieldsOnGraphQLNodeType = actions => {
  return menuNodeType(actions)
}

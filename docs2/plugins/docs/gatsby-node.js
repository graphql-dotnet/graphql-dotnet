import path from 'path'
import menuNodeType from './menuNodeType.js'
import sourceNodesG from './gatsby/sourceNodes.js'
import GithubSlugger from 'github-slugger'
import GraphQLJSON from 'graphql-type-json'
const slugger = new GithubSlugger()

export const sourceNodes = sourceNodesG

export const onPostBuild = ({ reporter }) => {
  reporter.info(`Your Gatsby site has been built!`)
}

export const onCreateNode = ({ node, actions, getNode }, options) => {
  if (node.internal.type !== 'MarkdownRemark') {
    return
  }

  const { createNodeField } = actions

  const markdownAbsolutePath = getNode(node.parent).absolutePath
  const docsAbsolutePath = path.parse(options.config).dir

  createNodeField({
    node,
    name: 'relativePath',
    value: path.relative(docsAbsolutePath, markdownAbsolutePath)
  })
}

export const createPages = ({ graphql, actions }, options) => {
  const { createPage } = actions
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
              component: path.resolve('./src/components/docs-page.js'),
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

// export const setFieldsOnGraphQLNodeType = actions => {
//   return menuNodeType(actions)
// }

export const createSchemaCustomization = ({actions}) => {
  const { createTypes } = actions
  createTypes(`
    type DocsMenu implements Node {
      pages: JSON
    }
  `)
}

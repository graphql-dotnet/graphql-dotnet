/**
 * Implement Gatsby's Node APIs in this file.
 *
 * See: https://www.gatsbyjs.org/docs/node-apis/
 */

// const path = require('path')
// const { createFilePath } = require('gatsby-source-filesystem')

// exports.onCreateNode = ({ node, boundActionCreators, getNode }, pluginOptions) => {
//   if (node.internal.type !== 'MarkdownRemark') {
//     return
//   }

//   const { createNodeField } = boundActionCreators
//   const slug = createFilePath({ node, getNode, basePath: 'docs' })
//   createNodeField({
//     node,
//     name: 'slug',
//     value: 'docs'+slug,
//   })
// }

// exports.createPages = ({ graphql, boundActionCreators }) => {
//   const { createPage } = boundActionCreators
//   return new Promise((resolve, reject) => {
//     graphql(`
//       {
//         allMarkdownRemark {
//           edges {
//             node {
//               fields {
//                 slug
//               }
//             }
//           }
//         }
//       }
//     `
//     ).then(result => {
//       result.data.allMarkdownRemark.edges.forEach(({ node }) => {
//         createPage({
//           path: node.fields.slug,
//           component: path.resolve('./src/templates/docs-page.js'),
//           context: {
//             // Data passed to context is available
//             // in page queries as GraphQL variables.
//             slug: node.fields.slug,
//           },
//         })
//       })
//       resolve()
//     })
//   })
// }

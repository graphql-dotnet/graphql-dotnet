/**
 * Implement Gatsby's Node APIs in this file.
 *
 * See: https://www.gatsbyjs.org/docs/node-apis/
 */
const CopyWebpackPlugin = require('copy-webpack-plugin')
const path = require('path')

module.exports = {
  onCreateWebpackConfig: ({ actions }) => {
    actions.setWebpackConfig({
      devtool: 'eval-source-map',
      plugins: [
        new CopyWebpackPlugin({
          patterns: [
            {
              from: path.resolve(
                __dirname,
                'node_modules/prismjs/themes/prism-tomorrow.min.css'
              ),
              to: 'themes/prism-tomorrow.min.css' // this will be output in /dist/themes/
            },
            {
              from: path.resolve(
                __dirname,
                'node_modules/prismjs/themes/prism-solarizedlight.min.css'
              ),
              to: 'themes/prism-solarizedlight.min.css' // this will be output in /dist/themes/
            }
          ]
        })
      ]
    })
  }
}

const path = require('path')
const ExtractTextPlugin = require('extract-text-webpack-plugin')

module.exports = [
  {
    entry: {
      'bundle': './src/GraphQL.Harness/app/app.js',
    },

    output: {
      path: path.resolve('./src/GraphQL.Harness/public'),
      filename: '[name].js'
    },

    resolve: {
      extensions: ['.js', '.json']
    },

    module: {
      loaders: [
        { test: /\.js/, loader: 'babel-loader', exclude: /node_modules/ },
        { test: /\.css$/, loader: ExtractTextPlugin.extract({
            fallback: 'style-loader',
            use: 'css-loader'
          })
        },
        { test: /\.flow/, loader: 'ignore-loader' }
      ]
    },

    plugins: [
      new ExtractTextPlugin('style.css', { allChunks: true })
    ]
  }
]

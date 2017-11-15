const path = require('path')
const ManifestPlugin = require('webpack-manifest-plugin')

module.exports = {
  entry: './src/content/main.js',
  output: {
    filename: 'bundle.js',
    path: path.resolve('./src/content')
  },
  module: {
    rules: [
      { test: /\.js$/, exclude: /node_modules/, loader: "babel-loader" }
    ]
  },
  plugins: [
    new ManifestPlugin({
      publicPath: 'content/'
    })
  ]
}

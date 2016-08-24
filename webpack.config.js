var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

var output = './src/GraphQL.GraphiQL/public';

module.exports = {
  entry: {
    'bundle': './src/GraphQL.GraphiQL/app/app.js'
  },

  output: {
    path: output,
    filename: '[name].js'
  },

  resolve: {
    extensions: ['', '.js', '.json']
  },

  module: {
    loaders: [
      { test: /\.js/, loader: 'babel', exclude: /node_modules/ },
      { test: /\.css$/, loader: ExtractTextPlugin.extract('style-loader', 'css-loader!postcss-loader') }
    ]
  },

  plugins: [
    new ExtractTextPlugin('style.css', { allChunks: true })
  ]
};

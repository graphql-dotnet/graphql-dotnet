var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = [
  {
    entry: {
      'bundle': './src/GraphQL.GraphiQL/app/app.js',
    },

    output: {
      path: './src/GraphQL.GraphiQL/public',
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
  },
  {
    entry: {
      'bundle': './src/GraphQL.GraphiQL/app/app.js',
    },

    output: {
      path: './src/GraphQL.GraphiQLCore/public',
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
  }
];

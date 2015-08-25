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
    extensions: ['', '.js', '.json'],
  },

  module: {
    loaders: [
      { test: /\.js/, loader: 'babel', exclude: /node_modules/ }
    ]
  },

};

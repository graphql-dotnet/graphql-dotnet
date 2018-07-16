const path = require('path')

module.exports = {
  siteMetadata: {
    title: 'GraphQL .NET',
    description: '',
    githubUrl: 'https://github.com/graphql-dotnet/graphql-dotnet'
  },
  plugins: [
    'gatsby-plugin-react-helmet',
    'gatsby-transformer-yaml',
    {
      resolve: 'gatsby-source-filesystem',
      options: {
        name: 'content-pages',
        path: `${__dirname}/docs`
      }
    },
    {
      resolve: 'gatsby-transformer-remark',
      options: {
        plugins: [
          {
            resolve: path.resolve('./plugins/docs'),
            options: {
              config: './src/sitemap.yml'
            }
          },
          'gatsby-remark-prismjs',
          {
            resolve: 'gatsby-remark-images',
            options: {
              maxWidth: 600
            }
          },
          'gatsby-remark-autolink-headers'
        ]
      }
    }
  ]
}

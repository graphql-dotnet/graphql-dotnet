const path = require('path')

const config = {
  config: `${__dirname}/site/sitemap.yml`,
  resources: `${__dirname}/site/docs`
}

module.exports = {
  siteMetadata: {
    title: 'GraphQL .NET',
    description: '',
    githubUrl: 'https://github.com/graphql-dotnet/graphql-dotnet'
  },
  plugins: [
    {
      resolve: path.resolve('./plugins/docs'),
      options: {
        config: config.config
      }
    },
    'gatsby-plugin-react-helmet',
    'gatsby-transformer-yaml',
    {
      resolve: 'gatsby-source-filesystem',
      options: {
        name: 'content-pages',
        path: config.resources
      }
    },
    {
      resolve: 'gatsby-transformer-remark',
      options: {
        plugins: [
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

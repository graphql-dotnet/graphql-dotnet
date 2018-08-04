const settings = {
  config: `${__dirname}/site/sitemap.yml`,
  resources: `${__dirname}/site/docs`
}

module.exports = {
  siteMetadata: {
    title: 'GraphQL .NET',
    description: 'GraphQL for .NET',
    githubUrl: 'https://github.com/graphql-dotnet/graphql-dotnet',
    githubEditUrl: 'https://github.com/graphql-dotnet/graphql-dotnet/edit/master/docs2/site',
    keywords: 'graphql,api,web api,.net,.net core'
  },
  plugins: [
    {
      // local plugin, /plugins/docs
      resolve: 'docs',
      options: {
        config: settings.config
      }
    },
    'gatsby-plugin-react-helmet',
    'gatsby-transformer-yaml',
    {
      resolve: 'gatsby-source-filesystem',
      options: {
        name: 'content-pages',
        path: settings.resources
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

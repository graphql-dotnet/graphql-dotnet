import path from 'node:path'
import { load } from 'js-yaml'
import GithubSlugger from 'github-slugger'
import sitemap from '../../site/sitemap.yml?raw'

export const metadata = {
  title: 'GraphQL .NET',
  description: 'GraphQL for .NET',
  githubUrl: 'https://github.com/graphql-dotnet/graphql-dotnet',
  githubEditUrl:
    'https://github.com/graphql-dotnet/graphql-dotnet/edit/master/docs2/site',
  keywords: 'graphql,api,web api,.net,.net core'
}

export function pageHref(url) {
  return url === '/' ? '/' : `${url.replace(/\/$/, '')}/`
}

// sitemap.yml is the ordered navigation and route manifest, not an XML sitemap.
// Use POSIX paths explicitly so Windows and Linux produce identical URLs.
const config = load(sitemap)
const slugger = new GithubSlugger()
export const docsPages = []

function sideMenu(items, urlPrefix, sourcePrefix) {
  return items.map((item) => {
    if (item.file) {
      const basename = path.posix.basename(
        item.file,
        path.posix.extname(item.file)
      )
      const url = path.posix.join(urlPrefix, slugger.slug(basename))
      const relativePath = path.posix.join(sourcePrefix, item.file)
      docsPages.push({
        url,
        id: relativePath.replace(/^docs\//, '').replace(/\.md$/, ''),
        relativePath
      })
      return { ...item, url }
    }
    const children = sideMenu(
      item.items || [],
      path.posix.join(urlPrefix, item.dir || ''),
      path.posix.join(sourcePrefix, item.dir || '')
    )
    return { ...item, items: children, url: children[0]?.url || '/' }
  })
}

export const menu = config.map((page) => {
  if (page.file) return { ...page, url: page.url || '/', sidemenu: [] }
  const prefix = `/${(page.url || page.dir).replace(/^\.\//, '').replace(/^\/+|\/+$/g, '')}`
  const sidemenu = sideMenu(
    page.sidemenu || [],
    prefix,
    page.dir.replace(/^\.\//, '')
  )
  return { ...page, sidemenu, url: sidemenu[0]?.url || '/' }
})

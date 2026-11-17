import path from 'node:path'

// Preserve the published GraphiQL image URLs as well as its responsive sizes.
// These are URL aliases; the images are rebuilt from the original source PNG.
export const graphiqlImage = {
  source: path.resolve('site/docs/getting-started/graphiql.png'),
  base: '/static/9a7bf96b9594702e8e5c17f274a936e0',
  variants: [
    { width: 150, key: '8a4e8' },
    { width: 300, key: '5a46d' },
    { width: 600, key: '0a47e' },
    { width: 900, key: '1cfc2' },
    { width: 1200, key: 'c1b63' },
    { width: 1920, key: '29114' }
  ]
}

export function imageUrl(variant) {
  return `${graphiqlImage.base}/${variant.key}/graphiql.png`
}

import { defineCollection } from 'astro:content'
import { glob } from 'astro/loaders'

export const collections = {
  docs: defineCollection({
    loader: glob({
      base: './site/docs',
      pattern: '**/*.md',
      generateId: ({ entry }) => entry.replace(/\.md$/, '')
    })
  })
}

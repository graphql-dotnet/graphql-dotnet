import { defineConfig } from 'astro/config'
import { unified } from '@astrojs/markdown-remark'
import { copyFile, mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import remarkCompatibility from './src/utils/remark-compatibility.js'

export default defineConfig({
  site: 'https://graphql-dotnet.github.io',
  output: 'static',
  trailingSlash: 'always',
  build: { format: 'directory' },
  markdown: {
    // Keep Gatsby's Prism markup and heading slugs, including headings with
    // inline code or HTML. The content remains ordinary, unmodified Markdown.
    syntaxHighlight: false,
    processor: unified({
      smartypants: false,
      remarkPlugins: [remarkCompatibility]
    })
  },
  integrations: [
    {
      name: 'preserve-404-alias',
      hooks: {
        'astro:build:done': async ({ dir }) => {
          const output = fileURLToPath(dir)
          await mkdir(`${output}/404`, { recursive: true })
          await copyFile(`${output}/404.html`, `${output}/404/index.html`)
        }
      }
    }
  ]
})

import GithubSlugger from 'github-slugger'
import Prism from 'prismjs'
import loadLanguages from 'prismjs/components/index.js'
import { graphiqlImage, imageUrl } from './legacy-images.js'

loadLanguages(['csharp', 'graphql', 'ini', 'json'])

const icon =
  '<svg aria-hidden="true" focusable="false" height="16" version="1.1" viewBox="0 0 16 16" width="16"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg>'

function visit(node, callback) {
  callback(node)
  node.children?.forEach((child) => visit(child, callback))
}

function escapeHtml(value) {
  const escapes = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  }
  return value.replace(/[&<>"']/g, (character) => escapes[character])
}

function highlight(language, value) {
  const grammar = Prism.languages[language]
  return grammar ? Prism.highlight(value, grammar, language) : escapeHtml(value)
}

// This intentionally matches mdast-util-to-string@2, used by the old Gatsby
// plugin. In particular, raw HTML contributes to the slug. Changing that would
// break existing bookmarks for headings containing named anchors or inline code.
function legacyHeadingText(node) {
  return (
    node.value ||
    node.alt ||
    node.title ||
    node.children?.map(legacyHeadingText).join('') ||
    ''
  )
}

export default function remarkCompatibility() {
  return (tree) => {
    // Gatsby highlighted inline code BEFORE generating heading IDs. Preserve
    // that order and the Solarized Light Prism classes, without browser JS.
    visit(tree, (node) => {
      if (node.type === 'code') {
        const language = (node.lang || 'text').toLowerCase()
        const className = `language-${escapeHtml(language)}`
        node.type = 'html'
        node.value = `<div class="gatsby-highlight" data-language="${escapeHtml(language)}"><pre class="${className}"><code class="${className}">${highlight(language, node.value)}</code></pre></div>`
      } else if (node.type === 'inlineCode') {
        node.type = 'html'
        node.value = `<code class="language-text">${highlight('text', node.value)}</code>`
      } else if (node.type === 'image' && node.url === 'graphiql.png') {
        const variants = graphiqlImage.variants
        const source = imageUrl(
          variants.find((variant) => variant.width === 600)
        )
        const original = imageUrl(variants.at(-1))
        const srcset = variants
          .map((variant) => `${imageUrl(variant)} ${variant.width}w`)
          .join(',\n')
        const alt = escapeHtml(node.alt || 'graphiql')
        // Retain the original image link and layout; only Gatsby's temporary
        // blurred loading placeholder is omitted.
        node.type = 'html'
        node.value = `<span class="gatsby-resp-image-wrapper" style="position:relative;display:block;margin-left:auto;margin-right:auto;max-width:600px"><a class="gatsby-resp-image-link" href="${original}" style="display:block" target="_blank" rel="noopener"><span style="padding-bottom:59.333333333333336%;display:block"></span><img class="gatsby-resp-image-image" alt="${alt}" title="" src="${source}" srcset="${srcset}" sizes="(max-width: 600px) 100vw, 600px" style="width:100%;height:100%;margin:0;vertical-align:middle;position:absolute;top:0;left:0" loading="lazy" decoding="async"></a></span>`
      }
    })

    const slugger = new GithubSlugger()
    visit(tree, (node) => {
      if (node.type !== 'heading') return
      const id = slugger.slug(legacyHeadingText(node))
      node.data = {
        ...node.data,
        hProperties: {
          ...node.data?.hProperties,
          id,
          style: 'position:relative;'
        }
      }
      node.children.unshift({
        type: 'link',
        url: `#${id}`,
        children: [],
        data: {
          hProperties: {
            'aria-label': `${id.replace(/-/g, ' ')} permalink`,
            className: ['anchor', 'before']
          },
          hChildren: [{ type: 'raw', value: icon }]
        }
      })
    })
  }
}

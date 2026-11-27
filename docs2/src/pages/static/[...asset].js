import sharp from 'sharp'
import { graphiqlImage, imageUrl } from '../../utils/legacy-images.js'

export function getStaticPaths() {
  return graphiqlImage.variants.map((variant) => ({
    params: { asset: imageUrl(variant).replace(/^\/static\//, '') },
    props: { width: variant.width }
  }))
}

export async function GET({ props }) {
  const image = await sharp(graphiqlImage.source)
    .resize({ width: props.width, withoutEnlargement: true })
    .png({ compressionLevel: 9, adaptiveFiltering: true })
    .toBuffer()
  return new Response(new Uint8Array(image), {
    headers: { 'Content-Type': 'image/png' }
  })
}

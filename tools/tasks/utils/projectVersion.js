import XmlReader from 'xml-reader'
import readFile from './readFile'

function getText(element) {
  const text = element.children.find(el => el.type === 'text') || { value: '' }
  return text.value
}

export default async function projectVersion(file) {
  const xml = await readFile(file)
  return new Promise((resolve, reject) => {
    let version = ''

    const reader = XmlReader.create()
    reader.on('tag:VersionPrefix', data => version = getText(data))
    reader.on('done', data => version !== '' ? resolve(version) : reject('Unable to read project version'))
    reader.parse(xml)
  })
}

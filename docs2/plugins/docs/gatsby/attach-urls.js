const _ = require('lodash')
const url = require('url')
const path = require('path')

const NOT_MATCHING_URL = '/'

function findFirstUrl(menuItems) {
  if (!menuItems) {
    return
  }

  return menuItems
    .reduce(
      (acc, menuItem) => acc
        ? acc
        : menuItem.url || findFirstUrl(menuItem.sidemenu || menuItem.items),
      '',
    )
}

/**
 * Generate url from file name
 * @param {String} fileName
 * @return {String} main page url
 */
function getUrlFromFileName(fileName = '') {
  return path.parse(fileName).name
}

/**
 * Generate page url
 * @param {Object} mainNav
 * @param {String} url
 * @param {String} dir
 * @param {String} file
 * @return {String} page url
 */
function getPageUrl({ url, dir, file } = {}) {
  // if there is url specified in config, then use it
  if (url) {
    return `/${_.trim(url, '/')}`
  }

  // if user specified directory, then ruse that dir name
  if (dir) {
    const fileSuffix = file ? `/${getUrlFromFileName(file)}` : ''

    // remove relative path signs
    const dirName = _.trim(dir, './')
    return `/${dirName}${fileSuffix}`
  }

  // if user specified file, then create url path from it
  if (file) {
    return `/${getUrlFromFileName(file)}`
  }

  return ''
}


/**
 * Generate urls for sidemenu items
 * @param {Array} menu
 * @param {String} menu[].file
 * @param {Array} menu[].items
 * @param {String} urlPrefix
 * @return {Object} extended sidemenu
 */
function getSidemenuWithUrl(menu = [], urlPrefix = '/') {
  if (!menu || !_.isArray(menu)) {
    return menu
  }

  return menu
    .map(menuItem => {
      // generate url for menu item
      const urlPath = _.trim(url.resolve(`${urlPrefix}/`, _.trimStart(getPageUrl(menuItem), '/')), '/')

      const items = getSidemenuWithUrl(menuItem.items, urlPath)

      return {
        ...menuItem,
        url: menuItem.file ? `/${urlPath}` : (findFirstUrl(items) || NOT_MATCHING_URL),
        items
      }
    })
}

/**
 * Generate urls for menu node
 * @param {Menu[]} navNode
 * @return {Object} extended menu node
 */
function attachUrlToNavNode(navNode = []) {
  return navNode
    .map(page => {
      const url = getPageUrl(page)

      const sidemenu = getSidemenuWithUrl(page.sidemenu, url)

      const urlPath = page.file ? url : findFirstUrl(sidemenu)

      return {
        ...page,
        url: urlPath || NOT_MATCHING_URL,
        sidemenu: sidemenu || []
      }
    })
}

exports.attachUrlToNavNode = attachUrlToNavNode

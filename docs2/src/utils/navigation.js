
/**
 * Retrieve all url's from menu
 * @param {Array} menuItems
 * @return {Array} urls
 */
function getAllUrls(menuItems) {
  if (!menuItems) {
    return []
  }

  return menuItems
    .reduce(
      (acc, menuItem) => {
        acc.push(menuItem.url)

        if (menuItem.items) {
          acc.push(...getAllUrls(menuItem.items))
        }

        return acc
      },
      []
    )
}

/**
 * Find page, that matches location pathname
 * @param {Array} menuItems
 * @param {String} pathname location pathname
 * @param {String} prefix
 * @return {Object} page config
 */
export function findMatchingPage(menuItems, pathname, prefix = '') {
  if (!menuItems) {
    return false
  }

  if (!pathname.startsWith(prefix)) {
    return false
  }

  const pathNameWithoutPrefix = pathname.substr(prefix.length)

  return menuItems.find(menuItem => {
    if (!menuItem) {
      return false
    }

    return pathNameWithoutPrefix === menuItem.url
      || getAllUrls(menuItem.sidemenu || menuItem.items).indexOf(pathNameWithoutPrefix) !== -1
  })
}

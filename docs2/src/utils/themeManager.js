import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useSyncExternalStore
} from 'react'

const THEME_KEY = 'theme'
export const THEMES = {
  LIGHT: 'LIGHT',
  DARK: 'DARK',
  SYSTEM: 'SYSTEM'
}

const ThemeManagerFactory = () => {
  /******** private members ********/

  let eventTarget = new EventTarget()
  let currentTheme = THEMES.SYSTEM
  let isInitialized = false

  function getSystemTheme() {
    return window?.matchMedia('(prefers-color-scheme: dark)').matches
      ? THEMES.DARK
      : THEMES.LIGHT
  }

  function getStoredTheme() {
    const storedTheme = localStorage?.getItem(THEME_KEY)
    return (storedTheme && THEMES[storedTheme]) || THEMES.SYSTEM
  }

  function dispatchThemeChange() {
    eventTarget.dispatchEvent(new Event(THEME_KEY))
  }

  /******** public members ********/

  function init() {
    currentTheme = getStoredTheme()
    dispatchThemeChange();

    // Listen for system theme changes
    window
      .matchMedia('(prefers-color-scheme: dark)')
      .addEventListener('change', () => {
        if (currentTheme === THEMES.SYSTEM) {
          dispatchThemeChange()
        }
      })

    isInitialized = true
  }

  function subscribe(listener) {
    const listenerWrapper = () => listener();

    eventTarget.addEventListener(THEME_KEY, listenerWrapper)
    return () => eventTarget.removeEventListener(THEME_KEY, listenerWrapper)
  }

  function setTheme(theme) {
    if (!THEMES[theme]) return

    currentTheme = theme
    dispatchThemeChange()

    isInitialized && localStorage.setItem(THEME_KEY, theme)
  }

  function getCalculatedTheme() {
    return currentTheme === THEMES.SYSTEM ? getSystemTheme() : currentTheme
  }

  function getTheme() {
    return currentTheme
  }

  return { init, subscribe, setTheme, getTheme, getCalculatedTheme }
}

const themeManager = ThemeManagerFactory()
const ThemeContext = createContext()
ThemeContext.displayName = "sriThemeContext"

export const ThemeProvider = ({ children }) => {
  const theme = useSyncExternalStore(
    themeManager.subscribe,
    themeManager.getCalculatedTheme,
    themeManager.getTheme,
  )

  const value = useMemo(() => {
    const setThemeWrapper = (newTheme) => {
      themeManager.setTheme(newTheme)
    }

    return { theme, setTheme: setThemeWrapper }
  }, [theme])

  // can only be run in browser
  useEffect(() => {
    themeManager.init()
  }, [])

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export const useTheme = () => useContext(ThemeContext) || {}

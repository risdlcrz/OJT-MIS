/** Navigate inside the single-page app without a full reload. */
export function go(path) {
  window.history.pushState({}, '', path.startsWith('/') ? path : `/${path}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

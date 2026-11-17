/*
 * App-wide navigation helper. Pages, services and layout components call
 * go() instead of touching window.history, so vue-router stays in charge of
 * the URL and its guard (login, session expiry, role access) always runs.
 *
 * The router is registered once from main.js (bindRouter) rather than
 * imported here, which keeps services/api.js free of an import cycle with
 * the router and the pages it loads.
 */

let router = null

export function bindRouter(instance) {
  router = instance
}

/**
 * Navigate inside the single-page app without a full reload.
 * Resolves once the new page is in place (or the guard redirected).
 * @param {string} path e.g. "/idgenerate.html?internId=3" or "applicants.html#schedule"
 * @param {{ replace?: boolean }} [options]
 */
export async function go(path, { replace = false } = {}) {
  const target = path.startsWith('/') ? path : `/${path}`

  if (!router) {
    window.location.assign(target)
    return
  }

  try {
    await (replace ? router.replace(target) : router.push(target))
  } catch {
    // Navigation was cancelled or redirected by the guard: nothing to do.
  }
}

/**
 * Rewrite the query/hash of the current page in place (no navigation),
 * keeping vue-router's own history state so Back/Forward keep working.
 * @param {string | URL} url
 */
export function replaceUrl(url) {
  window.history.replaceState(window.history.state, '', url)
}

/** "/coc-generate.html?internId=3#x" -> "coc-generate.html" */
export function pageOf(path) {
  return String(path || '').split(/[?#]/)[0].replace(/^\//, '').toLowerCase()
}

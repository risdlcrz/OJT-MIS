/**
 * Clock and date display that does not depend on Date parsing.
 * API timestamps can include 7 fractional digits, which Date rejects.
 */

function clockFromMatch(hours, minutes) {
  const hour = Number(hours)
  const suffix = hour >= 12 ? 'PM' : 'AM'
  const display = hour % 12 || 12
  return `${display}:${String(minutes).padStart(2, '0')} ${suffix}`
}

export function formatClock(value) {
  if (value == null || value === '') return '—'

  const text = String(value).trim()
  if (!text) return '—'

  const labeled = text.match(/^(\d{1,2}):(\d{2})(?::\d{2})?\s*([AaPp][Mm])$/)
  if (labeled) {
    let hour = Number(labeled[1])
    if (labeled[3].toLowerCase() === 'pm' && hour < 12) hour += 12
    if (labeled[3].toLowerCase() === 'am' && hour === 12) hour = 0
    return clockFromMatch(hour, labeled[2])
  }

  const wall = text.match(/(?:T|\s)(\d{2}):(\d{2})(?::\d{2})?/)
  const bare = text.match(/^(\d{1,2}):(\d{2})(?::\d{2})?$/)
  const match = wall || bare
  if (match) return clockFromMatch(match[1], match[2])

  const normalized = text.replace(/(\.\d{3})\d+/, '$1')
  const date = new Date(normalized)
  if (Number.isNaN(date.getTime())) return '—'
  return clockFromMatch(date.getHours(), date.getMinutes())
}

export function formatDisplayDate(value) {
  if (value == null || value === '') return '—'
  const text = String(value).trim().replace(/(\.\d{3})\d+/, '$1')
  const date = new Date(text.length <= 10 ? `${text.slice(0, 10)}T00:00:00` : text)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

export function dateKey(value) {
  if (!value) return ''
  const text = String(value).trim()
  const iso = text.match(/^(\d{4}-\d{2}-\d{2})/)
  if (iso) return iso[1]
  const date = new Date(text.replace(/(\.\d{3})\d+/, '$1'))
  if (Number.isNaN(date.getTime())) return ''
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

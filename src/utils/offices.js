/* Shared COA office / department list — the same source that feeds the
   Signatories module's Department dropdown (assets/json/offices.json). */
import officesData from '../../assets/json/offices.json'

const offices = (officesData.offices || []).filter((office) => office && office.name)

export const officeOptions = [...new Set(offices.map((office) => office.name))]

const normalize = (value) =>
  String(value ?? '')
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, ' ')
    .trim()

/* "ITO - Information Technology Office" -> "INFORMATION TECHNOLOGY OFFICE" */
const descriptivePart = (name) => {
  const index = name.indexOf(' - ')
  return normalize(index >= 0 ? name.slice(index + 3) : name)
}

/* Map a free-text office (an intern's applicantOffice, a signatory's
   department) onto the standardized option it refers to. Tries, in order:
   exact name, the descriptive part ("…-INFORMATION TECHNOLOGY OFFICE"),
   the office code ("ITO"), then a leading code prefix ("ITO-…").
   Returns '' when nothing matches. */
export function matchOffice(value) {
  const raw = String(value ?? '').trim()
  if (!raw) return ''

  const target = normalize(raw)

  const exact = offices.find((office) => normalize(office.name) === target)
  if (exact) return exact.name

  const byDescription = offices.find((office) => {
    const description = descriptivePart(office.name)
    return description && (target === description || target.endsWith(` ${description}`))
  })
  if (byDescription) return byDescription.name

  const byCode = offices.find((office) => office.code && normalize(office.code) === target)
  if (byCode) return byCode.name

  const prefix = normalize(raw.split(/[-–:]/)[0])
  const byPrefix = offices.find((office) => office.code && normalize(office.code) === prefix)
  if (byPrefix) return byPrefix.name

  return ''
}

/* Standardized option when one matches, otherwise the original text, so an
   office missing from the list still shows (as an extra dropdown option). */
export function resolveOffice(value) {
  return matchOffice(value) || String(value ?? '').trim()
}

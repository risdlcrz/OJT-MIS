/*
 * Upload limits shared with the API (OJTMISApi appsettings.json "Uploads",
 * served by GET /api/uploads/policy). The file pickers use the same rules to
 * pre-check a file and to show the hint under the button, so the browser and
 * the server never disagree.
 *
 *   avatar      - profile picture (Account page); sent to the API and re-checked there
 *   idPhoto     - ID generator photo; stays in the browser (printed only)
 *   idSignature - ID generator signature; stays in the browser (printed only)
 */
import { uploadsAPI } from '@/services/api'

const MB = 1024 * 1024
const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']

/* Used until the API answers (or if it cannot be reached). Keep in step with appsettings.json. */
export const DEFAULT_UPLOAD_POLICY = Object.freeze({
  avatar: { maxBytes: 5 * MB, maxFiles: 1, allowedContentTypes: IMAGE_TYPES, allowedExtensions: IMAGE_EXTENSIONS },
  idPhoto: { maxBytes: 5 * MB, maxFiles: 1, allowedContentTypes: IMAGE_TYPES, allowedExtensions: IMAGE_EXTENSIONS },
  idSignature: {
    maxBytes: 2 * MB,
    maxFiles: 1,
    allowedContentTypes: ['image/png', 'image/jpeg'],
    allowedExtensions: ['.png', '.jpg', '.jpeg']
  }
})

let policyPromise = null

/** Loads the policy once per page load. Falls back to the defaults on any failure. */
export function loadUploadPolicy() {
  if (!policyPromise) {
    policyPromise = uploadsAPI
      .policy()
      .then((policy) => ({ ...DEFAULT_UPLOAD_POLICY, ...(policy || {}) }))
      .catch(() => {
        policyPromise = null
        return DEFAULT_UPLOAD_POLICY
      })
  }
  return policyPromise
}

export function formatBytes(bytes) {
  if (bytes >= MB) {
    const value = bytes / MB
    return `${Number.isInteger(value) ? value : value.toFixed(1)} MB`
  }
  return `${Math.max(1, Math.round(bytes / 1024))} KB`
}

function typeNames(rule) {
  const names = [
    ...new Set((rule.allowedExtensions || []).map((ext) => ext.replace('.', '').toUpperCase()).map((n) => (n === 'JPEG' ? 'JPG' : n)))
  ]
  if (names.length <= 1) return names.join('')
  return `${names.slice(0, -1).join(', ')} or ${names[names.length - 1]}`
}

/** "JPG, PNG or WEBP, up to 5 MB" (plus the file count when more than one is allowed). */
export function describeUploadRule(rule) {
  if (!rule) return ''
  const count = rule.maxFiles > 1 ? `, up to ${rule.maxFiles} files` : ''
  return `${typeNames(rule)}, up to ${formatBytes(rule.maxBytes)}${count}`
}

/** Value for an <input type="file" accept="...">. */
export function acceptAttribute(rule) {
  return [...(rule?.allowedExtensions || []), ...(rule?.allowedContentTypes || [])].join(',')
}

/* The first bytes of each allowed type, so a renamed file is caught before upload. */
const SIGNATURES = {
  'image/jpeg': (b) => b.length >= 3 && b[0] === 0xff && b[1] === 0xd8 && b[2] === 0xff,
  'image/png': (b) =>
    b.length >= 8 && b[0] === 0x89 && b[1] === 0x50 && b[2] === 0x4e && b[3] === 0x47 &&
    b[4] === 0x0d && b[5] === 0x0a && b[6] === 0x1a && b[7] === 0x0a,
  'image/webp': (b) =>
    b.length >= 12 && b[0] === 0x52 && b[1] === 0x49 && b[2] === 0x46 && b[3] === 0x46 &&
    b[8] === 0x57 && b[9] === 0x45 && b[10] === 0x42 && b[11] === 0x50
}

async function sniffType(file) {
  const bytes = new Uint8Array(await file.slice(0, 16).arrayBuffer())
  return Object.keys(SIGNATURES).find((type) => SIGNATURES[type](bytes)) || ''
}

/**
 * Checks the picked files against a rule: count, extension, declared MIME type,
 * size, and the file's real leading bytes.
 * @param {FileList|File[]} files
 * @returns {Promise<string>} a short message for the user, or '' when the files are fine
 */
export async function validateUploadFiles(files, rule) {
  const list = Array.from(files || [])
  const hint = describeUploadRule(rule)

  if (list.length === 0) return 'No file selected.'
  if (list.length > rule.maxFiles) {
    return rule.maxFiles === 1 ? 'Choose only one file.' : `Choose up to ${rule.maxFiles} files.`
  }

  for (const file of list) {
    const name = (file.name || '').toLowerCase()
    const extension = name.includes('.') ? name.slice(name.lastIndexOf('.')) : ''
    const type = (file.type || '').toLowerCase()

    if (!rule.allowedExtensions.includes(extension) || !rule.allowedContentTypes.includes(type)) {
      return `This file type is not allowed. Use ${hint}.`
    }
    if (file.size === 0) return 'The file is empty. Choose a different file.'
    if (file.size > rule.maxBytes) return `The file is too large. The limit is ${formatBytes(rule.maxBytes)}.`

    let actual
    try {
      actual = await sniffType(file)
    } catch {
      return 'The file could not be read. Choose a different file.'
    }
    if (!actual || !rule.allowedContentTypes.includes(actual)) {
      return `The file is not a valid image. Use ${hint}.`
    }
  }

  return ''
}

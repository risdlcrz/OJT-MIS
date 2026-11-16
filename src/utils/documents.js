import { computed, ref } from 'vue'
import { hrAttendanceAPI } from '@/services/api'

/** Document types as stored in the GeneratedDocuments table. */
export const DOC_TYPES = {
  ID: 'ID',
  COC: 'Certificate of Completion'
}

/** "Nov 16, 2026" from the issuance date, falling back to when it was recorded. */
export function issuedOn(doc) {
  const value = doc?.dateIssued || doc?.createdAt
  const date = value ? new Date(value) : null
  return date && !Number.isNaN(date.getTime())
    ? date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : ''
}

/**
 * Permanent issuance history for one document type. Powers the
 * "Already generated / Regenerate" labels in the COC and ID selectors.
 *
 * @param {string} type One of DOC_TYPES.
 * @param {import('vue').Ref} selectedInternId The intern picked in the generator.
 */
export function useIssuanceHistory(type, selectedInternId) {
  const documents = ref([])
  const loading = ref(false)

  /* Latest issuance per intern id. */
  const latestByIntern = computed(() => {
    const latest = new Map()
    for (const doc of documents.value) {
      if (doc.documentType !== type) continue
      const internId = Number(doc.internId)
      if (!Number.isFinite(internId) || internId <= 0) continue
      const current = latest.get(internId)
      if (!current || new Date(doc.createdAt) >= new Date(current.createdAt)) latest.set(internId, doc)
    }
    return latest
  })

  const historyFor = (intern) => latestByIntern.value.get(Number(intern?.id)) || null
  const selected = computed(() => latestByIntern.value.get(Number(selectedInternId.value)) || null)

  async function load() {
    if (loading.value) return
    loading.value = true
    try {
      const data = await hrAttendanceAPI.getGeneratedDocuments()
      documents.value = Array.isArray(data) ? data : []
    } catch {
      documents.value = []
    } finally {
      loading.value = false
    }
  }

  return { documents, loading, historyFor, selected, load }
}

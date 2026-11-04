<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header">
        <h3 class="card-title mb-0">Daily Time Record</h3>
      </div>
      <div class="card-body">
        <p class="text-muted mb-2">Scan the QR code on your intern ID to time in or time out.</p>
        <div v-if="message" class="alert" :class="ok ? 'alert-success' : 'alert-danger'">{{ message }}</div>
        <div v-if="today" class="mb-3">
          <div>AM Time In / Out: {{ shiftLabel(today.morning) }}</div>
          <div>PM Time In / Out: {{ shiftLabel(today.afternoon) }}</div>
          <div class="text-muted small">{{ today.hours || 0 }} hours · {{ today.status || 'On Time' }}</div>
        </div>
        <div id="intern-qr-reader" class="intern-qr-reader rounded mb-3"></div>
        <div class="d-flex flex-wrap gap-2">
          <button type="button" class="btn btn-primary" :disabled="busy" @click="startCamera">
            {{ scanning ? 'Scanning…' : 'Open camera' }}
          </button>
          <button type="button" class="btn btn-outline-secondary" :disabled="busy" @click="stopCamera">Stop</button>
          <label class="btn btn-outline-secondary mb-0">
            Scan QR image
            <input type="file" accept="image/*" class="d-none" @change="fromFile" />
          </label>
        </div>
        <p v-if="cameraHint" class="text-muted small mt-2 mb-0">{{ cameraHint }}</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { Html5Qrcode } from 'html5-qrcode'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const readerId = 'intern-qr-reader'
const message = ref('')
const ok = ref(false)
const busy = ref(false)
const scanning = ref(false)
const cameraHint = ref('')
const today = ref(null)
let scanner = null
let lastPayload = ''

function formatTime(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : '—'
}

function shiftLabel(shift) {
  if (!shift?.timeIn) return '—'
  return `${formatTime(shift.timeIn)} – ${shift.timeOut ? formatTime(shift.timeOut) : 'open'}`
}

async function loadToday() {
  try {
    const data = await internPortalAPI.getPortal()
    today.value = data.today
  } catch {
    today.value = null
  }
}

async function submit(payload) {
  if (!payload || payload === lastPayload || busy.value) return
  lastPayload = payload
  busy.value = true
  try {
    const result = await internPortalAPI.logTime(payload)
    ok.value = true
    message.value = `${result.punch || result.shift || 'Punch'} recorded.`
    today.value = result.today || today.value
    window.dispatchEvent(new CustomEvent('ojt:dtr-updated', { detail: result }))
    await stopCamera()
  } catch (e) {
    ok.value = false
    message.value = getApiErrorMessage(e, 'Could not record DTR.')
  } finally {
    busy.value = false
    setTimeout(() => { lastPayload = '' }, 2500)
  }
}

function ensureScanner() {
  if (!scanner) scanner = new Html5Qrcode(readerId, { verbose: false })
  return scanner
}

async function startCamera() {
  message.value = ''
  cameraHint.value = ''
  const qr = ensureScanner()
  if (qr.isScanning) return
  try {
    await qr.start(
      { facingMode: 'environment' },
      { fps: 10, qrbox: { width: 240, height: 240 } },
      (decoded) => { submit(decoded) },
      () => {}
    )
    scanning.value = true
  } catch {
    scanning.value = false
    cameraHint.value = 'Camera permission was denied or no camera is available. You can scan a QR image instead.'
  }
}

async function stopCamera() {
  scanning.value = false
  if (!scanner) return
  try {
    if (scanner.isScanning) await scanner.stop()
    scanner.clear()
  } catch {
    /* already stopped */
  }
  scanner = null
}

async function fromFile(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  await stopCamera()
  const qr = ensureScanner()
  try {
    const text = await qr.scanFile(file, false)
    await submit(text)
  } catch {
    ok.value = false
    message.value = 'No QR code found on that image.'
  } finally {
    try { qr.clear() } catch { /* ignore */ }
    scanner = null
  }
}

onMounted(loadToday)
onBeforeUnmount(stopCamera)
</script>

<style scoped>
.intern-qr-reader {
  width: 100%;
  max-width: 420px;
  min-height: 240px;
  background: #111;
}
</style>

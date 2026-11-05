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
        <qrcode-drop-zone class="intern-qr-drop d-block mb-3" @detect="onDetect" @error="onDropError">
          <div class="intern-qr-reader rounded">
            <qrcode-stream
              v-if="scanning"
              :paused="busy"
              :constraints="cameraConstraints"
              @detect="onDetect"
              @error="onCameraError"
            />
            <p v-else class="intern-qr-placeholder mb-0">
              Camera is off. Open the camera or drop a QR image here.
            </p>
          </div>
        </qrcode-drop-zone>
        <div class="d-flex flex-wrap gap-2">
          <button type="button" class="btn btn-primary" :disabled="busy || scanning" @click="startCamera">
            {{ scanning ? 'Scanning…' : 'Open camera' }}
          </button>
          <button type="button" class="btn btn-outline-secondary" :disabled="busy || !scanning" @click="stopCamera">
            Stop
          </button>
          <label class="btn btn-outline-secondary mb-0">
            Scan QR image
            <qrcode-capture class="visually-hidden" @detect="onDetect" />
          </label>
        </div>
        <p v-if="cameraHint" class="text-muted small mt-2 mb-0">{{ cameraHint }}</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { QrcodeCapture, QrcodeDropZone, QrcodeStream } from 'vue-qrcode-reader'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const message = ref('')
const ok = ref(false)
const busy = ref(false)
const scanning = ref(false)
const cameraHint = ref('')
const today = ref(null)
const cameraConstraints = { facingMode: 'environment' }
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

function payloadFromDetect(detectedCodes) {
  const first = Array.isArray(detectedCodes) ? detectedCodes[0] : detectedCodes
  return first?.rawValue || first?.raw || (typeof first === 'string' ? first : '')
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
    stopCamera()
  } catch (e) {
    ok.value = false
    message.value = getApiErrorMessage(e, 'Could not record DTR.')
  } finally {
    busy.value = false
    setTimeout(() => { lastPayload = '' }, 2500)
  }
}

function onDetect(detectedCodes) {
  const payload = payloadFromDetect(detectedCodes)
  if (payload) submit(payload)
}

function onCameraError() {
  scanning.value = false
  cameraHint.value = 'Camera permission was denied or no camera is available. You can scan a QR image instead.'
}

function onDropError() {
  ok.value = false
  message.value = 'No QR code found on that image.'
}

function startCamera() {
  message.value = ''
  cameraHint.value = ''
  scanning.value = true
}

function stopCamera() {
  scanning.value = false
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
  overflow: hidden;
}

.intern-qr-placeholder {
  color: #bbb;
  padding: 5rem 1rem;
  text-align: center;
}

.intern-qr-drop :deep(input) {
  display: none;
}
</style>

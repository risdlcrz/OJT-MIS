<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header">
        <h3 class="card-title mb-0">Daily Time Record</h3>
      </div>
      <div class="card-body">
        <p class="text-muted">Point your camera at your intern ID QR code to time in or time out.</p>
        <div v-if="message" class="alert" :class="ok ? 'alert-success' : 'alert-danger'">{{ message }}</div>
        <div v-if="today" class="mb-3">
          Today: {{ formatTime(today.timeIn) }} – {{ today.timeOut ? formatTime(today.timeOut) : 'still in' }}
          <span class="badge bg-secondary ms-1">{{ today.status }}</span>
        </div>
        <video ref="videoEl" class="w-100 rounded bg-dark mb-3" style="max-height: 360px; object-fit: cover;" playsinline muted autoplay></video>
        <div class="d-flex flex-wrap gap-2">
          <button type="button" class="btn btn-primary" :disabled="busy" @click="startCamera">
            {{ scanning ? 'Scanning…' : 'Open camera' }}
          </button>
          <button type="button" class="btn btn-outline-secondary" :disabled="busy" @click="stopCamera">Stop</button>
          <label class="btn btn-outline-secondary mb-0">
            Upload ID photo
            <input type="file" accept="image/*" capture="environment" class="d-none" @change="fromFile" />
          </label>
        </div>
        <p v-if="cameraHint" class="text-muted small mt-2 mb-0">{{ cameraHint }}</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const videoEl = ref(null)
const message = ref('')
const ok = ref(false)
const busy = ref(false)
const scanning = ref(false)
const cameraHint = ref('')
const today = ref(null)
let stream = null
let timer = null
let lastPayload = ''

function formatTime(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : '—'
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
    const result = await internPortalAPI.scanDtr(payload)
    ok.value = true
    message.value = result.action === 'in' ? 'Timed in successfully.' : 'Timed out successfully.'
    today.value = result.record
    stopCamera()
  } catch (e) {
    ok.value = false
    message.value = getApiErrorMessage(e, 'Could not record DTR.')
  } finally {
    busy.value = false
    setTimeout(() => { lastPayload = '' }, 2500)
  }
}

async function detectFrame() {
  const video = videoEl.value
  if (!video || !scanning.value || !('BarcodeDetector' in window)) return
  try {
    const detector = new window.BarcodeDetector({ formats: ['qr_code'] })
    const codes = await detector.detect(video)
    if (codes[0]?.rawValue) await submit(codes[0].rawValue)
  } catch {
    cameraHint.value = 'Could not read a QR code from this camera frame.'
  }
}

async function startCamera() {
  message.value = ''
  cameraHint.value = ''
  if (!navigator.mediaDevices?.getUserMedia) {
    cameraHint.value = 'Camera is not available. Use Upload ID photo instead.'
    return
  }
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: { ideal: 'environment' } },
      audio: false
    })
    videoEl.value.srcObject = stream
    await videoEl.value.play()
    scanning.value = true
    if (!('BarcodeDetector' in window)) {
      cameraHint.value = 'This browser cannot decode QR from live video. Capture a photo of your ID instead.'
      return
    }
    timer = window.setInterval(detectFrame, 700)
  } catch {
    cameraHint.value = 'Camera permission was denied. Allow camera access or upload a photo of your ID.'
  }
}

function stopCamera() {
  scanning.value = false
  if (timer) {
    clearInterval(timer)
    timer = null
  }
  stream?.getTracks().forEach((t) => t.stop())
  stream = null
  if (videoEl.value) videoEl.value.srcObject = null
}

async function fromFile(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  if (!('BarcodeDetector' in window)) {
    ok.value = false
    message.value = 'This browser cannot scan QR codes from photos. Use Chrome or Edge.'
    return
  }
  const bitmap = await createImageBitmap(file)
  const detector = new window.BarcodeDetector({ formats: ['qr_code'] })
  const codes = await detector.detect(bitmap)
  if (!codes[0]?.rawValue) {
    ok.value = false
    message.value = 'No QR code found on that image.'
    return
  }
  await submit(codes[0].rawValue)
}

onMounted(async () => {
  await loadToday()
  await startCamera()
})
onBeforeUnmount(stopCamera)
</script>

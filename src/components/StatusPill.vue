<template>
  <span class="ojt-pill" :class="`ojt-pill--${resolvedTone}`">{{ text }}</span>
</template>

<script setup>
import { computed } from 'vue'

/* Status -> tone. Shared by every table so the same status always reads
   the same color. Unknown statuses fall back to neutral. */
const TONES = {
  approved: 'success',
  active: 'success',
  hired: 'success',
  open: 'success',
  confirmed: 'success',
  'on time': 'success',
  pending: 'warning',
  late: 'warning',
  invited: 'warning',
  'awaiting confirmation': 'warning',
  rejected: 'danger',
  terminated: 'danger',
  closed: 'neutral',
  full: 'neutral',
  unlinked: 'neutral',
  completed: 'info',
  accepted: 'info',
  scheduled: 'info',
  incoming: 'brand'
}

/* Stored statuses that read better under another name. */
const LABELS = {
  Hired: 'Active'
}

const props = defineProps({
  status: { type: String, default: '' },
  /* Override the computed label. */
  label: { type: String, default: '' },
  /* Override the computed tone: success | warning | danger | info | brand | neutral */
  tone: { type: String, default: '' }
})

const text = computed(() => props.label || LABELS[props.status] || props.status || 'Unknown')
const resolvedTone = computed(() =>
  props.tone || TONES[String(props.status || '').trim().toLowerCase()] || 'neutral')
</script>

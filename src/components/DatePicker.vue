<template>
  <div class="date-picker">
    <div class="date-picker-input-wrapper" @click="open">
      <input
        type="text"
        :value="formattedValue"
        @click="open"
        @focus="open"
        @keydown.esc="close"
        readonly
        class="form-control"
        placeholder="Select date"
      />
      <i class="fas fa-calendar-alt date-picker-icon" @click.stop="open"></i>
    </div>

    <Teleport to="body">
      <div v-if="isOpen" class="date-picker-dropdown" :style="dropdownStyle" @click.stop>
        <div class="date-picker-header">
          <button type="button" class="nav-btn" @click="prevYear" title="Previous year">&#171;</button>
          <button type="button" class="nav-btn" @click="prevMonth" title="Previous month">&#8249;</button>
          <span class="month-year">{{ monthLabel }} {{ currentYear }}</span>
          <button type="button" class="nav-btn" @click="nextMonth" title="Next month">&#8250;</button>
          <button type="button" class="nav-btn" @click="nextYear" title="Next year">&#187;</button>
        </div>
        <div class="date-picker-weekdays">
          <span v-for="d in weekdays" :key="d">{{ d }}</span>
        </div>
        <div class="date-picker-grid">
          <button
            v-for="day in calendarDays"
            :key="day.date"
            type="button"
            class="date-btn"
            :class="{
              'other-month': !day.inMonth,
              'selected': day.isSelected,
              'today': day.isToday,
              'disabled': day.disabled
            }"
            :disabled="day.disabled"
            @click="selectDay(day)"
          >
            {{ day.date.getDate() }}
          </button>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'

const props = defineProps({
  modelValue: { type: [String, Date, null], default: null },
  disabled: { type: Boolean, default: false },
  minDate: { type: [String, Date], default: null },
  maxDate: { type: [String, Date], default: null }
})

const emit = defineEmits(['update:modelValue'])

const isOpen = ref(false)
const currentMonth = ref(new Date().getMonth())
const currentYear = ref(new Date().getFullYear())

const weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

const parsedValue = computed(() => {
  if (!props.modelValue) return null
  const d = new Date(props.modelValue)
  return isNaN(d.getTime()) ? null : d
})

const formattedValue = computed(() => {
  if (!parsedValue.value) return ''
  return parsedValue.value.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: '2-digit' })
})

const monthLabel = computed(() => {
  return new Date(currentYear.value, currentMonth.value).toLocaleString(undefined, { month: 'long' })
})

const minDate = computed(() => {
  if (!props.minDate) return null
  const d = new Date(props.minDate)
  d.setHours(0,0,0,0)
  return d
})

const maxDate = computed(() => {
  if (!props.maxDate) return null
  const d = new Date(props.maxDate)
  d.setHours(0,0,0,0)
  return d
})

const calendarDays = computed(() => {
  const year = currentYear.value
  const month = currentMonth.value
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const startDay = firstDay.getDay()
  const daysInMonth = lastDay.getDate()
  const daysInPrevMonth = new Date(year, month, 0).getDate()

  const today = new Date()
  today.setHours(0,0,0,0)

  const days = []

  for (let i = startDay - 1; i >= 0; i--) {
    const date = new Date(year, month - 1, daysInPrevMonth - i)
    days.push({ date, inMonth: false, isSelected: false, isToday: false, disabled: isDisabled(date) })
  }

  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month, d)
    days.push({
      date,
      inMonth: true,
      isSelected: parsedValue.value && date.getTime() === parsedValue.value.getTime(),
      isToday: date.getTime() === today.getTime(),
      disabled: isDisabled(date)
    })
  }

  const remaining = (7 - (days.length % 7)) % 7
  for (let d = 1; d <= remaining; d++) {
    const date = new Date(year, month + 1, d)
    days.push({ date, inMonth: false, isSelected: false, isToday: false, disabled: isDisabled(date) })
  }

  return days
})

function isDisabled(date) {
  if (minDate.value && date < minDate.value) return true
  if (maxDate.value && date > maxDate.value) return true
  return false
}

function selectDay(day) {
  if (day.disabled) return
  const newDate = new Date(day.date)
  newDate.setHours(12, 0, 0, 0)
  emit('update:modelValue', newDate.toISOString().split('T')[0])
  close()
}

function prevMonth() { currentMonth.value = (currentMonth.value - 1 + 12) % 12; if (currentMonth.value === 11) currentYear.value-- }
function nextMonth() { currentMonth.value = (currentMonth.value + 1) % 12; if (currentMonth.value === 0) currentYear.value++ }
function prevYear() { currentYear.value-- }
function nextYear() { currentYear.value++ }

function handleOutsideClick(e) {
  if (!isOpen.value) return
  const wrapper = document.querySelector('.date-picker-input-wrapper')
  const dropdown = document.querySelector('.date-picker-dropdown')
  if (wrapper && !wrapper.contains(e.target) && dropdown && !dropdown.contains(e.target)) {
    close()
  }
}

function open() {
  if (props.disabled) return
  if (parsedValue.value) {
    currentMonth.value = parsedValue.value.getMonth()
    currentYear.value = parsedValue.value.getFullYear()
  }
  isOpen.value = true
  nextTick(() => positionDropdown())
  document.addEventListener('click', handleOutsideClick)
}

function close() {
  isOpen.value = false
  document.removeEventListener('click', handleOutsideClick)
}

function positionDropdown() {
  const wrapper = document.querySelector('.date-picker-input-wrapper')
  const dropdown = document.querySelector('.date-picker-dropdown')
  if (!wrapper || !dropdown) return
  const rect = wrapper.getBoundingClientRect()
  const dropdownHeight = dropdown.offsetHeight || 320
  const spaceBelow = window.innerHeight - rect.bottom
  const spaceAbove = rect.top

  if (spaceBelow < dropdownHeight && spaceAbove >= dropdownHeight) {
    dropdown.style.top = `${rect.top + window.scrollY - dropdownHeight - 4}px`
    dropdown.style.left = `${rect.left + window.scrollX}px`
    dropdown.style.minWidth = `${rect.width}px`
    dropdown.classList.add('opens-up')
  } else {
    dropdown.style.top = `${rect.bottom + window.scrollY + 4}px`
    dropdown.style.left = `${rect.left + window.scrollX}px`
    dropdown.style.minWidth = `${rect.width}px`
    dropdown.classList.remove('opens-up')
  }
}

const dropdownStyle = computed(() => {
  if (!isOpen.value) return { display: 'none' }
  return { position: 'absolute', zIndex: 2147483647, display: 'block' }
})

onMounted(() => {
  if (parsedValue.value) {
    currentMonth.value = parsedValue.value.getMonth()
    currentYear.value = parsedValue.value.getFullYear()
  }
})

onBeforeUnmount(() => document.removeEventListener('click', handleOutsideClick))
</script>

<style scoped>
.date-picker-input-wrapper { position: relative; }
.date-picker-icon { position: absolute; right: 0.75rem; top: 50%; transform: translateY(-50%); color: #6c757d; cursor: pointer; }
.date-picker-dropdown {
  background: #fff;
  border: 1px solid #dee2e6;
  border-radius: 0.5rem;
  box-shadow: 0 0.5rem 1rem rgba(0,0,0,0.15);
  padding: 0.75rem;
  min-width: 260px;
  font-family: inherit;
  position: absolute;
  z-index: 2147483647;
}
.date-picker-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.5rem; }
.nav-btn { background: none; border: none; font-size: 1rem; color: #f97316; cursor: pointer; padding: 0.25rem 0.5rem; border-radius: 0.25rem; transition: background 0.15s; }
.nav-btn:hover:not(:disabled) { background: #fff7ed; }
.nav-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.month-year { font-weight: 600; color: #1a1a2e; }
.date-picker-weekdays { display: grid; grid-template-columns: repeat(7, 1fr); text-align: center; font-size: 0.75rem; font-weight: 600; color: #6c757d; text-transform: uppercase; margin-bottom: 0.25rem; }
.date-picker-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 2px; }
.date-btn {
  aspect-ratio: 1; border: none; background: transparent; border-radius: 0.375rem;
  font-size: 0.875rem; color: #212529; cursor: pointer; transition: all 0.1s;
  display: flex; align-items: center; justify-content: center;
}
.date-btn:hover:not(.disabled):not(.selected) { background: #fff7ed; color: #f97316; }
.date-btn.today { font-weight: 600; color: #f97316; }
.date-btn.selected { background: #f97316; color: #fff; }
.date-btn.selected:hover { background: #ea580c; }
.date-btn.other-month { color: #adb5bd; }
.date-btn.disabled { opacity: 0.4; cursor: not-allowed; }
</style>
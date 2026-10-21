<template>
  <div class="date-picker">
    <div ref="wrapper" class="date-picker-input-wrapper" @click="open">
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
      <div ref="dropdown" v-if="isOpen" class="date-picker-dropdown" :style="dropdownStyle" @click.stop>
        <div class="date-picker-header">
          <button type="button" class="nav-btn" @click="navPrevStep" :title="navTitles.stepPrev">&#171;</button>
          <button type="button" class="nav-btn" @click="navPrevSmall" :title="navTitles.smallPrev">&#8249;</button>
          <button type="button" class="month-year" @click="toggleHeaderView" :title="`${headerLabel} — click to change`">
            {{ headerLabel }}
          </button>
          <button type="button" class="nav-btn" @click="navNextSmall" :title="navTitles.smallNext">&#8250;</button>
          <button type="button" class="nav-btn" @click="navNextStep" :title="navTitles.stepNext">&#187;</button>
        </div>

        <!-- Day grid -->
        <template v-if="viewMode === 'days'">
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
        </template>

        <!-- Quick month selector -->
        <div v-else-if="viewMode === 'months'" class="date-picker-panel-grid">
          <button
            v-for="(label, index) in monthOptions"
            :key="label"
            type="button"
            class="panel-btn"
            :class="{ active: index === currentMonth }"
            @click="selectMonth(index)"
          >
            {{ label }}
          </button>
        </div>

        <!-- Quick year selector -->
        <div v-else class="date-picker-panel-grid">
          <button
            v-for="year in yearOptions"
            :key="year"
            type="button"
            class="panel-btn"
            :class="{ active: year === currentYear }"
            @click="selectYear(year)"
          >
            {{ year }}
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
const wrapper = ref(null)
const dropdown = ref(null)

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

/* Compact popover view state: header click cycles days -> months -> years. */
const viewMode = ref('days')
const monthOptions = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

const yearPageStart = computed(() => Math.floor(currentYear.value / 12) * 12)
const yearOptions = computed(() => Array.from({ length: 12 }, (_, i) => yearPageStart.value + i))

const headerLabel = computed(() => {
  if (viewMode.value === 'days') return `${monthLabel.value} ${currentYear.value}`
  if (viewMode.value === 'months') return `${currentYear.value}`
  return `${yearPageStart.value} - ${yearPageStart.value + 11}`
})

const navTitles = computed(() => {
  if (viewMode.value === 'days') {
    return { stepPrev: 'Previous year', smallPrev: 'Previous month', smallNext: 'Next month', stepNext: 'Next year' }
  }
  if (viewMode.value === 'months') {
    return { stepPrev: 'Previous 12 years', smallPrev: 'Previous year', smallNext: 'Next year', stepNext: 'Next 12 years' }
  }
  return { stepPrev: 'Previous 12 years', smallPrev: 'Previous 12 years', smallNext: 'Next 12 years', stepNext: 'Next 12 years' }
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

/* Header click cycles the quick selector: days -> months -> years. */
function toggleHeaderView() {
  if (viewMode.value === 'days') viewMode.value = 'months'
  else if (viewMode.value === 'months') viewMode.value = 'years'
  else viewMode.value = 'days'
}

function selectMonth(index) {
  currentMonth.value = index
  viewMode.value = 'days'
}

function selectYear(year) {
  currentYear.value = year
  viewMode.value = 'months'
}

/* '<' and '>': month-by-month in the day grid, year-by-year in the panels. */
function navPrevSmall() {
  if (viewMode.value === 'days') return prevMonth()
  if (viewMode.value === 'months') { currentYear.value -= 1; return }
  currentYear.value -= 12
}

function navNextSmall() {
  if (viewMode.value === 'days') return nextMonth()
  if (viewMode.value === 'months') { currentYear.value += 1; return }
  currentYear.value += 12
}

/* '<<' and '>>': year-by-year in the day grid, 12-year jumps in the panels. */
function navPrevStep() {
  if (viewMode.value === 'days') return prevYear()
  currentYear.value -= 12
}

function navNextStep() {
  if (viewMode.value === 'days') return nextYear()
  currentYear.value += 12
}

function handleOutsideClick(e) {
  if (!isOpen.value) return
  const w = wrapper.value
  const d = dropdown.value
  if (w && !w.contains(e.target) && d && !d.contains(e.target)) {
    close()
  }
}

function open() {
  if (props.disabled) return
  if (parsedValue.value) {
    currentMonth.value = parsedValue.value.getMonth()
    currentYear.value = parsedValue.value.getFullYear()
  }
  viewMode.value = 'days'
  isOpen.value = true
  nextTick(() => positionDropdown())
  document.addEventListener('click', handleOutsideClick)
}

function close() {
  isOpen.value = false
  document.removeEventListener('click', handleOutsideClick)
}

function positionDropdown() {
  const w = wrapper.value
  const d = dropdown.value
  if (!w || !d) return
  // Always render the popover above the input so it isn't trapped inside the modal.
  const rect = w.getBoundingClientRect()
  d.style.width = `${rect.width}px`
  d.style.left = `${rect.left + rect.width / 2}px`
  d.style.bottom = `${rect.bottom + window.scrollY + 4}px`
  d.style.transform = `translateX(-50%) translateY(-100%)`
  d.style.top = 'auto'
  d.classList.add('opens-up')
  d.classList.remove('opens-down')
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
  border: 1px solid #e5e7eb;
  border-radius: 0.75rem;
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.15), 0 8px 10px -6px rgba(15, 23, 42, 0.1);
  padding: 0.75rem;
  min-width: 276px;
  font-family: inherit;
  position: absolute;
  z-index: 9999;
  max-height: 280px;
  overflow-y: auto;
}
.date-picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.125rem;
  margin-bottom: 0.5rem;
}
.nav-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.75rem;
  height: 1.75rem;
  background: none;
  border: none;
  font-size: 1rem;
  line-height: 1;
  color: #ff8c1a;
  cursor: pointer;
  border-radius: 0.5rem;
  transition: background 0.15s, color 0.15s;
}
.nav-btn:hover:not(:disabled) { background: #fff3e5; }
.nav-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.month-year {
  flex: 1;
  background: none;
  border: none;
  font-weight: 600;
  font-size: 0.9rem;
  color: #1E0F0C;
  cursor: pointer;
  padding: 0.25rem 0.5rem;
  border-radius: 0.5rem;
  transition: background 0.15s, color 0.15s;
}
.month-year:hover { background: #fff3e5; color: #ff8c1a; }
.date-picker-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  text-align: center;
  font-size: 0.6875rem;
  font-weight: 600;
  color: #94a3b8;
  text-transform: uppercase;
  margin-bottom: 0.25rem;
}
.date-picker-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 2px; }
.date-btn {
  aspect-ratio: 1; border: none; background: transparent; border-radius: 0.5rem;
  font-size: 0.8125rem; color: #1E0F0C; cursor: pointer; transition: background 0.12s, color 0.12s;
  display: flex; align-items: center; justify-content: center;
}
.date-btn:hover:not(.disabled):not(.selected) { background: #fff3e5; color: #ff8c1a; }
.date-btn.today { font-weight: 700; color: #ff8c1a; }
.date-btn.selected { background: #ff8c1a; color: #fff; font-weight: 600; }
.date-btn.selected:hover { background: #e07712; }
.date-btn.other-month { color: #cbd5e1; }
.date-btn.disabled { opacity: 0.35; cursor: not-allowed; }

/* Quick month / year selector panels */
.date-picker-panel-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0.375rem;
  padding: 0.125rem;
}
.panel-btn {
  border: none;
  background: #f8fafc;
  border-radius: 0.5rem;
  padding: 0.5rem 0.25rem;
  font-size: 0.8125rem;
  font-weight: 500;
  color: #1E0F0C;
  cursor: pointer;
  transition: background 0.12s, color 0.12s;
}
.panel-btn:hover { background: #fff3e5; color: #ff8c1a; }
.panel-btn.active { background: #ff8c1a; color: #fff; font-weight: 600; }
.panel-btn.active:hover { background: #e07712; color: #fff; }
</style>
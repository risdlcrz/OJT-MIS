<template>
  <!-- Same hero + overview layout and theme as the HR dashboard. -->
  <section class="content dashboard-page" :style="{ '--scroll': scrollY }">
    <div class="row">
      <div class="col-md-12">
        <div class="ojt-hero">
          <div class="ojt-hero-inner">
            <p class="ojt-hero-kicker">Supervisor dashboard</p>
            <h1 class="ojt-hero-title ojt-hero-title-name">{{ departmentCode || 'Department' }}</h1>
            <p class="ojt-hero-sub">
              <template v-if="signatory">{{ signatory.name }} &middot; <span class="text-capitalize">{{ signatory.position }}</span></template>
              <template v-else>{{ userName }}</template>
            </p>
            <div class="ojt-hero-actions">
              <a class="ojt-btn ojt-btn-primary" href="/supervisor-time-adjustments.html" @click.prevent="go('supervisor-time-adjustments.html')">
                Review Adjustments
              </a>
              <a class="ojt-btn ojt-btn-ghost" href="/supervisor-interns.html" @click.prevent="go('supervisor-interns.html')">
                Monitor Interns
              </a>
            </div>
          </div>

          <button type="button" class="ojt-scroll-cue" aria-label="Scroll to overview" @click="scrollToSummary">
            <span></span>
            <span></span>
          </button>
        </div>

        <section ref="summaryEl" class="ojt-summary">
          <h2 class="ojt-section-title">Overview</h2>

          <div v-if="scopeError" class="alert alert-warning">
            <i class="fas fa-info-circle me-2"></i>{{ scopeError }}
          </div>
          <div v-else-if="error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
            <span>{{ error }}</span>
            <button type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
          </div>

          <div class="ojt-stats">
            <a class="stat-card stat-orange reveal" href="/supervisor-interns.html" @click.prevent="go('supervisor-interns.html')">
              <div>
                <span class="stat-label">Active Interns</span>
                <span class="stat-number">{{ activeInterns }}</span>
              </div>
              <i class="fas fa-user-graduate"></i>
            </a>

            <a class="stat-card stat-blue reveal" href="/supervisor-time-adjustments.html" @click.prevent="go('supervisor-time-adjustments.html')">
              <div>
                <span class="stat-label">Adjustments to Review</span>
                <span class="stat-number">{{ pending.length }}</span>
              </div>
              <i class="fas fa-user-clock"></i>
            </a>

            <a class="stat-card stat-purple reveal" href="/supervisor-requests.html" @click.prevent="go('supervisor-requests.html')">
              <div>
                <span class="stat-label">Available Slots</span>
                <span class="stat-number">{{ availableSlots }}</span>
              </div>
              <i class="fas fa-layer-group"></i>
            </a>
          </div>

          <div class="ojt-info-grid">
            <article class="ojt-info reveal">
              <h3>Waiting for Review</h3>
              <ul v-if="pending.length" class="ojt-list">
                <li v-for="r in pending.slice(0, 4)" :key="r.id">
                  <span>{{ r.internName }} &middot; {{ adjustmentFieldLabel(r.targetField) }}</span>
                  <span class="ojt-chip">{{ formatDisplayDate(r.requestedTime) }}</span>
                </li>
              </ul>
              <p v-else class="ojt-info-meta">You're all caught up.</p>
            </article>

            <article class="ojt-info reveal">
              <h3>Upcoming Orientations</h3>
              <ul v-if="orientations.length" class="ojt-list">
                <li v-for="o in orientations.slice(0, 4)" :key="o.id">
                  <span>{{ o.fullName }}</span>
                  <span class="ojt-chip" :class="{ 'is-warn': !o.confirmed }">{{ formatDisplayDate(o.date) }}</span>
                </li>
              </ul>
              <p v-else class="ojt-info-meta">No orientations scheduled.</p>
            </article>

            <article class="ojt-info reveal">
              <h3>Summary</h3>
              <ul class="ojt-list">
                <li><span>Interns assigned</span><strong>{{ interns.length }}</strong></li>
                <li><span>Completed interns</span><strong>{{ completedInterns }}</strong></li>
                <li><span>Slot requests awaiting HR</span><strong>{{ pendingSlotRequests }}</strong></li>
                <li><span>Approves</span><strong>{{ scopeLabel }}</strong></li>
              </ul>
            </article>
          </div>
        </section>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, nextTick, onMounted, ref } from 'vue'
import { currentUser, getApiErrorMessage, supervisorAPI } from '@/services/api'
import { formatDisplayDate } from '@/utils/datetime'
import { useHeroDashboard } from '@/utils/dashboard'
import { go } from '@/utils/navigation'
import { adjustmentFieldLabel, useDepartment } from '@/utils/supervisor'
import '../assets/css/Dashboard.css'

const { scrollY, summaryEl, scrollToSummary, revealCards } = useHeroDashboard()
const { department, signatory, scopeError, loadDepartment } = useDepartment()
const userName = currentUser()?.fullName || 'Supervisor'

const interns = ref([])
const pending = ref([])
const orientations = ref([])
const slots = ref([])
const error = ref('')

/* "PMO - Project Management Office" -> "PMO" for the hero title. */
const departmentCode = computed(() => (department.value || '').split(' - ')[0].trim())
const activeInterns = computed(() => interns.value.filter((i) => i.status === 'Hired').length)
const completedInterns = computed(() => interns.value.filter((i) => i.status === 'Completed').length)
const availableSlots = computed(() => slots.value
  .filter((s) => s.status === 'Open')
  .reduce((n, s) => n + (s.remainingSlots || 0), 0))
const pendingSlotRequests = computed(() => slots.value.filter((s) => s.status === 'Pending').length)
const scopeLabel = computed(() => ({ Certificate: 'COC', DTR: 'DTR' })[signatory.value?.approvalScope] || 'COC & DTR')

async function load() {
  error.value = ''
  await loadDepartment()
  if (scopeError.value) return
  try {
    const [i, p, o, s] = await Promise.all([
      supervisorAPI.interns(),
      supervisorAPI.timeAdjustments('Pending'),
      supervisorAPI.orientations({ upcomingOnly: true }),
      supervisorAPI.slots()
    ])
    interns.value = i || []
    pending.value = p || []
    orientations.value = o || []
    slots.value = s || []
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load your dashboard.')
  } finally {
    nextTick(revealCards)
  }
}

onMounted(load)
</script>

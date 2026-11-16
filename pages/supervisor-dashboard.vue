<template>
  <section class="content">
    <div class="sup-hero">
      <div>
        <span class="sup-eyebrow">Supervisor Portal</span>
        <h2 class="sup-title">{{ department || 'Your Department' }}</h2>
        <p class="sup-sub">
          <template v-if="signatory">
            {{ signatory.name }} &middot; <span class="text-capitalize">{{ signatory.position }}</span>
            &middot; Approves {{ scopeLabel(signatory.approvalScope) }}
          </template>
          <template v-else>Signed in as {{ userName }}</template>
        </p>
      </div>
    </div>

    <div v-if="scopeError" class="alert alert-warning">
      <i class="fas fa-info-circle me-2"></i>{{ scopeError }}
    </div>
    <div v-else-if="error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
    </div>

    <div class="ojt-stat-grid">
      <a class="ojt-stat" href="/supervisor-interns.html" @click.prevent="go('/supervisor-interns.html')">
        <span class="ojt-stat-label">Active interns</span>
        <span class="ojt-stat-value">{{ isLoading ? '…' : activeInterns }}</span>
        <span class="ojt-stat-hint">{{ interns.length }} assigned in total</span>
      </a>
      <a class="ojt-stat" :class="{ 'ojt-stat--alert': pending.length > 0 }" href="/supervisor-time-adjustments.html"
         @click.prevent="go('/supervisor-time-adjustments.html')">
        <span class="ojt-stat-label">Adjustments to review</span>
        <span class="ojt-stat-value">{{ isLoading ? '…' : pending.length }}</span>
        <span class="ojt-stat-hint">Time correction requests</span>
      </a>
      <a class="ojt-stat" href="/supervisor-orientations.html" @click.prevent="go('/supervisor-orientations.html')">
        <span class="ojt-stat-label">Upcoming orientations</span>
        <span class="ojt-stat-value">{{ isLoading ? '…' : orientations.length }}</span>
        <span class="ojt-stat-hint">Scheduled by HR</span>
      </a>
      <a class="ojt-stat" href="/supervisor-requests.html" @click.prevent="go('/supervisor-requests.html')">
        <span class="ojt-stat-label">Available slots</span>
        <span class="ojt-stat-value">{{ isLoading ? '…' : availableSlots }}</span>
        <span class="ojt-stat-hint">{{ pendingSlotRequests }} request(s) awaiting HR</span>
      </a>
    </div>

    <div class="row g-3">
      <div class="col-lg-6">
        <div class="ojt-table-card h-100">
          <div class="ojt-table-head">
            <h3 class="ojt-table-title">Waiting for your review</h3>
            <button type="button" class="btn btn-link btn-sm p-0" @click="go('/supervisor-time-adjustments.html')">View all</button>
          </div>
          <div class="table-responsive">
            <table class="table table-hover ojt-table">
              <tbody>
                <TableState :colspan="3" :loading="isLoading" :skeleton-rows="3" :empty="!isLoading && pending.length === 0"
                            icon="fas fa-check" title="You're all caught up" text="No time adjustments are pending." />
                <template v-if="!isLoading">
                  <tr v-for="r in pending.slice(0, 5)" :key="r.id" class="ojt-row-link" tabindex="0"
                      @click="go('/supervisor-time-adjustments.html')" @keydown.enter="go('/supervisor-time-adjustments.html')">
                    <td>
                      <span class="ojt-cell-primary">{{ r.internName }}</span>
                      <span class="ojt-cell-secondary">{{ adjustmentFieldLabel(r.targetField) }} &rarr; {{ formatClock(r.requestedTime) }}</span>
                    </td>
                    <td class="col-date">{{ formatDisplayDate(r.requestedTime) }}</td>
                    <td class="col-actions"><StatusPill status="Pending" /></td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <div class="col-lg-6">
        <div class="ojt-table-card h-100">
          <div class="ojt-table-head">
            <h3 class="ojt-table-title">Coming up</h3>
            <button type="button" class="btn btn-link btn-sm p-0" @click="go('/supervisor-orientations.html')">View schedule</button>
          </div>
          <div class="table-responsive">
            <table class="table table-hover ojt-table">
              <tbody>
                <TableState :colspan="3" :loading="isLoading" :skeleton-rows="3" :empty="!isLoading && orientations.length === 0"
                            icon="far fa-calendar" title="Nothing scheduled" text="No upcoming orientations for your department." />
                <template v-if="!isLoading">
                  <tr v-for="o in orientations.slice(0, 5)" :key="o.id">
                    <td>
                      <span class="ojt-cell-primary">{{ o.fullName }}</span>
                      <span class="ojt-cell-secondary">{{ o.schoolName || 'School not set' }}</span>
                    </td>
                    <td class="col-date">
                      <span class="ojt-cell-primary">{{ formatDisplayDate(o.date) }}</span>
                      <span class="ojt-cell-secondary">{{ relativeDay(o.date) }}<template v-if="o.time"> &middot; {{ formatClock(o.time) }}</template></span>
                    </td>
                    <td class="col-actions"><StatusPill :status="o.confirmed ? 'Confirmed' : 'Awaiting confirmation'" :label="o.confirmed ? 'Confirmed' : 'Unconfirmed'" /></td>
                  </tr>
                </template>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { currentUser, getApiErrorMessage, supervisorAPI } from '@/services/api'
import { formatClock, formatDisplayDate } from '@/utils/datetime'
import { adjustmentFieldLabel, go, relativeDay, useDepartment } from '@/utils/supervisor'

const { department, signatory, scopeError, loadDepartment } = useDepartment()
const userName = currentUser()?.fullName || 'Supervisor'

const interns = ref([])
const pending = ref([])
const orientations = ref([])
const slots = ref([])
const isLoading = ref(true)
const error = ref('')

const activeInterns = computed(() => interns.value.filter((i) => i.status === 'Hired').length)
const availableSlots = computed(() => slots.value
  .filter((s) => s.status === 'Open')
  .reduce((n, s) => n + (s.remainingSlots || 0), 0))
const pendingSlotRequests = computed(() => slots.value.filter((s) => s.status === 'Pending').length)

function scopeLabel(scope) {
  if (scope === 'Certificate') return 'Certificates of Completion'
  if (scope === 'DTR') return 'DTRs'
  return 'Certificates and DTRs'
}

async function load() {
  isLoading.value = true
  error.value = ''
  await loadDepartment()
  if (scopeError.value) {
    isLoading.value = false
    return
  }
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
    isLoading.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.sup-hero {
  margin-bottom: 1.25rem;
  padding: 1.4rem 1.5rem;
  background: linear-gradient(135deg, #fff7ed 0%, #ffffff 70%);
  border: 1px solid #fed7aa;
  border-radius: 14px;
}

.sup-eyebrow {
  color: #c2410c;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.sup-title {
  margin: 0.2rem 0 0.25rem;
  color: #1E0F0C;
  font-size: 1.4rem;
  font-weight: 700;
}

.sup-sub {
  margin: 0;
  color: #6b625d;
  font-size: 0.9rem;
}
</style>

<template>
  <section class="content dashboard-page">
    <div class="row">
      <div class="col-md-12">
        <div class="ojt-hero">
          <h1 class="ojt-hero-title">OJTMS</h1>
          <div class="ojt-hero-actions">
            <a class="ojt-btn ojt-btn-primary" href="/requests.html" @click.prevent="go('requests.html')">
              Request for Intern
            </a>
            <a class="ojt-btn ojt-btn-ghost" href="/internlist.html" @click.prevent="go('internlist.html')">
              Monitor Interns
            </a>
          </div>
        </div>

        <div class="row stats-row">
          <div class="col-lg-4 col-md-6 mb-4">
            <a class="stat-card stat-blue" href="/requests.html" @click.prevent="go('requests.html')">
              <div>
                <span class="stat-label">Requests</span>
                <span class="stat-number">{{ internRequests }}</span>
              </div>
              <i class="fas fa-file-alt"></i>
            </a>
          </div>

          <div class="col-lg-4 col-md-6 mb-4">
            <a class="stat-card stat-purple" href="/applicants.html" @click.prevent="go('applicants.html')">
              <div>
                <span class="stat-label">Applicants</span>
                <span class="stat-number">{{ internApplicants }}</span>
              </div>
              <i class="fas fa-users"></i>
            </a>
          </div>

          <div class="col-lg-4 col-md-6 mb-4">
            <a class="stat-card stat-orange" href="/internlist.html" @click.prevent="go('internlist.html')">
              <div>
                <span class="stat-label">Active Interns</span>
                <span class="stat-number">{{ activeInterns }}</span>
              </div>
              <i class="fas fa-user-graduate"></i>
            </a>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { internsAPI, internRequestsAPI, applicantsAPI } from '@/services/api'
import '../assets/css/Dashboard.css'

const activeInterns = ref(0)
const internRequests = ref(0)
const internApplicants = ref(0)

/* Parehong paraan ng pag-navigate ng BaseLayout: baguhin ang URL tapos
   ipaalam sa App na muli itong buksan. */
function go(page) {
  window.history.pushState({}, '', `/${page}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

async function loadStats() {
  try {
    const [interns, requests, applicants] = await Promise.all([
      internsAPI.getAll(),
      internRequestsAPI.getAll(),
      applicantsAPI.getAll()
    ])
    activeInterns.value = interns.length
    internRequests.value = requests.length
    // Bilangin lamang ang mga hindi pa nahihire para tumugma ito sa
    // Applicants table (tingnan ang displayApplicants sa applicants.vue).
    internApplicants.value = applicants.filter(a => !a.hired).length
  } catch (error) {
    console.error('Failed to load dashboard stats:', error)
  }
}

onMounted(loadStats)
</script>




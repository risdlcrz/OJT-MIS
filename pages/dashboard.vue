<template>
  <section class="content dashboard-page">
    <div class="row">
      <div class="col-md-12">
        <div class="card dashboard-hero-card">
          <div class="card-body clearfix">
            <div class="row align-items-center">
              <div class="bannerWrapper">
                <div class="banner">
                  <div class="bannerImageWrapper">
                    <div class="banner-col banner-col-1">&nbsp;</div>
                    <div class="banner-col banner-col-2">&nbsp;</div>
                    <div class="banner-col banner-col-3">&nbsp;</div>
                    <div class="banner-col banner-col-4">&nbsp;</div>
                  </div>
                  <div class="paper1 bannerPaper d-none d-md-block"></div>
                  <div class="paper2 bannerPaper d-none d-md-block"></div>
                  <div class="paper3 bannerPaper d-none d-md-block"></div>
                </div>
                <blockquote class="quote-Theme">
                  <div>
                    <p class="hero-kicker">OJT Management System</p>
                    <span class="font-weight-light display-4 d-none d-md-block logoSysName">OJTMIS</span>
                    <span class="font-weight-light display-5 d-none d-sm-block d-md-none logoSysName">OJTMIS</span>
                    <span class="font-weight-light display-6 d-block d-sm-none logoSysName">OJTMIS</span>
                  </div>
                </blockquote>
              </div>
            </div>
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




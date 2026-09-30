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
          <div class="col-lg-3 col-md-6 mb-4">
            <div class="stat-card stat-orange">
              <div>
                <span class="stat-label">Active Intern</span>
                <span class="stat-number">{{ activeInterns }}</span>
              </div>
              <i class="fas fa-user-graduate"></i>
            </div>
          </div>

          <div class="col-lg-3 col-md-6 mb-4">
            <div class="stat-card stat-blue">
              <div>
                <span class="stat-label">Intern Request</span>
                <span class="stat-number">{{ internRequests }}</span>
              </div>
              <i class="fas fa-file-alt"></i>
            </div>
          </div>

          <div class="col-lg-3 col-md-6 mb-4">
            <div class="stat-card stat-green">
              <div>
                <span class="stat-label">Office Requested</span>
                <span class="stat-number">{{ officesRequested }}</span>
              </div>
              <i class="fas fa-building"></i>
            </div>
          </div>

          <div class="col-lg-3 col-md-6 mb-4">
            <div class="stat-card stat-purple">
              <div>
                <span class="stat-label">Intern Applicant</span>
                <span class="stat-number">{{ internApplicants }}</span>
              </div>
              <i class="fas fa-users"></i>
            </div>
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
const officesRequested = ref(0)
const internApplicants = ref(0)

async function loadStats() {
  try {
    const [interns, requests, applicants] = await Promise.all([
      internsAPI.getAll(),
      internRequestsAPI.getAll(),
      applicantsAPI.getAll()
    ])
    activeInterns.value = interns.length
    internRequests.value = requests.length
    officesRequested.value = new Set(requests.map(r => r.officeCode)).size
    internApplicants.value = applicants.length
  } catch (error) {
    console.error('Failed to load dashboard stats:', error)
  }
}

onMounted(loadStats)
</script>




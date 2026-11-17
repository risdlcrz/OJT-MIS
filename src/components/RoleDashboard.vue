<script setup>
import { computed, defineAsyncComponent } from 'vue'
import { isIntern, isSupervisor } from '@/services/api'

/*
 * dashboard.html shows a different screen per role. The choice is made each
 * time this renders (vue-router would cache a lazily loaded route component,
 * so a different role signing in on the same tab would see the old one).
 */
const HrDashboard = defineAsyncComponent(() => import('../../pages/dashboard.vue'))
const InternDashboard = defineAsyncComponent(() => import('../../pages/intern-dashboard.vue'))
const SupervisorDashboard = defineAsyncComponent(() => import('../../pages/supervisor-dashboard.vue'))

const dashboard = computed(() => {
  if (isIntern()) return InternDashboard
  if (isSupervisor()) return SupervisorDashboard
  return HrDashboard
})
</script>

<template>
  <component :is="dashboard" />
</template>

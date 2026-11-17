<script setup>
import BaseLayout from './components/BaseLayout.vue'
import { pageOf } from './utils/navigation'

/*
 * Routing lives in src/router/index.js (vue-router). The sign-in page is
 * shown on its own; every other page is wrapped in the app layout
 * (sidebar, top bar, notifications).
 */
</script>

<template>
  <!-- Nothing renders until the router has resolved the first page, so the
       layout (and its notification requests) never flashes for a signed-out visitor. -->
  <router-view v-slot="{ Component, route }">
    <template v-if="!route.matched.length" />
    <component v-else-if="route.meta.bare" :is="Component" />
    <BaseLayout v-else :current-page="pageOf(route.path)">
      <component :is="Component" />
    </BaseLayout>
  </router-view>
</template>

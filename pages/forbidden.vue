<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-body">
        <div class="text-center py-5">
          <i class="fas fa-lock fa-3x text-danger mb-3"></i>
          <h5 class="mb-2">Access denied</h5>
          <p class="text-muted mb-3">
            Ang mga view na ito ay para sa <strong>HR Admin</strong> lamang.
          </p>
          <p v-if="user" class="text-muted small mb-4">
            Naka-login ka bilang <strong>{{ user.fullName }}</strong>
            <span v-if="user.roles?.length"> ({{ user.roles.join(', ') }})</span>.
          </p>
          <button type="button" class="btn btn-outline-secondary" @click="signOut">
            Sign out
          </button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { currentUser, clearSession } from '@/services/api'

const user = currentUser()

function signOut() {
  clearSession()
  window.history.replaceState({}, '', '/login.html')
  window.dispatchEvent(new PopStateEvent('popstate'))
}
</script>

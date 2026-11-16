<template>
  <!-- Loading: skeleton rows that keep the table's shape. -->
  <template v-if="loading">
    <tr v-for="n in skeletonRows" :key="`sk-${n}`" aria-hidden="true">
      <td v-for="c in colspan" :key="c">
        <span class="ojt-skeleton" :style="{ width: `${45 + ((n * 17 + c * 23) % 45)}%` }"></span>
      </td>
    </tr>
  </template>

  <!-- Empty: icon, title, and an optional hint. -->
  <tr v-else-if="empty">
    <td :colspan="colspan" class="ojt-empty">
      <div class="ojt-empty-icon"><i :class="icon" aria-hidden="true"></i></div>
      <p class="ojt-empty-title">{{ title }}</p>
      <p v-if="text" class="ojt-empty-text">{{ text }}</p>
      <slot />
    </td>
  </tr>
</template>

<script setup>
defineProps({
  colspan: { type: Number, required: true },
  loading: { type: Boolean, default: false },
  empty: { type: Boolean, default: false },
  icon: { type: String, default: 'far fa-folder-open' },
  title: { type: String, default: 'Nothing to show yet' },
  text: { type: String, default: '' },
  skeletonRows: { type: Number, default: 4 }
})
</script>

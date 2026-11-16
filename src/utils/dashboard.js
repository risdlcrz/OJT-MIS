import { onBeforeUnmount, onMounted, ref } from 'vue'

/**
 * Shared behavior of the hero dashboards (HR, intern, supervisor):
 * parallax scroll value, the "scroll to summary" cue, and the fade-in of
 * `.ojt-summary .reveal` cards.
 *
 * Call `revealCards()` again after cards render late (e.g. after loading).
 */
export function useHeroDashboard() {
  const scrollY = ref(0)
  const summaryEl = ref(null)
  let raf = 0
  let observer = null

  function onScroll() {
    if (raf) return
    raf = requestAnimationFrame(() => {
      scrollY.value = window.scrollY
      raf = 0
    })
  }

  function scrollToSummary() {
    summaryEl.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  function revealCards() {
    observer?.disconnect()
    const items = document.querySelectorAll('.ojt-summary .reveal')
    if (!('IntersectionObserver' in window)) {
      items.forEach((el) => el.classList.add('reveal-in'))
      return
    }
    observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return
        entry.target.classList.add('reveal-in')
        observer.unobserve(entry.target)
      })
    }, { threshold: 0.15 })
    items.forEach((el) => observer.observe(el))
  }

  onMounted(() => {
    window.addEventListener('scroll', onScroll, { passive: true })
    revealCards()
  })

  onBeforeUnmount(() => {
    window.removeEventListener('scroll', onScroll)
    if (raf) cancelAnimationFrame(raf)
    observer?.disconnect()
  })

  return { scrollY, summaryEl, scrollToSummary, revealCards }
}

import { createApp } from 'vue'
import App from './App.vue'

// Bootstrap 5.3 is the styling baseline for the CRUD screens. Individual
// pages can still layer their own stylesheets on top of it.
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap'

// Colors of the status badges in tables (the BS4 badge-* classes).
import '../assets/css/badges.css'

// Puts modals and their backdrops above Bootstrap's defaults,
// so a panel never ends up behind a see-through backdrop.
import '../assets/css/ojt-modals.css'

// Orange accent and dark text color for the whole app.
import '../assets/css/theme.css'

// Shared data-table look: toolbar, status pills, empty states.
import '../assets/css/data-table.css'

import router from './router'
import { bindRouter } from './utils/navigation'

bindRouter(router)
createApp(App).use(router).mount('#app')

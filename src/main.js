import { createApp } from 'vue'
import App from './App.vue'

// Bootstrap 5.3 is the styling baseline for the CRUD screens. Individual
// pages can still layer their own stylesheets on top of it.
import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap'

// Ipinapataas ang layering ng modal/backdrop sa ibabaw ng Bootstrap,
// para hindi mapupunat ang panel sa likod ng transparent na backdrop.
import '../assets/css/ojt-modals.css'

createApp(App).mount('#app')

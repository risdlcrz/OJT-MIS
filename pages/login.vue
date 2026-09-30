<template>


    <!-- Loader -->
    <div id="preloader">
        <div id="status">
            <div class="spinner"></div>
        </div>
    </div>

    <!-- Begin page -->
    <div class="accountbg"></div>

    <div class="account-pages mt-5 pt-5">
        <div class="container">
            <div class="row justify-content-center">
                <div class="col-md-8 col-lg-5 col-xl-4">
                    <div class="card">
                        <div class="card-body">
                            
                            <div class="p-3">
                                <div class="float-text text-center">
                                    <a href="javascript: void(0);" class="logo-admin">
                                        <img src="../assets/images/logo.png" height="70" alt="logo">
                                    </a>
                                    <h4 class="font-18 mt-3 m-b-5">OJT-MIS</h4>
                                    <p class="login-note">Sign in to continue to your dashboard</p>
                                </div>
                            </div>

                            <div class="p-3">

                        <form class="form-horizontal" id="loginForm" @submit.prevent="submit">
                            <div class="loginWrapper">
                                <div class="mb-3" v-if="mode === 'signup'">
                                    <div class="row">
                                        <div class="col-sm-6 mb-2">
                                            <label class="form-label" for="firstName">First Name</label>
                                            <input v-model.trim="firstName" type="text" class="form-control" id="firstName"
                                                   maxlength="50" required>
                                        </div>
                                        <div class="col-sm-6 mb-2">
                                            <label class="form-label" for="lastName">Last Name</label>
                                            <input v-model.trim="lastName" type="text" class="form-control" id="lastName"
                                                   maxlength="50" required>
                                        </div>
                                    </div>
                                </div>

                                <div class="mb-3">
                                    <label class="form-label" for="email">Email</label>
                                    <input v-model.trim="email" type="email" class="form-control" id="email"
                                           placeholder="Enter email address" name="loginid" autocomplete="email"
                                           maxlength="256" required>
                                </div>

                                <div class="mb-3">
                                    <label class="form-label" for="emailpassword">Password</label>
                                    <input v-model="password" type="password" class="form-control" id="emailpassword"
                                           :placeholder="mode === 'signup' ? 'At least 8 characters' : 'Enter password'"
                                           name="password" :autocomplete="mode === 'signup' ? 'new-password' : 'current-password'"
                                           maxlength="100" required>
                                </div>

                                <p v-if="errorMessage" class="text-danger" role="alert">{{ errorMessage }}</p>

                                <div class="row mt-4">
                                    <div class="col-sm-6">
                                        <span class="text-muted small">
                                            {{ mode === 'signup' ? 'Bagong account? Mag-sign up.' : 'Wala pang account?' }}
                                            <a href="#" class="text-primary" @click.prevent="toggleMode">
                                                {{ mode === 'signup' ? 'Mag-sign in' : 'Sign up' }}
                                            </a>
                                        </span>
                                    </div>
                                    <div class="col-sm-6 text-end">
                                        <button class="btn btn-primary w-md waves-effect waves-light" type="submit" :disabled="isBusy">
                                            <span v-if="isBusy" class="spinner-border spinner-border-sm me-1"></span>
                                            {{ mode === 'signup' ? 'Sign Up' : 'Log In' }}
                                        </button>
                                    </div>
                                </div>

                                <div class="mb-0 row">
                                    <div class="col-12 mt-4 text-center">
                                        <a href="#" class="text-muted"><i class="mdi mdi-lock"></i>Forgot your password?</a>
                                    </div>
                                </div>
                            </div>
                        </form>

                            </div>


                        </div>
                    </div>
                    <div class="mt-5 text-center position-relative text-white">
                        <div class="d-none d-sm-block">
                            <span class="text-sm">System Version <span class="myAppInfo_Version text-Theme">1.0.0</span></span>
                        </div>
                        <span class="lead text-Theme text-shadow">
                            © 2026 OJT-MIS
                        </span>
                        <span class="d-none d-md-block footer-note">Developed and Maintained by Information Systems Development and Maintenance Division (ISDMD), Information Technology Office</span>

                    </div>

                </div>
            </div>
        </div>
    </div>


    <%-- Javascripts --%>
    
</template>

<script setup>
import { onMounted, ref } from 'vue'
import '../assets/css/themeCSS.css'
import '../assets/css/myCSS.css'
import '../assets/css/Login.css'
import { authAPI, getApiErrorMessage } from '@/services/api'

const mode = ref('login')
const email = ref('')
const password = ref('')
const firstName = ref('')
const lastName = ref('')
const errorMessage = ref('')
const isBusy = ref(false)

function toggleMode() {
    mode.value = mode.value === 'login' ? 'signup' : 'login'
    errorMessage.value = ''
}

async function submit() {
    errorMessage.value = ''

    if (mode.value === 'signup' && (!firstName.value || !lastName.value)) {
        errorMessage.value = 'Please enter your first and last name.'
        return
    }

    isBusy.value = true

    try {
        if (mode.value === 'signup') {
            await authAPI.register({
                email: email.value,
                password: password.value,
                firstName: firstName.value,
                lastName: lastName.value
            })
        } else {
            await authAPI.login(email.value, password.value)
        }

        window.history.pushState({}, '', '/dashboard.html')
        window.dispatchEvent(new PopStateEvent('popstate'))
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error, 'Unable to sign in. Please try again.')
    } finally {
        isBusy.value = false
    }
}

/* Kung may session na, huwag nang ipakita ang login form. */
onMounted(() => {
    if (localStorage.getItem('authToken')) {
        window.history.replaceState({}, '', '/dashboard.html')
        window.dispatchEvent(new PopStateEvent('popstate'))
    }
})
</script>

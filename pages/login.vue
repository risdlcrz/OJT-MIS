<template>
    <div
        class="login-page"
        :style="{ backgroundImage: `linear-gradient(to right, rgba(120, 80, 20, 0.6), rgba(120, 30, 30, 0.7)), url(${loginBackground})` }"
    >
        <main class="login-panel">
            <!-- Seal -->
            <header class="login-header">
                <div class="login-seal">
                    <img src="../assets/images/logo.png" alt="Commission on Audit seal" />
                </div>
                <h1 class="login-heading">Sign in with email</h1>
            </header>

            <!-- Login Form -->
            <form id="loginForm" class="login-form" @submit.prevent="submit">
                <!-- Email -->
                <div class="login-field">
                    <input
                        id="email"
                        v-model.trim="email"
                        type="email"
                        class="login-input"
                        placeholder="Email..."
                        name="loginid"
                        autocomplete="email"
                        aria-label="Email"
                        maxlength="256"
                        required
                    />
                </div>

                <!-- Password -->
                <div class="login-field">
                    <input
                        id="emailpassword"
                        v-model="password"
                        type="password"
                        class="login-input"
                        :placeholder="mode === 'signup' ? 'At least 8 characters' : 'Password...'"
                        name="password"
                        :autocomplete="mode === 'signup' ? 'new-password' : 'current-password'"
                        aria-label="Password"
                        maxlength="100"
                        required
                    />
                    <div class="login-forgot">
                        <a href="#" class="login-forgot-link" @click.prevent>Forgot password?</a>
                    </div>
                </div>

                <!-- Sign Up Fields (Hidden by default) -->
                <div v-if="mode === 'signup'" class="login-name-row">
                    <input
                        id="firstName"
                        v-model.trim="firstName"
                        type="text"
                        class="login-input"
                        placeholder="First Name..."
                        name="firstName"
                        autocomplete="given-name"
                        aria-label="First name"
                        maxlength="50"
                        required
                    />
                    <input
                        id="lastName"
                        v-model.trim="lastName"
                        type="text"
                        class="login-input"
                        placeholder="Last Name..."
                        name="lastName"
                        autocomplete="family-name"
                        aria-label="Last name"
                        maxlength="50"
                        required
                    />
                </div>

                <!-- Error Message -->
                <p v-if="errorMessage" class="login-error" role="alert">{{ errorMessage }}</p>

                <!-- Submit Button -->
                <button type="submit" class="login-submit" :disabled="isBusy">
                    <span
                        v-if="isBusy"
                        class="spinner-border spinner-border-sm me-2"
                        role="status"
                        aria-hidden="true"
                    ></span>
                    {{ mode === 'signup' ? 'Sign Up' : 'Login' }}
                </button>
            </form>

            <!-- Sign Up / Sign In Toggle -->
            <footer class="login-footer">
                <span class="login-footer-text">
                    {{ mode === 'signup' ? 'Already have an account?' : "Don't have an account yet?" }}
                </span>
                <a href="#" class="login-footer-link" @click.prevent="toggleMode">
                    {{ mode === 'signup' ? 'Sign in' : 'Sign up' }}
                </a>
            </footer>
        </main>
    </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import '../assets/css/themeCSS.css'
import '../assets/css/myCSS.css'
import '../assets/css/Login.css'
import loginBackground from '../assets/images/login-bg.jpg'
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

<style scoped>
/* Full-screen page with background photo + warm gradient overlay */
.login-page {
    position: fixed;
    inset: 0;
    z-index: 1000;
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding: 24px;
    overflow-y: auto;
    background-color: #6b4a1c;
    background-position: center;
    background-size: 1550px;
    background-repeat: no-repeat;
    font-family: "Inter", "Segoe UI", Roboto, Arial, sans-serif;
    -webkit-font-smoothing: antialiased;
}

/* Centered glassmorphism card */
.login-panel {
    width: 100%;
    max-width: 430px;
    margin: auto;
    padding: 46px 30px 44px;
    border-radius: 1.5rem;
    background: rgba(30, 30, 30, 0.58);
    border: 1px solid rgba(255, 255, 255, 0.15);
    box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.37);
    -webkit-backdrop-filter: blur(12px) saturate(180%);
    backdrop-filter: blur(12px) saturate(180%);
    color: #ffffff;
    text-align: center;
}

/* Seal + heading */
.login-header {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
    margin-bottom: 42px;
}

.login-seal {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 72px;
    height: 72px;
    border-radius: 50%;
    background: #ffffff;
    box-shadow: 0 4px 14px rgba(0, 0, 0, 0.35);
}

.login-seal img {
    width: 54px;
    height: 54px;
    object-fit: contain;
}

.login-heading {
    margin: 0;
    color: #ffffff;
    font-size: 20px;
    font-weight: 700;
    line-height: 1.25;
    letter-spacing: 0.01em;
}

/* Form */
.login-form {
    display: flex;
    flex-direction: column;
    gap: 24px;
}

.login-field {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.login-input {
    width: 100%;
    padding: 14px 20px;
    border: 0;
    border-radius: 999px;
    background: #ffffff;
    color: #1E0F0C;
    font-family: inherit;
    line-height: 1.5;
    outline: none;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
    transition: box-shadow 0.2s ease;
    box-sizing: border-box;
    font-size: 16px;
}

.login-input::placeholder {
    color: #8b9099;
    opacity: 1;
}

.login-input:focus {
    box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.35), 0 2px 8px rgba(0, 0, 0, 0.12);
}

.login-name-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}

/* Forgot password */
.login-forgot {
    display: flex;
    justify-content: flex-end;
    padding-right: 2px;
}

.login-forgot-link {
    color: #ffffff;
    font-size: 12px;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 2px;
    transition: color 0.2s ease;
}

.login-forgot-link:hover {
    color: rgba(255, 255, 255, 0.8);
}

/* Error */
.login-error {
    margin: 0;
    padding: 10px 14px;
    border: 1px solid rgba(254, 202, 202, 0.4);
    border-radius: 12px;
    background: rgba(153, 27, 27, 0.3);
    color: #fee2e2;
    font-size: 12.5px;
    line-height: 1.45;
    text-align: left;
}

/* Primary button */
.login-submit {
    align-self: center;
    min-width: 160px;
    padding: 15px 38px;
    border: 0;
    border-radius: 999px;
    background: #ffffff;
    color: #1E0F0C;
    font-family: inherit;
    font-size: 15px;
    font-weight: 700;
    line-height: 1.4;
    cursor: pointer;
    margin-top: -6px;
    box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
    transition: background 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.login-submit:hover:not(:disabled) {
    background: #f5f5f5;
    box-shadow: 0 8px 20px rgba(0, 0, 0, 0.28);
    transform: translateY(-1px);
}

.login-submit:active:not(:disabled) {
    transform: translateY(0);
}

.login-submit:disabled {
    opacity: 0.65;
    cursor: not-allowed;
}

.login-submit .spinner-border {
    width: 1rem;
    height: 1rem;
    border-width: 0.15em;
    vertical-align: -0.15em;
}

/* Footer links */
.login-footer {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 9px;
    margin-top: 38px;
}

.login-footer-text {
    color: rgba(255, 255, 255, 0.9);
    font-size: 12.5px;
    font-weight: 600;
}

.login-footer-link {
    color: #ffffff;
    font-size: 12.5px;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 2px;
    transition: color 0.2s ease;
}

.login-footer-link:hover {
    color: rgba(255, 255, 255, 0.8);
}

@media (max-width: 480px) {
    .login-page {
        padding: 16px;
    }

    .login-panel {
        padding: 30px 22px 26px;
        border-radius: 1.25rem;
    }

    .login-header {
        gap: 12px;
        margin-bottom: 26px;
    }

    .login-seal {
        width: 62px;
        height: 62px;
    }

    .login-seal img {
        width: 46px;
        height: 46px;
    }

    .login-heading {
        font-size: 18px;
    }

    .login-name-row {
        grid-template-columns: 1fr;
    }

    .login-submit {
        width: 100%;
    }
}

@media (max-height: 700px) {
    .login-panel {
        padding: 28px 30px 26px;
    }

    .login-header {
        gap: 10px;
        margin-bottom: 24px;
    }

    .login-form {
        gap: 14px;
    }

    .login-footer {
        margin-top: 22px;
    }
}

@media (prefers-reduced-motion: reduce) {
    .login-input,
    .login-submit,
    .login-forgot-link,
    .login-footer-link {
        transition: none;
    }
}
</style>

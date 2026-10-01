let alertResolve = null
let confirmResolve = null

export function useDialog() {
    const isAlertOpen = ref(false)
    const isConfirmOpen = ref(false)
    const alertTitle = ref('')
    const alertMessage = ref('')
    const confirmTitle = ref('')
    const confirmMessage = ref('')

    function closeAlert() {
        isAlertOpen.value = false
        if (alertResolve) {
            alertResolve()
            alertResolve = null
        }
    }

    function closeConfirm(result) {
        isConfirmOpen.value = false
        if (confirmResolve) {
            confirmResolve(result)
            confirmResolve = null
        }
    }

    function alert(message, title = 'Notification') {
        alertTitle.value = title
        alertMessage.value = message
        isAlertOpen.value = true
        return new Promise((resolve) => {
            alertResolve = resolve
        })
    }

    function confirm(message, title = 'Confirm') {
        confirmTitle.value = title
        confirmMessage.value = message
        isConfirmOpen.value = true
        return new Promise((resolve) => {
            confirmResolve = resolve
        })
    }

    return {
        isAlertOpen,
        isConfirmOpen,
        alertTitle,
        alertMessage,
        confirmTitle,
        confirmMessage,
        alert,
        confirm,
        closeAlert,
        closeConfirm
    }
}

import { ref } from 'vue'
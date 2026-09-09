var baseUrl = $('body').data('baseurl')
var thisPage = $('meta[name=PageName]').attr('content');

var _pageAdd = ''
var _pageEdt = ''

function ValidateError(code) {
    console.log(code.status)
    switch (code.status) {

        case 401:
            errorModal("Invalid Login Credentials. Please Login Again.", ReturnLogin)
            break;
        case 403:
            Open403()
            break;
        case 491: /*DUPLICATE RECORD*/
            warningModal("Record is already at the table. Please add another.", "")
            break;
        case 590: /*DUPLICATE RECORD*/
            warningModal("Reqesting IS did not respond. Please try again later", '')
            break;
        default:
            errorModal("Please try again", '')
            break;
    }
}

function ReturnLogin() {
    window.open(baseUrl + "/login.aspx", "_self")
}

function ReturnHome() {
    window.open(baseUrl + "/pages/home.aspx", "_self")
}

function ActivateMenu() {
    $("li." + thisPage).addClass("mm-active")
    $("a." + thisPage).addClass("mm-active")

    $("#side-menu").metisMenu('dispose').metisMenu()

}

function ValidateError(code) {
    switch (code) {

        case 401:
            errorModal("Invalid Login Credentials. Please Login Again.", ReturnLogin)
            break;
        case 401:
            Open403()
            break
        case 403:
            Open403()
            break;
        case 491: /*DUPLICATE RECORD*/
            warningModal("Record is already at the table. Please add another.", "")
            break;
        case 590: /*DUPLICATE RECORD*/
            warningModal("Reqesting IS did not respond. Please try again later", '')
            break;
        default:
            errorModal("Please try again", '')
            break;
    }
}

const swalBs4 = Swal.mixin({
    customClass: {
        confirmButton: 'btn btn-success',
        cancelButton: 'btn btn-outline-secondary'
    },
    buttonsStyling: false
})

function successModal(message, okFunction = '') {

    swalBs4.fire({
        title: "Success!",
        text: message,
        icon: "success",
        showCancelButton: false,
        confirmButtonText: '<i class="fas fa-check"></i> Okay'
    }).then((result) => {
        if (result.value) {

            if (okFunction != '') {
                okFunction()
            }
        }
    })
}

function errorModal(message, okFunction = '') {
    swalBs4.fire({
        title: "Something went wrong!",
        text: message,
        icon: "error",
        showCancelButton: false,
        confirmButtonText: '<i class="fas fa-check"></i> okay'
    }).then((result) => {
        if (result.value) {
            if (okFunction != '') {
                okFunction()
            }
        }
    })
}

function warningModal(message, okFunction = '') {
    swalBs4.fire({
        title: "Oops!",
        text: message,
        icon: "warning",
        showCancelButton: false,
        confirmButtonText: '<i class="fas fa-check"></i> Okay'
    }).then((result) => {
        if (result.value) {
            if (okFunction != '') {
                okFunction()
            }
        }
    })

}

function infoModal(message, okFunction = '') {
    swalBs4.fire({
        title: "Oops!",
        text: message,
        icon: "info",
        showCancelButton: false,
        confirmButtonText: '<i class="fas fa-check"></i> Okay'
    }).then((result) => {
        if (result.value) {
            if (okFunction != '') {
                okFunction()
            }
        }
    })
}

function yesNoModal(message, yesFunction = '', noFunction = '') {
    swalBs4.fire({
        title: "Question!",
        text: message,
        icon: "question",
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-check"></i> Yes',
        cancelButtonText: '<i class="fas fa-times"></i> No '
    }).then((result) => {
        if (result.value) {
            if (yesFunction != '') {
                yesFunction()
            }
        } else {
            if (noFunction != '') {
                noFunction()
            }
        }
    })
}

function TableCol_YesNo(data) {
    var html = "";
    if (data == 1) {
        html = '<span class="badge badge-pill badge-success">YES</span>'
    }
    else {
        html = '<span class="badge badge-pill badge-danger">NO</span>'
    }

    return html
}

function TableCol_Status(data) {
    var html = "";
    if (data == 1) {
        html = '<span class="badge badge-soft-success">ACTIVE</span>'
    }
    else if (data == 626) {
        html = '<span class="badge badge-soft-warning">PENDING</span>'
    }
    else {
        html = '<span class="badge badge-soft-danger">INACTIVE</span>'
    }

    return html
}

function ToggleItems(item2Hide, item2Show) {
    item2Hide.slideUp("slow");
    item2Show.slideDown("slow");
}

function Open403() {
    window.location(baseUrl + "/pages/err/403.aspx", "_self")
}

function Open401() {
    window.location(baseUrl + "/pages/err/403.aspx", "_self")
}
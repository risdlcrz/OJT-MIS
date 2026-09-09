function Login() {

    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiLogin.asmx/ProcessLogin',
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        error: function (errorDesc) {
            errorModal("Invalid Login", "")            
        },
        success: function (data) {            
            window.open(data.key, "_self")
        }
    })


}

function Logout() {

    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiLogin.asmx/ProcessLogout',
        data: JSON.stringify({}),
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        async: false,
        error: function (errorDesc) {
            errorModal("Invalid Login", "")            
        },
        success: function (data) {
            window.open(data.key, "_self")
        }
    })


}
function LoadEmployeeTable() {
    $('#tblMain').DataTable().destroy()
    $('#tblMain').DataTable({
        'search': {
            'return': true
        },
        'processing': true,
        'serverSide': true,
        'responsive': true,
        'ajax': {
            'url': baseUrl + "/services/utility/UsersTable.ashx?pEdit=" + _pageEdt,
            'dataSrc': 'data',
            'type': 'POST',
            'error': function (errorDesc) {
                ValidateError(errorDesc.status)
            }
        },
        "lengthMenu": [[5, 10, 25, 50, 100], [5, 10, 25, 50, 100]],
        'paging': true,
        'pagingType': 'full_numbers',
        'ordering': false,
        //"scrollX": true,
        "columns": [
            {
                data: "EMP_NO",
                render: function (data, type, row, meta) {

                    var div = $("<div class='row'></div>")
                    var stat = TableCol_Status(row["STATUS"])
                    //var name = $("<span class='display-6'>" + row["NAME"] + "<br/> <small>" + data + "<small> <br/>" + stat + "</span>")

                    var name = $("<h5 class='fs-6'>"+ row["NAME"] + "<br/><small>" + data + "</small><br/>"+ stat +"</h5>")

                    div.append(name)

                    return div[0].outerHTML
                }

            },
            {
                data: 'BTNS',
                render: function (data, type, row, meta) {

                    var child = $(data)
                    var parent = $('<div class="col-md-8 btn-group btn-group-sm btn-group-vertical" role="group"></div>')
                    var row = $('<div class="d-flex justify-content-center"></row>')

                    parent.append(child)
                    row.append(parent)

                    return row[0].outerHTML
                }
            }
        ]
    });
}

function initPage() {
    LoadEmployeeTable()
}

function LoadEmployeeDDL(ctr) {
    ctr.empty()
    ctr.select2({
        "dropdownParent": $("#frmModal"),
        "placeholder": "Please choose an Employee...",
        "minimumInputLength": 2,
        "width": "100%",
        "ajax": {
            url: baseUrl + '/services/utility/EmployeeDDL.ashx',
            cache: true,
            dataType: 'json',
            type: 'GET',
            data: function (params) {
                return {
                    search: params.term
                }
            },
            processResults: function (data, params) {

                if (data.items === undefined) {
                    return {
                        results: [
                            { id: '', text: 'No results found' }
                        ]
                    };
                }

                params.page = params.page || 1;
                return {
                    results: data.items,
                    pagination: {
                        more: (params.page * 30) < data.total_count
                    }
                };
            }
           
        },
        "escapeMarkup": function (markup) {
            return markup;
        },
        "templateResult": function (repo) {
            if (repo.loading) return repo.text;

            var markup = "<h6 class='fs-6'>" + repo.text + "<br/><small>" + TableCol_Status(repo.status) + "</small></h6>"
            return markup;
        },
        "templateSelection": function (repo) {
            return repo.text || repo.text;
        }
    })
}

function OpenFormModal() {
    $("#frmModal").modal("show")
}

function CloseFormModal() {
    $("#frmModal").modal("hide")
}

function LoadAddRecord() {
    LoadEmployeeDDL($("#frmEmp"))
    OpenFormModal()
}

function SaveRecord() {

    var empno = $("#frmEmp").val()

    if (empno == "" || empno == null) {
        warningModal("Please complete the form")
    }
    else {
        $.ajax({
            type: 'POST',
            url: baseUrl + '/services/utility/apiUsers.asmx/SaveRecord',
            data: JSON.stringify({ "empno": empno }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: false,
            error: function (errorDesc) {
                ValidateError(errorDesc)
            },
            success: function (data) {
                successModal("Successfully saved the record", function () {
                    CloseFormModal();
                    LoadEmployeeTable();
                })
            }
        })
    }
}

var _emp = ''
function OpenRemModal() {
    $("#frmRemov").modal("show")
}
function CloseRemModal() {
    $("#frmRemov").modal("hide")
}

function DeleteUser(val1, val2) {
    _emp = val1
    var cell = val2.closest("tr").querySelector("td").innerHTML
    $(".frmRemContent").html(cell)

    OpenRemModal()
}

function SaveRemove() {
    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiUsers.asmx/SaveRemove',
        data: JSON.stringify({ empno: _emp }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        error: function (errorDesc) {
            ValidateError(errorDesc)
        },
        success: function (data) {
            successModal("Successfully removed the record", function () {
                CloseRemModal();
                LoadEmployeeTable();
            })
        }
    })
}


function AssignGroup(val1, val2) {
    _emp = val1
    var cell = val2.closest("tr").querySelector("td").innerHTML
    $(".viewAccessInfo").html(cell)

    LoadUserAccessTable()

    ToggleItems($("#viewMain"), $("#viewAccess"))
}

function ViewMainPage() {
    ToggleItems($("#viewAccess"), $("#viewMain"))
}

function LoadUserAccessDDL(ctr) {
    ctr.empty()
    ctr.select2({
        "dropdownParent": $("#frmGrpModal"),
        "placeholder": "Please choose an Access Group...",
        "width": "100%",
        "ajax": {
            url: baseUrl + '/services/utility/AccessGroupDDL.ashx?empno=' + _emp,
            cache: true,
            dataType: 'json',
            type: 'GET',
            data: function (params) {
                return {
                    search: params.term
                }
            },
            processResults: function (data, params) {

                if (data.items === undefined) {
                    return {
                        results: [
                            { id: '', text: 'No results found' }
                        ]
                    };
                }

                params.page = params.page || 1;
                return {
                    results: data.items,
                    pagination: {
                        more: (params.page * 30) < data.total_count
                    }
                };
            }

        },
        "escapeMarkup": function (markup) {
            return markup;
        },
        "templateResult": function (repo) {
            if (repo.loading) return repo.text;

            var markup = "<h6 class='fs-6'>" + repo.text + "<br/><small>" + TableCol_Status(repo.STATUS) + "</small></h6>"
            return markup;
        },
        "templateSelection": function (repo) {
            return repo.text || repo.text;
        }
    })
}

function LoadUserAccessTable() {
    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiUsers.asmx/GetAccessGroups',
        data: JSON.stringify({ "empno": _emp, "edit": _pageEdt }),
        contentType: "application/json; charset=utf-8",
        dataType:"json",
        async: true,
        error: function (errorDesc) {
            HideLoadingModal();
            ValidateError(errorDesc.status)
        },
        success: function (data) {

            $('#tblAccess').DataTable().destroy()
            $('#tblAccess tbody').empty()
            $('#tblAccess').DataTable({
                'search': {
                    'return': true
                },
                'data': data.data,
                "lengthMenu": [[5, 10, 25, 50, 100], [5, 10, 25, 50, 100]],
                'paging': true,
                'pagingType': 'full_numbers',
                'ordering': false,
                "columns": [
                    {
                        data: "GRP_NAME",
                        render: function (data, type, row, meta) {

                            var div = $("<div class='row'></div>")
                            var stat = TableCol_Status(row["GRP_STATUS"])
                            var name = $("<h5 class='fs-6'>" + data + "<br/>" + stat + "</h5>")

                            div.append(name)

                            return div[0].outerHTML
                        }

                    },
                    {
                        data: 'BTNS',
                        render: function (data, type, row, meta) {

                            var child = $(data)
                            var parent = $('<div class="col-md-8 btn-group btn-group-sm btn-group-vertical" role="group"></div>')
                            var row = $('<div class="d-flex justify-content-center"></row>')

                            parent.append(child)
                            row.append(parent)

                            return row[0].outerHTML
                        }
                    }
                ]
            });

        }
    })
}

function OpenGrpFormModal() {
    $("#frmGrpModal").modal("show")
}

function CloseGrpFormModal() {
    $("#frmGrpModal").modal("hide")
}

function LoadAddUserGrp() {
    LoadUserAccessDDL($("#frmGrp"))
    OpenGrpFormModal();
}
function SaveGrpRecord() {
    var grp = $("#frmGrp").val()

    if (grp == "" || grp == null) {
        warningModal("Please complete the form")
    }
    else {
        $.ajax({
            type: 'POST',
            url: baseUrl + '/services/utility/apiUsers.asmx/SaveGrpRecord',
            data: JSON.stringify({ empno: _emp, group: grp }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: false,
            error: function (errorDesc) {
                ValidateError(errorDesc)
            },
            success: function (data) {
                successModal("Successfully saved the record", function () {
                    CloseGrpFormModal();
                    LoadUserAccessTable();
                })
            }
        })
    }
}

function OpenRemGrpModal() {
    $("#frmGrpRemov").modal("show")
}
function CloseRemGrpModal() {
    $("#frmGrpRemov").modal("hide")
}

var _grp
function DeleteAccess(val1, val2) {
    _grp = val1
    var cell = val2.closest("tr").querySelector("td").innerHTML
    $(".frmGrpRemContent").html(cell)

    OpenRemGrpModal()
}

function SaveGrpRemove() {
    $.ajax({
        type: 'POST',
        url: baseUrl + '/services/utility/apiUsers.asmx/SaveRemoveGrp',
        data: JSON.stringify({ id: _grp }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        async: false,
        error: function (errorDesc) {
            ValidateError(errorDesc)
        },
        success: function (data) {
            successModal("Successfully removed the record", function () {
                CloseRemGrpModal();
                LoadUserAccessTable();
            })
        }
    })
}
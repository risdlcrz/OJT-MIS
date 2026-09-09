$(document).ready(function () {
    $('#tblMain').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/services/systemsetup/City.ashx',
            type: 'POST',
            dataSrc: 'data'
        },
        columns: [
            { data: 'ID', title: 'City ID' },
            { data: 'CODE', title: 'Country ID' },
            { data: 'DESCRIPTION', title: 'City Name' },
            {
                data: null,
                title: 'Actions',
                render: function (data, type, row) {
                    return `
                        <button class="btn btn-warning" onclick="editCity('${row.ID}', '${row.DESCRIPTION}')">Edit</button>
                        <button class="btn btn-danger" onclick="deleteCity('${row.ID}', '${row.DESCRIPTION}')">Delete</button>
                    `;
                }
            }
        ],
        lengthMenu: [[10, 25, 50], [10, 25, 50]],
        paging: true,
        responsive: true,
        ordering: false
    });
});

function editCity(id, name) {
    // Populate the modal with city data
    $('#editCityId').val(id);
    $('#editCityName').val(name);
    $('#editModal').modal('show');
}

function deleteCity(id, name) {
    // Show the delete confirmation modal
    $('#deleteCityName').text(name);
    $('#deleteModal').modal('show');

    // Store the city ID for deletion
    $('#deleteModal').data('id', id);
}

function saveCity() {
    const id = $('#editCityId').val();
    const name = $('#editCityName').val();

    if (!name) {
        alert('City name cannot be empty.');
        return;
    }

    // Make an AJAX call to save the changes
    $.ajax({
        url: '/services/systemsetup/EditCity.ashx',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({ ID: id, DESCRIPTION: name }),
        success: function () {
            $('#editModal').modal('hide');
            $('#tblMain').DataTable().ajax.reload();
        },
        error: function () {
            alert('Failed to save city changes.');
        }
    });
}

function confirmDeleteCity() {
    const id = $('#deleteModal').data('id');

    // Make an AJAX call to delete the city
    $.ajax({
        url: '/services/systemsetup/DeleteCity.ashx',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({ ID: id }),
        success: function () {
            $('#deleteModal').modal('hide');
            $('#tblMain').DataTable().ajax.reload();
        },
        error: function () {
            alert('Failed to delete city.');
        }
    });
}

$(document).ready(function () {
    $('#tblMain').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/services/systemsetup/Continent.ashx',
            type: 'POST',
            dataSrc: 'data'
        },
        columns: [
            { data: 'ID', title: 'Continent ID' },
            { data: 'DESCRIPTION', title: 'Continent Name' },
            {
                data: null,
                title: 'Actions',
                render: function (data, type, row) {
                    return `
                        <button class="btn btn-warning" onclick="editContinent('${row.ID}', '${row.DESCRIPTION}')">Edit</button>
                        <button class="btn btn-danger" onclick="deleteContinent('${row.ID}', '${row.DESCRIPTION}')">Delete</button>
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

function editContinent(id, name) {
    alert(`Edit: ${id}, ${name}`);
}

function deleteContinent(id, name) {
    alert(`Delete: ${id}, ${name}`);
}

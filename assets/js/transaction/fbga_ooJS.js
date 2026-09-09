$('#tblMain').DataTable({
    processing: true,
    serverSide: true,
    ajax: {
        url: '/services/transaction/fbga_oo.ashx',
        type: 'POST',
        dataSrc: 'data', // The 'data' key in the JSON response
        data: function (d) {
            // Send additional parameters to the server if needed
        }
    },
    columns: [
        { data: 'REF', title: 'Reference No.' },
        { data: 'OON', title: 'Office Order No.' },
        { data: 'OOD', title: 'Office Order Date' },
        { data: 'AP', title: 'Audit Place' },
        { data: 'ADF', title: 'Audit Date From' },
        { data: 'ADT', title: 'Audit Date To' },
        {
            data: 'OOS',
            title: 'Office Order Status',
            render: function (data) {
                return data === 1 ? 'Approved' : 'Not Approved';
            }
        },
        {
            data: null,
            title: 'Actions',
            render: function (data, type, row) {
                return `
                    <button class="btn btn-warning" onclick="editOfficeOrder('${row.REF}')">Edit</button>
                    <button class="btn btn-danger" onclick="deleteOfficeOrder('${row.REF}')">Delete</button>
                `;
            }
        }
    ],
    lengthMenu: [[10, 25, 50], [10, 25, 50]],
    paging: true,
    responsive: true,
    ordering: false
});


// Utility function to format date
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toISOString().split('T')[0]; // Extract only the date part (YYYY-MM-DD)
}

// Function to handle editing an office order
function editOfficeOrder(referenceNo) {
    alert(`Edit Office Order with Reference No: ${referenceNo}`);
    // You can load the details into a modal for editing here
}

// Function to handle deleting an office order
function deleteOfficeOrder(referenceNo) {
    if (confirm(`Are you sure you want to delete Office Order with Reference No: ${referenceNo}?`)) {
        $.ajax({
            url: '/services/transaction/fbga_oo.ashx',
            type: 'POST',
            data: JSON.stringify({ action: 'delete', referenceNo }),
            contentType: 'application/json; charset=utf-8',
            success: function (response) {
                alert('Office Order deleted successfully.');
                $('#tblMain').DataTable().ajax.reload();
            },
            error: function (xhr, status, error) {
                alert('Failed to delete Office Order. Please try again.');
                console.error('Error:', error);
            }
        });
    }
}

function LoadAddOfficeOrder() {
    console.log('LoadAddOfficeOrder triggered'); // Debugging output
    // Clear form fields
    $('#referenceOrderNo').val('');
    $('#officeOrderNo').val('');
    $('#officeOrderDate').val('');
    $('#auditPlace').val('');
    $('#auditDateFrom').val('');
    $('#auditDateTo').val('');

    // Show the modal
    $('#addOfficeOrderModal').modal('show');
}

function saveOfficeOrder() {
    // Collect form data
    const officeOrderData = {
        referenceOrderNo: $('#referenceOrderNo').val(),
        officeOrderNo: $('#officeOrderNo').val(),
        officeOrderDate: $('#officeOrderDate').val(),
        auditPlace: $('#auditPlace').val(),
        auditDateFrom: $('#auditDateFrom').val(),
        auditDateTo: $('#auditDateTo').val()
    };

    // Validate fields
    if (!officeOrderData.referenceOrderNo || !officeOrderData.officeOrderNo || !officeOrderData.officeOrderDate || !officeOrderData.auditPlace || !officeOrderData.auditDateFrom || !officeOrderData.auditDateTo) {
        alert('Please fill out all required fields.');
        return;
    }

    // Make an AJAX call to the Web Service
    $.ajax({
        url: '/services/transaction/wbservice.asmx/saveOfficeOrder', // Specify the exact Web Method
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({ officeOrderData: officeOrderData }), // Pass the data object correctly
        success: function (response) {
            const result = response.d; // Web Service wraps the response in `d`
            if (result.ReturnCode === "0000") {
                alert(result.ReturnValue);
                $('#addOfficeOrderModal').modal('hide');
                $('#tblMain').DataTable().ajax.reload(); // Reload the DataTable
            } else {
                alert(result.ReturnValue);
            }
        },
        error: function (xhr, status, error) {
            alert('Failed to save Office Order. Please try again.');
            console.error('Error:', error);
        }
    });
}



```vue
<template>
    <section class="content">


        <div class="card modern-card">

            <!-- Card Header -->
            <div class="card-header modern-card-header">
                <div>
                    <h3 class="card-title">List of Applicants</h3>
                </div>

                <div class="card-tools">
                    <button
                        id="addApplicantBtn"
                        type="button"
                        class="btn btn-primary btn-sm modern-btn"
                        @click="isApplicantModalOpen = true"
                    >
                        <i class="fas fa-plus"></i>
                        Add Applicant
                    </button>
                </div>
            </div>


            <!-- Card Body -->
            <div class="card-body">

                <table
                    id="example1"
                    class="table table-hover modern-table"
                >
                    <thead class="text-center">
                        <tr>
                            <th>APPLICANT NO.</th>
                            <th>LAST NAME</th>
                            <th>FIRST NAME</th>
                            <th>PROGRAM</th>
                            <th>REQUIREMENTS</th>
                            <th>STATUS</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr
                            v-for="applicant in applicants"
                            :key="applicant.applicantNo"
                        >
                            <td>{{ applicant.applicantNo }}</td>

                            <td>{{ applicant.lastName }}</td>

                            <td>{{ applicant.firstName }}</td>

                            <td>
                                {{
                                    applicant.program ||
                                    applicant.educationLevel ||
                                    ''
                                }}
                            </td>

                            <!-- Requirements -->
                            <td>
                                <button
                                    type="button"
                                    class="btn btn-orange-action btn-sm req-btn"
                                    @click="openRequirementsModal(applicant)"
                                >
                                    <i class="fas fa-list-ul"></i>

                                    <span class="req-count badge badge-light text-dark">
                                        {{ reqCount(applicant) }}/6
                                    </span>
                                </button>
                            </td>

                            <!-- Status -->
                            <td>
                                <span
                                    class="badge"
                                    :class="statusInfo(applicant).badgeClass"
                                >
                                    {{ statusInfo(applicant).label }}
                                </span>
                            </td>

                            <!-- Actions -->
                            <td>
                                <div
                                    class="btn-group action-dropdown"
                                    :class="{
                                        dropup:
                                            openDropdown ===
                                                applicant.applicantNo &&
                                            openDropdownUp
                                    }"
                                >

                                    <button
                                        type="button"
                                        class="btn btn-outline-secondary btn-sm dropdown-toggle"
                                        @click.stop="
                                            toggleDropdown(
                                                applicant.applicantNo,
                                                $event
                                            )
                                        "
                                    >
                                        Actions
                                    </button>

                                </div>

                                <Teleport to="body">
                                    <div
                                        v-if="
                                            openDropdown ===
                                            applicant.applicantNo
                                        "
                                        class="dropdown-menu action-dropdown-menu show"
                                        :style="dropdownStyle"
                                        @click.stop
                                    >
                                        <button
                                            type="button"
                                            class="dropdown-item"
                                            @click="openEvaluation(applicant)"
                                        >
                                            <i class="fas fa-clipboard-check"></i>
                                            Evaluation
                                        </button>

                                        <button
                                            type="button"
                                            class="dropdown-item"
                                            @click="openHire(applicant)"
                                        >
                                            <i class="fas fa-user-check"></i>
                                            Hire
                                        </button>

                                        <button
                                            type="button"
                                            class="dropdown-item"
                                            @click="
                                                viewApplicantDetails(applicant)
                                            "
                                        >
                                            <i class="fas fa-eye"></i>
                                            View
                                        </button>

                                        <button
                                            type="button"
                                            class="dropdown-item text-danger"
                                            @click="deleteApplicant(applicant)"
                                        >
                                            <i class="fas fa-trash"></i>
                                            Delete
                                        </button>
                                    </div>
                                </Teleport>
                            </td>
                        </tr>

                        <!-- Empty State -->
                        <tr v-if="applicants.length === 0">
                            <td
                                colspan="7"
                                class="text-center text-muted py-4"
                            >
                                No applicants saved yet.
                            </td>
                        </tr>
                    </tbody>
                </table>

            </div>
        </div>



        <div
            id="ojtModal"
            class="modal fade"
            :class="{ show: isApplicantModalOpen }"
            :style="{
                display: isApplicantModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isApplicantModalOpen"
        >

            <div
                class="modal-dialog modal-xl modal-dialog-centered"
                role="document"
            >

                <div class="modal-content modern-modal">


                    <div class="modal-header modern-modal-header">

                        <h5 class="modal-title font-weight-bold">
                            OJT Applicant Form
                        </h5>

                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="isApplicantModalOpen = false"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>

                    </div>



                    <form
                        id="ojtForm"
                        @submit.prevent="saveApplicant"
                    >

                        <div class="modal-body p-4">


                            <small class="section-title">
                                Personal Information
                            </small>

                            <hr class="section-divider" />


                            <!-- FIRST + MIDDLE + LAST NAME -->
                            <div class="row">

                                <!-- First Name -->
                                <div class="col-md-4 mb-3">
                                    <label>
                                        First Name
                                    </label>

                                    <input
                                        id="firstName"
                                        type="text"
                                        class="form-control"
                                        placeholder="First Name"
                                        required
                                    />
                                </div>


                                <!-- Middle Name -->
                                <div class="col-md-4 mb-3">
                                    <label>
                                        Middle Name
                                    </label>

                                    <input
                                        id="middleName"
                                        type="text"
                                        class="form-control"
                                        placeholder="Middle Name"
                                    />
                                </div>


                                <!-- Last Name -->
                                <div class="col-md-4 mb-3">
                                    <label>
                                        Last Name
                                    </label>

                                    <input
                                        id="lastName"
                                        type="text"
                                        class="form-control"
                                        placeholder="Last Name"
                                        required
                                    />
                                </div>

                            </div>


                            <div class="row">

                                <div class="col-md-12 mb-3">

                                    <label>
                                        Suffix
                                    </label>

                                    <input
                                        id="suffix"
                                        type="text"
                                        class="form-control"
                                        placeholder="Jr., Sr., III"
                                    />

                                </div>

                            </div>



                            <div class="row">

                                <!-- Email -->
                                <div class="col-md-8 mb-3">

                                    <label>
                                        Email Address
                                    </label>

                                    <input
                                        id="email"
                                        type="email"
                                        class="form-control"
                                        placeholder="email@example.com"
                                    />

                                </div>


                                <!-- Contact -->
                                <div class="col-md-4 mb-3">

                                    <label>
                                        Contact Number
                                    </label>

                                    <input
                                        id="contactNumber"
                                        type="tel"
                                        inputmode="numeric"
                                        pattern="[0-9]*"
                                        class="form-control"
                                        placeholder="0912..."
                                        @input="sanitizeContactNumber"
                                    />

                                </div>

                            </div>


                            <!-- =================================================
                                 HOUSE ADDRESS
                                 FULL ROW
                                 ================================================= -->
                            <div class="row">

                                <div class="col-md-12 mb-4">

                                    <label>
                                        House Address
                                    </label>

                                    <input
                                        id="houseAddress"
                                        type="text"
                                        class="form-control"
                                        placeholder="Complete Address"
                                    />

                                </div>

                            </div>



                            <small class="section-title">
                                Academic Background
                            </small>

                            <hr class="section-divider" />


                            <!-- SCHOOL + COURSE -->
                            <div class="row">

                                <!-- School -->
                                <div class="col-md-6 mb-3">

                                    <label>
                                        School Name
                                    </label>

                                    <select
                                        id="schoolName"
                                        class="form-control"
                                        required
                                    >
                                        <option
                                            value=""
                                            selected
                                            disabled
                                        >
                                            Select School
                                        </option>

                                        <option>
                                            Philippine Science High School
                                        </option>

                                        <option>
                                            Polytechnic University
                                        </option>

                                        <option>
                                            Local State University
                                        </option>

                                        <option>
                                            National High School
                                        </option>

                                        <option>
                                            Other
                                        </option>
                                    </select>

                                </div>


                                <!-- Course -->
                                <div class="col-md-6 mb-3">

                                    <label>
                                        Course
                                    </label>

                                    <select
                                        id="educationLevel"
                                        class="form-control"
                                    >
                                        <option
                                            value=""
                                            selected
                                            disabled
                                        >
                                            Select Course
                                        </option>

                                        <option>
                                            BS Computer Science
                                        </option>

                                        <option>
                                            BS Information Technology
                                        </option>

                                        <option>
                                            BS Business Administration
                                        </option>

                                        <option>
                                            BS Accountancy
                                        </option>

                                        <option>
                                            BS Computer Engineering
                                        </option>

                                        <option>
                                            Other
                                        </option>
                                    </select>

                                </div>

                            </div>



                            <small class="section-title section-title-spacing">
                                Internship Details
                            </small>

                            <hr class="section-divider" />

                            <p class="text-muted small">
                                Internship fields are filled automatically
                                based on the selected school.
                            </p>


                            <!-- COORDINATOR + REQUIRED HOURS -->
                            <div class="row">

                                <!-- Coordinator -->
                                <div class="col-md-6 mb-3">

                                    <label>
                                        Coordinator Name
                                    </label>

                                    <input
                                        id="coordName"
                                        type="text"
                                        class="form-control coordinator-input"
                                        readonly
                                        placeholder="Selected school will populate this"
                                    />

                                </div>


                                <!-- Required Hours -->
                                <div class="col-md-6 mb-3">

                                    <label>
                                        Required Hours
                                    </label>

                                    <input
                                        id="requiredHours"
                                        type="number"
                                        class="form-control"
                                        min="0"
                                        placeholder="Enter required hours"
                                    />

                                </div>

                            </div>


                            <!-- Hidden fields -->
                            <input
                                type="hidden"
                                id="dateStarted"
                            />

                            <input
                                type="hidden"
                                id="validUntil"
                            />



                            <small class="section-title section-title-spacing">
                                Guardian Information
                            </small>

                            <hr class="section-divider" />


                            <!-- GUARDIAN NAME + CONTACT -->
                            <div class="row">

                                <!-- Guardian Name -->
                                <div class="col-md-8 mb-4">

                                    <label>
                                        Name
                                    </label>

                                    <input
                                        id="guardianName"
                                        type="text"
                                        class="form-control"
                                        placeholder=""
                                    />

                                </div>


                                <!-- Guardian Contact -->
                                <div class="col-md-4 mb-4">

                                    <label>
                                        Contact Number
                                    </label>

                                    <input
                                        id="guardianContact"
                                        type="tel"
                                        inputmode="numeric"
                                        pattern="[0-9]*"
                                        class="form-control"
                                        placeholder="09XXXXXXXXX"
                                        @input="sanitizeContactNumber"
                                    />

                                </div>

                            </div>

                        </div>



                        <div class="modal-footer border-0 pt-0">

                            <button
                                type="submit"
                                id="saveOjtApplicant"
                                class="btn btn-success px-4 modern-btn"
                            >
                                <i class="fas fa-save mr-1"></i>
                                Save
                            </button>

                            <button
                                type="button"
                                class="btn btn-outline-secondary px-4"
                                @click="isApplicantModalOpen = false"
                            >
                                <i class="fas fa-ban mr-1"></i>
                                Cancel
                            </button>

                        </div>

                    </form>

                </div>
            </div>
        </div>



        <div
            id="requirementsModal"
            class="modal fade"
            :class="{ show: isRequirementsModalOpen }"
            :style="{ display: isRequirementsModalOpen ? 'block' : 'none' }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isRequirementsModalOpen"
            @click.self="closeRequirementsModal"
        >
            <div class="modal-dialog modal-dialog-centered modal-lg" role="document" @click.stop>
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">Requirements</h5>
                        <button type="button" class="close" aria-label="Close" @click="closeRequirementsModal">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body p-4">
                        <div
                            v-for="(label, index) in reqLabels"
                            :key="label"
                            class="form-check req-item"
                        >
                            <input
                                :id="`requirement-${index}`"
                                v-model="reqChecks[index]"
                                type="checkbox"
                                class="form-check-input"
                            />
                            <label :for="`requirement-${index}`" class="form-check-label">
                                {{ label }}
                            </label>
                        </div>
                    </div>
                    <div class="modal-footer border-0 pt-0">
                        <button
                            type="button"
                            class="btn btn-success px-4 modern-btn"
                            aria-label="Save requirements"
                            title="Save requirements"
                            @click="saveRequirements"
                        >
                            <i class="fas fa-check"></i>
                        </button>
                        <button
                            type="button"
                            class="btn btn-outline-secondary px-4"
                            aria-label="Cancel requirements"
                            title="Cancel requirements"
                            @click="closeRequirementsModal"
                        >
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>


        <div
            id="evaluationModal"
            class="modal fade"
            :class="{ show: isEvaluationModalOpen }"
            :style="{ display: isEvaluationModalOpen ? 'block' : 'none' }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isEvaluationModalOpen"
            @click.self="closeEvaluationModal"
        >
            <div class="modal-dialog modal-dialog-centered evaluation-modal-dialog" role="document" @click.stop>
                <div class="modal-content modern-modal evaluation-modal-content">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">Applicant Evaluation</h5>
                        <button type="button" class="close" aria-label="Close" @click="closeEvaluationModal">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body p-4">
                        <div class="form-group">
                            <label for="evaluationOffice">Assign Office</label>
                            <select id="evaluationOffice" v-model="evalOffice" class="form-control">
                                <option value="" disabled>Select an office</option>
                                <option v-for="office in offices" :key="office.code" :value="office.code">
                                    {{ office.name }}
                                </option>
                            </select>
                        </div>
                        <div class="form-group mb-0">
                            <label for="evaluationRemarks">Remarks</label>
                            <textarea id="evaluationRemarks" v-model="evalRemarks" class="form-control" rows="4"></textarea>
                        </div>
                    </div>
                    <div class="modal-footer border-0 pt-0">
                        <button type="button" class="btn btn-success px-4 modern-btn" aria-label="Save evaluation" title="Save evaluation" @click="saveEvaluation">
                            <i class="fas fa-check"></i>
                        </button>
                        <button type="button" class="btn btn-outline-secondary px-4" aria-label="Cancel evaluation" title="Cancel evaluation" @click="closeEvaluationModal">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div
            id="hireModal"
            class="modal fade"
            :class="{ show: isHireModalOpen }"
            :style="{ display: isHireModalOpen ? 'block' : 'none' }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isHireModalOpen"
            @click.self="closeHireModal"
        >
            <div class="modal-dialog modal-dialog-centered hire-modal-dialog" role="document" @click.stop>
                <div class="modal-content modern-modal hire-modal-content">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">Hire Applicant</h5>
                        <button type="button" class="close" aria-label="Close" @click="closeHireModal">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body p-4">
                        <div class="form-group">
                            <label for="hireRequestNo">Request Number</label>
                            <select id="hireRequestNo" v-model="hireRequestNo" class="form-control" required>
                                <option value="" disabled>Select a request</option>
                                <option v-for="request in requests" :key="request.requestNo" :value="request.requestNo">
                                    {{ request.requestNo }} - {{ request.officeName || request.officeCode }}
                                </option>
                            </select>
                        </div>
                        <div class="form-group mb-0">
                            <label for="hireDate">Hire Date</label>
                            <input id="hireDate" ref="hireDatePickerEl" :value="hireDate" type="date" class="form-control" @input="onHireDatePicked" />
                        </div>
                    </div>
                    <div class="modal-footer border-0 pt-0">
                        <button type="button" class="btn btn-success px-4 modern-btn" aria-label="Hire applicant" title="Hire applicant" @click="submitHire">
                            <i class="fas fa-check"></i>
                        </button>
                        <button type="button" class="btn btn-outline-secondary px-4" aria-label="Cancel hiring" title="Cancel hiring" @click="closeHireModal">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div
            id="viewModal"
            class="modal fade"
            :class="{ show: isViewModalOpen }"
            :style="{
                display: isViewModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isViewModalOpen"
        >

            <div
                class="modal-dialog modal-dialog-centered modal-lg"
                role="document"
            >

                <div
                    v-if="viewApplicantData"
                    class="modal-content modern-modal"
                >

                    <div class="modal-header modern-modal-header">

                        <h5 class="modal-title font-weight-bold">
                            Applicant Details
                        </h5>

                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeViewModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>

                    </div>


                    <div class="modal-body p-4 swal-details">

                        <h4 class="swal-detail-title">
                            {{ viewApplicantData.applicantNo }}
                        </h4>

                        <p>
                            <strong>Name:</strong>
                            {{ fullName(viewApplicantData) }}
                        </p>

                        <p>
                            <strong>Email:</strong>
                            {{ viewApplicantData.email || '-' }}
                        </p>

                        <p>
                            <strong>Contact:</strong>
                            {{ viewApplicantData.contactNumber || '-' }}
                        </p>

                        <p>
                            <strong>Address:</strong>
                            {{ viewApplicantData.houseAddress || '-' }}
                        </p>

                        <hr />

                        <p>
                            <strong>Guardian:</strong>
                            {{ viewApplicantData.guardianName || '-' }}
                            ({{ viewApplicantData.guardianContact || '-' }})
                        </p>

                        <p>
                            <strong>School:</strong>
                            {{ viewApplicantData.schoolName || '-' }}
                        </p>

                        <p>
                            <strong>Education Level:</strong>
                            {{ viewApplicantData.educationLevel || '-' }}
                        </p>

                        <hr />

                        <p>
                            <strong>Coordinator:</strong>
                            {{ viewApplicantData.coordName || '-' }}
                        </p>

                        <p>
                            <strong>Required Hours:</strong>
                            {{ viewApplicantData.requiredHours || '-' }}
                        </p>

                        <hr />

                        <strong>Requirements</strong>

                        <ul class="swal-detail-list">

                            <li
                                v-for="(label, idx) in reqLabels"
                                :key="idx"
                            >
                                <strong>{{ label }}</strong>:

                                <span
                                    :class="
                                        (viewApplicantData.requirements || [])[idx]
                                            ? 'req-status provided'
                                            : 'req-status missing'
                                    "
                                >
                                    {{
                                        (viewApplicantData.requirements || [])[idx]
                                            ? 'Provided'
                                            : 'Missing'
                                    }}
                                </span>
                            </li>

                        </ul>

                        <hr />

                        <p>
                            <strong>Workflow:</strong>

                            <span
                                class="badge"
                                :class="
                                    statusInfo(viewApplicantData).badgeClass
                                "
                            >
                                {{ statusInfo(viewApplicantData).label }}
                            </span>
                        </p>

                    </div>


                    <div class="modal-footer border-0 pt-0">

                        <button
                            type="button"
                            class="btn btn-outline-secondary px-4"
                            @click="closeViewModal"
                        >
                            Close
                        </button>

                    </div>

                </div>

            </div>
        </div>


        <div
            v-if="anyModalOpen"
            class="modal-backdrop fade show"
            @click="closeAllModals"
        ></div>

    </section>
</template>


<script setup>
import {
    onMounted,
    onBeforeUnmount,
    ref,
    computed
} from 'vue'

import '../assets/css/Applicants.css'


const APPLICANTS_KEY = 'ojt_applicants_v1'



const reqLabels = [
    'Certificate of Good Moral',
    'One copy of School Registration Form (Certified by School Registrar)',
    'Two copies of most recent 1x1 ID picture with white background',
    'Medical Certificate',
    'COA Training Program Agreement',
    'Parent/Guardian consent with a photocopy of valid ID'
]



const isApplicantModalOpen = ref(false)



const applicants = ref([])

const openDropdown = ref(null)
const openDropdownUp = ref(false)
const dropdownStyle = ref({})



const isRequirementsModalOpen = ref(false)

const reqModalApplicantNo = ref(null)

const reqChecks = ref([
    false,
    false,
    false,
    false,
    false,
    false
])



const isEvaluationModalOpen = ref(false)

const evalApplicantNo = ref(null)

const evalOffice = ref('')

const evalRemarks = ref('')

const offices = ref([
    { code: 'ITO', name: 'AS - INFORMATION TECHNOLOGY OFFICE' }
])

const requests = ref([])



const isHireModalOpen = ref(false)

const hireApplicantNo = ref(null)

const hireRequestNo = ref('')

const hireDate = ref('')

const hireDatePickerEl = ref(null)

let pendingAction = null



const isViewModalOpen = ref(false)

const viewApplicantData = ref(null)



const anyModalOpen = computed(() =>
    isApplicantModalOpen.value ||
    isRequirementsModalOpen.value ||
    isEvaluationModalOpen.value ||
    isHireModalOpen.value ||
    isViewModalOpen.value
)



function closeAllModals() {

    isApplicantModalOpen.value = false

    isRequirementsModalOpen.value = false

    isEvaluationModalOpen.value = false

    isHireModalOpen.value = false

    isViewModalOpen.value = false

    openDropdown.value = null
}



function loadApplicants() {

    try {

        applicants.value = JSON.parse(
            localStorage.getItem(APPLICANTS_KEY) || '[]'
        )

    } catch (error) {

        console.error(
            'Failed to load saved applicants:',
            error
        )

        applicants.value = []
    }
}



function persist() {

    localStorage.setItem(
        APPLICANTS_KEY,
        JSON.stringify(applicants.value)
    )
}



function fullName(applicant) {

    return `${applicant.lastName || ''}, ${applicant.firstName || ''}` +
        (applicant.middleName
            ? ` ${applicant.middleName}`
            : '') +
        (applicant.suffix
            ? ` ${applicant.suffix}`
            : '')
}



function reqCount(applicant) {

    return (applicant.requirements || [])
        .filter(Boolean)
        .length
}



function statusInfo(applicant) {

    const count = reqCount(applicant)

    if (count < 6) {

        return {
            label: 'Incomplete',
            badgeClass: 'badge-warning'
        }
    }

    if (applicant.hired) {

        return {
            label: 'Hired',
            badgeClass: 'badge-success'
        }
    }

    if (applicant.accepted) {

        return {
            label: 'Accepted',
            badgeClass: 'badge-success'
        }
    }

    if (applicant.rejected) {

        return {
            label: 'Rejected',
            badgeClass: 'badge-danger'
        }
    }

    return {
        label: 'Completed',
        badgeClass: 'badge-info'
    }
}



function toggleDropdown(applicantNo, event) {

    openDropdown.value =
        openDropdown.value === applicantNo
            ? null
            : applicantNo

    if (openDropdown.value === applicantNo) {
        const buttonRect =
            event.currentTarget.getBoundingClientRect()
        const menuWidth = 176
        const menuHeight = 190
        const gap = 6

        openDropdownUp.value =
            window.innerHeight - buttonRect.bottom < menuHeight + gap

        dropdownStyle.value = {
            top: `${
                openDropdownUp.value
                    ? buttonRect.top - menuHeight - gap
                    : buttonRect.bottom + gap
            }px`,
            left: `${Math.max(
                8,
                Math.min(
                    buttonRect.right - menuWidth,
                    window.innerWidth - menuWidth - 8
                )
            )}px`
        }
    } else {
        openDropdownUp.value = false
        dropdownStyle.value = {}
    }
}


function handleOutsideClick() {
    openDropdown.value = null
    openDropdownUp.value = false
    dropdownStyle.value = {}
}


function sanitizeContactNumber(event) {
    event.target.value =
        event.target.value.replace(/\D/g, '')
}


function saveApplicant() {

    const value = (id) =>
        document.getElementById(id)?.value?.trim() || ''


    const firstName = value('firstName')

    const lastName = value('lastName')

    const schoolName = value('schoolName')

    const educationLevel = value('educationLevel')

    const contactNumber = value('contactNumber')


    /* Required validation */
    if (!firstName) {

        alert('Please enter the First Name.')

        return
    }


    if (!lastName) {

        alert('Please enter the Last Name.')

        return
    }

    if (contactNumber && !/^\d+$/.test(contactNumber)) {
        alert('Please enter a valid contact number using numbers only.')

        return
    }


    if (!schoolName) {

        alert('Please select a School Name.')

        return
    }


    /* Generate applicant number */
    const nextNumber =
        applicants.value.length + 1


    const applicant = {

        applicantNo:
            `A-${new Date().getFullYear()}-${String(nextNumber).padStart(3, '0')}`,

        firstName,

        middleName:
            value('middleName'),

        lastName,

        suffix:
            value('suffix'),

        email:
            value('email'),

        contactNumber,

        houseAddress:
            value('houseAddress'),

        schoolName,

        educationLevel,

        program:
            educationLevel || schoolName,

        coordName:
            value('coordName'),

        requiredHours:
            Number(value('requiredHours')) || 0,

        guardianName:
            value('guardianName'),

        guardianContact:
            value('guardianContact'),

        requirements: [
            false,
            false,
            false,
            false,
            false,
            false
        ],

        accepted: false,

        rejected: false,

        hired: false,

        createdAt:
            new Date().toISOString()
    }


    applicants.value.push(applicant)

    persist()


    /* Reset form */
    document
        .getElementById('ojtForm')
        ?.reset()


    isApplicantModalOpen.value = false


    alert('Applicant saved successfully.')
}



function openRequirementsModal(applicant) {

    openDropdown.value = null

    reqModalApplicantNo.value =
        applicant.applicantNo


    const reqs =
        applicant.requirements ||
        [
            false,
            false,
            false,
            false,
            false,
            false
        ]


    reqChecks.value =
        reqLabels.map(
            (_, idx) => !!reqs[idx]
        )


    isRequirementsModalOpen.value = true
}


function closeRequirementsModal() {

    isRequirementsModalOpen.value = false

    reqModalApplicantNo.value = null
}


function saveRequirements() {

    const applicant =
        applicants.value.find(
            a =>
                a.applicantNo ===
                reqModalApplicantNo.value
        )


    if (!applicant) {

        closeRequirementsModal()

        return
    }


    applicant.requirements =
        [...reqChecks.value]


    persist()


    const count =
        reqCount(applicant)


    closeRequirementsModal()


    if (
        count === 6 &&
        !applicant.accepted &&
        !applicant.rejected
    ) {

        const confirmAccept =
            confirm(
                'All requirements are completed for this applicant. Accept the applicant and move to scheduling?'
            )


        if (confirmAccept) {

            applicant.accepted = true

            applicant.rejected = false

            persist()
        }
    }
}



function openEvaluation(applicant) {

    openDropdown.value = null

    evalApplicantNo.value =
        applicant.applicantNo

    const savedOffice = applicant.applicantOffice || ''
    const matchingOffice = offices.value.find(
        office => office.code === savedOffice || office.name === savedOffice
    )
    evalOffice.value = matchingOffice?.code || ''

    evalRemarks.value =
        applicant.remarks || ''

    isEvaluationModalOpen.value = true
}


function closeEvaluationModal() {

    isEvaluationModalOpen.value = false

    evalApplicantNo.value = null

    evalOffice.value = ''

    evalRemarks.value = ''
}


function nextRequestNo() {

    const now = new Date()

    const y = now.getFullYear()

    const m =
        String(now.getMonth() + 1)
            .padStart(2, '0')

    const d =
        String(now.getDate())
            .padStart(2, '0')

    const seq =
        applicants.value.filter(
            a => a.requestNo
        ).length + 1


    return `RQ-${y}${m}${d}-${String(seq).padStart(3, '0')}`
}



function saveEvaluation() {

    if (!evalOffice.value) {

        alert('Please select an office.')

        return
    }


    const applicantNo =
        evalApplicantNo.value


    pendingAction = {

        applicantNo,

        office:
            evalOffice.value,

        remarks:
            evalRemarks.value,

    }


    closeEvaluationModal()


    openHire(
        applicants.value.find(
            a =>
                a.applicantNo ===
                applicantNo
        ),
        pendingAction
    )
}



function openHire(
    applicant,
    presetData = {}
) {

    if (!applicant) return

    openDropdown.value = null

    hireApplicantNo.value =
        applicant.applicantNo

    hireRequestNo.value =
        presetData.requestNo || ''

    hireDate.value =
        presetData.date || ''

    isHireModalOpen.value = true
}


function closeHireModal() {

    isHireModalOpen.value = false

    hireApplicantNo.value = null

    hireRequestNo.value = ''

    pendingAction = null
}


function openHireDatePicker() {

    const el =
        hireDatePickerEl.value


    if (!el) return


    if (
        hireDate.value &&
        !isNaN(
            Date.parse(
                hireDate.value
            )
        )
    ) {

        el.value =
            hireDate.value
    }


    if (
        typeof el.showPicker ===
        'function'
    ) {

        el.showPicker()

    } else {

        el.click()
    }
}


function onHireDatePicked(e) {

    hireDate.value =
        e.target.value
}


function submitHire() {

    if (
        !hireRequestNo.value ||
        !hireDate.value
    ) {

        alert(
            'Request number and date are required.'
        )

        return
    }


    if (
        isNaN(
            Date.parse(
                hireDate.value
            )
        )
    ) {

        alert(
            'Please enter a valid date.'
        )

        return
    }


    const applicant =
        applicants.value.find(
            a =>
                a.applicantNo ===
                hireApplicantNo.value
        )


    if (applicant) {

        if (
            pendingAction &&
            pendingAction.office
        ) {

            applicant.applicantOffice =
                pendingAction.office
        }


        if (
            pendingAction &&
            pendingAction.remarks
        ) {

            applicant.remarks =
                pendingAction.remarks
        }


        applicant.requestNo =
            hireRequestNo.value

        applicant.actionDate =
            hireDate.value

        applicant.accepted = true

        applicant.hired = true

        applicant.rejected = false

        persist()
    }


    closeHireModal()
}



function viewApplicantDetails(applicant) {

    openDropdown.value = null

    viewApplicantData.value =
        applicant

    isViewModalOpen.value = true
}


function closeViewModal() {

    isViewModalOpen.value = false

    viewApplicantData.value = null
}


function deleteApplicant(applicant) {

    openDropdown.value = null


    const confirmed =
        confirm(
            `Are you sure you want to delete "${applicant.lastName}, ${applicant.firstName}"?`
        )


    if (!confirmed) return


    applicants.value =
        applicants.value.filter(
            a =>
                a.applicantNo !==
                applicant.applicantNo
        )


    persist()
}



onMounted(() => {
    loadApplicants()

    try {
        requests.value = JSON.parse(
            localStorage.getItem('ojt_intern_requests_v1') || '[]'
        )
    } catch (error) {
        console.error('Failed to load saved requests:', error)
        requests.value = []
    }

    fetch('/assets/json/offices.json')
        .then(response => {
            if (!response.ok) {
                throw new Error(`Failed to load offices.json: ${response.status}`)
            }
            return response.json()
        })
        .then(data => {
            offices.value = data.offices || []
        })
        .catch(error => {
            console.error('Failed to load offices:', error)
        })

    document.addEventListener(
        'click',
        handleOutsideClick
    )
})


onBeforeUnmount(() => {

    document.removeEventListener(
        'click',
        handleOutsideClick
    )
})
</script>

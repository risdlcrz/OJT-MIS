<template>
    <section class="content">

        <!-- Tabs -->
        <div class="btn-group mb-3" role="group" aria-label="Applicants view switcher">
            <button
                type="button"
                class="btn"
                :class="activeTab === 'list' ? 'btn-primary' : 'btn-outline-primary'"
                @click="setTab('list')"
            >
                <i class="far fa-file mr-1"></i>
                List of Applicants
            </button>
            <button
                type="button"
                class="btn"
                :class="activeTab === 'schedule' ? 'btn-primary' : 'btn-outline-primary'"
                @click="setTab('schedule')"
            >
                <i class="fas fa-calendar-alt mr-1"></i>
                Schedule Orientation
            </button>
        </div>


        <!-- ================= LIST OF APPLICANTS TAB ================= -->
        <div v-if="activeTab === 'list'">
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
                    <div class="applicant-table-scroll" role="region" aria-label="Applicants list">
                        <table id="example1" class="table table-hover modern-table">
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
                                        <div class="btn-group action-dropdown">

                                            <button
                                                type="button"
                                                class="btn btn-outline-secondary btn-sm dropdown-toggle"
                                                @click.stop="toggleDropdown(applicant.applicantNo, $event)"
                                            >
                                                Actions
                                            </button>

                                            <Teleport to="body">
                                                <div
                                                    v-if="openDropdown === applicant.applicantNo"
                                                    class="dropdown-menu dropdown-menu-right show applicant-action-menu"
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
                                                        @click="viewApplicantDetails(applicant)"
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
                                        </div>
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
            </div>
        </div>


        <!-- ================= SCHEDULE ORIENTATION TAB ================= -->
        <div v-if="activeTab === 'schedule'">
            <div class="card modern-card">

                <div class="card-header modern-card-header">
                    <div>
                        <h3 class="card-title">Schedule Orientation</h3>
                    </div>
                </div>

                <div class="card-body">
                    <div class="applicant-table-scroll" role="region" aria-label="Schedule orientation list">
                        <table id="example2" class="table table-hover modern-table">
                            <thead class="text-center">
                                <tr>
                                    <th>OJT NO.</th>
                                    <th>LAST NAME</th>
                                    <th>FIRST NAME</th>
                                    <th>SCHEDULE</th>
                                    <th>OFFICE</th>
                                    <th>ACTION</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr
                                    v-for="applicant in scheduleCandidates"
                                    :key="applicant.applicantNo"
                                >
                                    <td>{{ applicant.applicantNo }}</td>
                                    <td>{{ applicant.lastName }}</td>
                                    <td>{{ applicant.firstName }}</td>
                                    <td>{{ scheduleLabel(applicant) }}</td>
                                    <td>{{ (applicant.orientation && applicant.orientation.office) || '-' }}</td>
                                    <td>
                                        <div class="d-flex justify-content-center" style="gap: 6px">
                                            <button
                                                type="button"
                                                class="btn btn-outline-primary btn-sm d-inline-flex flex-column align-items-center p-2"
                                                style="min-width: 60px"
                                                @click="openScheduleModal(applicant)"
                                            >
                                                <i class="fas fa-calendar-plus fa-lg mb-1"></i>
                                                <span style="font-size: 0.7rem; font-weight: bold; text-transform: uppercase">Assign</span>
                                            </button>

                                            <button
                                                v-if="applicant.orientation && applicant.orientation.date"
                                                type="button"
                                                class="btn btn-outline-success btn-sm d-inline-flex flex-column align-items-center p-2"
                                                style="min-width: 60px"
                                                @click="confirmOrientation(applicant)"
                                            >
                                                <i class="fas fa-check fa-lg mb-1"></i>
                                                <span style="font-size: 0.7rem; font-weight: bold; text-transform: uppercase">Confirm</span>
                                            </button>

                                            <button
                                                type="button"
                                                class="btn btn-outline-info btn-sm d-inline-flex flex-column align-items-center p-2"
                                                style="min-width: 60px"
                                                @click="viewApplicantDetails(applicant)"
                                            >
                                                <i class="fas fa-eye fa-lg mb-1"></i>
                                                <span style="font-size: 0.7rem; font-weight: bold; text-transform: uppercase">View</span>
                                            </button>
                                        </div>
                                    </td>
                                </tr>

                                <!-- Empty State -->
                                <tr v-if="scheduleCandidates.length === 0">
                                    <td colspan="6" class="text-center text-muted py-4">
                                        No applicants ready for orientation yet.
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>


        <!-- ================= ADD APPLICANT MODAL ================= -->
        <div
            id="ojtModal"
            class="modal fade applicant-modal"
            :class="{ show: isApplicantModalOpen }"
            :style="{
                display: isApplicantModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isApplicantModalOpen"
        >
            <div
                class="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable applicant-modal-dialog"
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
                                    <label>First Name</label>

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
                                    <label>Middle Name</label>

                                    <input
                                        id="middleName"
                                        type="text"
                                        class="form-control"
                                        placeholder="Middle Name"
                                    />
                                </div>

                                <!-- Last Name -->
                                <div class="col-md-4 mb-3">
                                    <label>Last Name</label>

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
                                    <label>Suffix</label>

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
                                    <label>Email Address</label>

                                    <input
                                        id="email"
                                        type="email"
                                        class="form-control"
                                        placeholder="email@example.com"
                                    />
                                </div>

                                <!-- Contact -->
                                <div class="col-md-4 mb-3">
                                    <label>Contact Number</label>

                                    <input
                                        id="contactNumber"
                                        type="text"
                                        class="form-control"
                                        placeholder="0912..."
                                    />
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-12 mb-4">
                                    <label>House Address</label>

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
                                    <label>School Name</label>

                                    <select
                                        id="schoolName"
                                        class="form-control"
                                        required
                                    >
                                        <option value="" selected disabled>
                                            Select School
                                        </option>

                                        <option> Philippine Science High School </option>
                                        <option> National High School </option>
                                        <option> Metro Business College </option>
                                        <option> De La Salle University </option>
                                        <option> Ateneo De Manila University </option>
                                        <option> Far Eastern University </option>
                                        <option> University of Santo Tomas </option>
                                        <option> University of the Philippinnes </option>
                                        <option> Polytechnic University </option>
                                        <option> Local State University </option>
                                        <option> Other </option>
                                    </select>
                                </div>

                                <!-- Course -->
                                <div class="col-md-6 mb-3">
                                    <label>Course</label>

                                    <select
                                        id="educationLevel"
                                        class="form-control"
                                    >
                                        <option value="" selected disabled>
                                            Select Course
                                        </option>

                                        <option> BS Computer Science </option>
                                        <option> BS Information Technology </option>
                                        <option> BS Business Administration </option>
                                        <option> BS Accountancy </option>
                                        <option> BS Computer Engineering </option>
                                        <option> BS Electrical Engineering </option>
                                        <option> Other </option>
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
                                <div class="col-md-8 mb-3">
                                    <label>Coordinator Name</label>

                                    <input
                                        id="coordName"
                                        type="text"
                                        class="form-control coordinator-input"
                                        placeholder="Enter coordinator name"
                                    />
                                </div>

                                <!-- Required Hours -->
                                <div class="col-md-4 mb-3">
                                    <label>Required Hours</label>

                                    <input
                                        id="requiredHours"
                                        type="number"
                                        class="form-control"
                                        min="0"
                                        value="0"
                                    />
                                </div>
                            </div>

                            <!-- Hidden fields -->
                            <input type="hidden" id="dateStarted" />

                            <input type="hidden" id="validUntil" />

                            <small class="section-title section-title-spacing">
                                Guardian Information
                            </small>

                            <hr class="section-divider" />

                            <!-- GUARDIAN NAME + CONTACT -->
                            <div class="row">

                                <!-- Guardian Name -->
                                <div class="col-md-8 mb-4">
                                    <label>Guardian</label>

                                    <input
                                        id="guardianName"
                                        type="text"
                                        class="form-control"
                                        placeholder="Name"
                                    />
                                </div>

                                <!-- Guardian Contact -->
                                <div class="col-md-4 mb-4">
                                    <label>Contact Number</label>

                                    <input
                                        id="guardianContact"
                                        type="text"
                                        class="form-control"
                                        placeholder="0912..."
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


        <!-- ================= REQUIREMENTS MODAL ================= -->
        <div
            id="requirementsModal"
            class="modal fade"
            :class="{ show: isRequirementsModalOpen }"
            :style="{
                display: isRequirementsModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isRequirementsModalOpen"
        >
            <div class="modal-dialog modal-dialog-centered modal-lg" role="document">
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">
                            Applicant Requirements
                        </h5>
                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeRequirementsModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <form @submit.prevent="saveRequirements">
                        <div class="modal-body p-4">
                            <p class="text-muted">
                                Applicant No.:
                                <strong>{{ reqModalApplicantNo }}</strong>
                            </p>

                            <div
                                v-for="(label, idx) in reqLabels"
                                :key="label"
                                class="custom-control custom-checkbox mb-3"
                            >
                                <input
                                    :id="`requirement-${idx}`"
                                    v-model="reqChecks[idx]"
                                    type="checkbox"
                                    class="custom-control-input"
                                />
                                <label
                                    class="custom-control-label"
                                    :for="`requirement-${idx}`"
                                >
                                    {{ label }}
                                </label>
                            </div>
                        </div>

                        <div class="modal-footer border-0 pt-0">
                            <button type="submit" class="btn btn-success px-4 modern-btn">
                                <i class="fas fa-save mr-1"></i>
                                Save Requirements
                            </button>
                            <button
                                type="button"
                                class="btn btn-outline-secondary px-4"
                                @click="closeRequirementsModal"
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>


        <!-- ================= EVALUATION MODAL ================= -->
        <div
            id="evaluationModal"
            class="modal fade action-side-panel"
            :class="{ show: isEvaluationModalOpen }"
            :style="{
                display: isEvaluationModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isEvaluationModalOpen"
        >
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">
                            Applicant Evaluation
                        </h5>
                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeEvaluationModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <form @submit.prevent="saveEvaluation">
                        <div class="modal-body p-4">
                            <p class="text-muted">
                                Applicant No.:
                                <strong>{{ evalApplicantNo }}</strong>
                            </p>

                            <div class="form-group">
                                <label for="evaluationOffice">
                                    Assigned Office
                                </label>
                                <select
                                    id="evaluationOffice"
                                    v-model="evalOffice"
                                    class="form-control"
                                    required
                                >
                                    <option value="" disabled>
                                        Select Office
                                    </option>
                                    <option
                                        v-for="office in offices"
                                        :key="office"
                                        :value="office"
                                    >
                                        {{ office }}
                                    </option>
                                </select>
                            </div>

                            <div class="form-group mb-0">
                                <label for="evaluationRemarks">
                                    Remarks
                                </label>
                                <textarea
                                    id="evaluationRemarks"
                                    v-model="evalRemarks"
                                    class="form-control"
                                    rows="4"
                                    placeholder="Enter evaluation remarks"
                                ></textarea>
                            </div>
                        </div>

                        <div class="modal-footer border-0 pt-0">
                            <button type="submit" class="btn btn-success px-4 modern-btn">
                                <i class="fas fa-check mr-1"></i>
                                Continue to Hire
                            </button>
                            <button
                                type="button"
                                class="btn btn-outline-secondary px-4"
                                @click="closeEvaluationModal"
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>


        <!-- ================= HIRE MODAL ================= -->
        <div
            id="hireModal"
            class="modal fade action-side-panel"
            :class="{ show: isHireModalOpen }"
            :style="{
                display: isHireModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isHireModalOpen"
        >
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">
                            Hire Applicant
                        </h5>
                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeHireModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <form @submit.prevent="submitHire">
                        <div class="modal-body p-4">
                            <p class="text-muted">
                                Applicant No.:
                                <strong>{{ hireApplicantNo }}</strong>
                            </p>

                            <div class="form-group">
                                <label for="hireRequestNo">
                                    Request Number
                                </label>
                                <input
                                    id="hireRequestNo"
                                    v-model="hireRequestNo"
                                    type="text"
                                    class="form-control"
                                    placeholder="e.g. RQ-20260101-001"
                                    required
                                />
                            </div>

                            <div class="form-group mb-0">
                                <label for="hireDate">
                                    Hire Date
                                </label>
                                <input
                                    id="hireDate"
                                    v-model="hireDate"
                                    type="date"
                                    class="form-control"
                                    required
                                />
                            </div>
                        </div>

                        <div class="modal-footer border-0 pt-0">
                            <button type="submit" class="btn btn-success px-4 modern-btn">
                                <i class="fas fa-user-check mr-1"></i>
                                Confirm Hire
                            </button>
                            <button
                                type="button"
                                class="btn btn-outline-secondary px-4"
                                @click="closeHireModal"
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>


        <!-- ================= VIEW MODAL ================= -->
        <div
            id="viewModal"
            class="modal fade action-side-panel"
            :class="{ show: isViewModalOpen }"
            :style="{
                display: isViewModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isViewModalOpen"
        >
            <div class="modal-dialog modal-dialog-centered modal-lg" role="document">

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

                        <p>
                            <strong>Program:</strong>
                            {{ viewApplicantData.program || '-' }}
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

                        <p>
                            <strong>Request Number:</strong>
                            {{ viewApplicantData.requestNo || '-' }}
                        </p>

                        <p>
                            <strong>Office:</strong>
                            {{ viewApplicantData.applicantOffice || '-' }}
                        </p>

                        <p>
                            <strong>Remarks:</strong>
                            {{ viewApplicantData.remarks || '-' }}
                        </p>

                        <p>
                            <strong>Action Date:</strong>
                            {{ viewApplicantData.actionDate || '-' }}
                        </p>

                        <template v-if="viewApplicantData.orientation">
                            <hr />
                            <p>
                                <strong>Orientation Date:</strong>
                                {{ viewApplicantData.orientation.date || '-' }}
                            </p>
                            <p>
                                <strong>Orientation Time:</strong>
                                {{ viewApplicantData.orientation.time || '-' }}
                            </p>
                            <p>
                                <strong>Orientation Office:</strong>
                                {{ viewApplicantData.orientation.office || '-' }}
                            </p>
                            <p>
                                <strong>Orientation Confirmed:</strong>
                                {{ viewApplicantData.orientation.confirmed ? 'Yes' : 'No' }}
                            </p>
                        </template>

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
                                :class="statusInfo(viewApplicantData).badgeClass"
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


        <!-- ================= DELETE MODAL ================= -->
        <div
            id="deleteModal"
            class="modal fade action-side-panel"
            :class="{ show: isDeleteModalOpen }"
            :style="{
                display: isDeleteModalOpen ? 'block' : 'none'
            }"
            role="dialog"
            :aria-hidden="!isDeleteModalOpen"
        >
            <div class="modal-dialog modal-dialog-centered" role="document">
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">
                            Delete Applicant
                        </h5>
                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeDeleteModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <div class="modal-body p-4">
                        <p class="mb-0">
                            Are you sure you want to delete
                            <strong>
                                {{ deleteApplicantData?.lastName }},
                                {{ deleteApplicantData?.firstName }}
                            </strong>
                            ?
                        </p>
                    </div>

                    <div class="modal-footer border-0 pt-0">
                        <button
                            type="button"
                            class="btn btn-danger px-4"
                            @click="confirmDeleteApplicant"
                        >
                            <i class="fas fa-trash mr-1"></i>
                            Delete
                        </button>
                        <button
                            type="button"
                            class="btn btn-outline-secondary px-4"
                            @click="closeDeleteModal"
                        >
                            Cancel
                        </button>
                    </div>
                </div>
            </div>
        </div>


        <!-- ================= SCHEDULE ORIENTATION MODAL ================= -->
        <div
            id="scheduleModal"
            class="modal fade"
            :class="{ show: isScheduleModalOpen }"
            :style="{
                display: isScheduleModalOpen ? 'block' : 'none'
            }"
            tabindex="-1"
            role="dialog"
            :aria-hidden="!isScheduleModalOpen"
        >
            <div class="modal-dialog modal-lg modal-dialog-centered" role="document">
                <div class="modal-content modern-modal">
                    <div class="modal-header modern-modal-header">
                        <h5 class="modal-title font-weight-bold">
                            Assign Orientation Schedule
                        </h5>
                        <button
                            type="button"
                            class="close"
                            aria-label="Close"
                            @click="closeScheduleModal"
                        >
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>

                    <form @submit.prevent="saveSchedule">
                        <div class="modal-body p-4">
                            <p class="text-muted">
                                Applicant No.:
                                <strong>{{ schedApplicantNo }}</strong>
                            </p>

                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="orientationDate">Date</label>
                                    <input
                                        id="orientationDate"
                                        v-model="orientationDate"
                                        type="date"
                                        class="form-control"
                                        required
                                    />
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="orientationTime">Time</label>
                                    <input
                                        id="orientationTime"
                                        v-model="orientationTime"
                                        type="time"
                                        class="form-control"
                                    />
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="orientationOffice">Office</label>
                                    <select
                                        id="orientationOffice"
                                        v-model="orientationOffice"
                                        class="form-control"
                                        required
                                    >
                                        <option value="" disabled>
                                            {{ officesLoading ? 'Loading offices...' : (scheduleOffices.length ? 'Select office' : 'No offices found') }}
                                        </option>
                                        <option
                                            v-for="office in scheduleOffices"
                                            :key="office"
                                            :value="office"
                                        >
                                            {{ office }}
                                        </option>
                                    </select>
                                </div>
                            </div>
                        </div>

                        <div class="modal-footer border-0 pt-0">
                            <button type="submit" class="btn btn-primary px-4 modern-btn">
                                Save Schedule
                            </button>
                            <button
                                type="button"
                                class="btn btn-outline-secondary px-4"
                                @click="closeScheduleModal"
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>


        <!-- Backdrop (side panels have none) -->
        <div
            v-if="anyModalOpen && !isEvaluationModalOpen && !isHireModalOpen && !isViewModalOpen && !isDeleteModalOpen"
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

const OFFICES_JSON = '/assets/json/offices.json'

/* Fallback office list, used if offices.json is missing or empty,
   so the Office field is never stuck with nothing to pick from. */
const FALLBACK_OFFICES = [
    'HR Office',
    'Records Office',
    'IT Office',
    'Finance Office',
    'Administration Office'
]


/* Which tab is showing: 'list' (List of Applicants) or 'schedule'
   (Schedule Orientation). No routing involved, just local state. */
const activeTab = ref('list')


/* Sinusundan ng tab ang URL: "#schedule" ay bubukas sa Schedule Orientation.
   Ginagamit ito ng sidebar links. */
function readTabFromUrl() {
    activeTab.value =
        window.location.hash === '#schedule' ? 'schedule' : 'list'
}

function setTab(tab) {
    activeTab.value = tab

    const url =
        window.location.pathname +
        (tab === 'schedule' ? '#schedule' : '')

    window.history.replaceState(null, '', url)

    /* replaceState hindi nagti-trigger ng hashchange; ipaalam sa sidebar
       (BaseLayout) para mag-update ang highlight at breadcrumb. */
    window.dispatchEvent(new HashChangeEvent('hashchange'))
}


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

const dropdownStyle = ref({})


/* ---- Requirements state ---- */

const isRequirementsModalOpen = ref(false)

const reqModalApplicantNo = ref(null)

const reqChecks = ref([false, false, false, false, false, false])


/* ---- Evaluation state ---- */

const isEvaluationModalOpen = ref(false)

const evalApplicantNo = ref(null)

const evalOffice = ref('')

const evalRemarks = ref('')

const offices = ref([...FALLBACK_OFFICES])


/* ---- Hire state ---- */

const isHireModalOpen = ref(false)

const hireApplicantNo = ref(null)

const hireRequestNo = ref('')

const hireDate = ref('')

const hireDatePickerEl = ref(null)

let pendingAction = null


/* ---- View / Delete state ---- */

const isViewModalOpen = ref(false)

const viewApplicantData = ref(null)

const isDeleteModalOpen = ref(false)

const deleteApplicantData = ref(null)


/* ---- Schedule Orientation state ---- */

const isScheduleModalOpen = ref(false)

const schedApplicantNo = ref(null)

const orientationDate = ref('')

const orientationTime = ref('')

const orientationOffice = ref('')

const scheduleOffices = ref([])

const officesLoading = ref(true)


/* Applicants who are accepted but not yet confirmed for orientation */
const scheduleCandidates = computed(() =>
    applicants.value.filter(
        a => a.accepted && !(a.orientation && a.orientation.confirmed)
    )
)


const anyModalOpen = computed(() =>
    isApplicantModalOpen.value ||
    isRequirementsModalOpen.value ||
    isEvaluationModalOpen.value ||
    isHireModalOpen.value ||
    isViewModalOpen.value ||
    isDeleteModalOpen.value ||
    isScheduleModalOpen.value
)


function closeAllModals() {
    isApplicantModalOpen.value = false
    isRequirementsModalOpen.value = false
    isEvaluationModalOpen.value = false
    isHireModalOpen.value = false
    isViewModalOpen.value = false
    isDeleteModalOpen.value = false
    isScheduleModalOpen.value = false

    deleteApplicantData.value = null
    openDropdown.value = null
}


function loadApplicants() {
    try {
        applicants.value = JSON.parse(
            localStorage.getItem(APPLICANTS_KEY) || '[]'
        )
    } catch (error) {
        console.error('Failed to load saved applicants:', error)

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
        (applicant.middleName ? ` ${applicant.middleName}` : '') +
        (applicant.suffix ? ` ${applicant.suffix}` : '')
}


function reqCount(applicant) {
    return (applicant.requirements || []).filter(Boolean).length
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
        const buttonRect = event.currentTarget.getBoundingClientRect()
        const menuWidth = 192

        dropdownStyle.value = {
            top: `${buttonRect.bottom + 6}px`,
            left: `${Math.max(8, buttonRect.right - menuWidth)}px`
        }
    }
}


function handleOutsideClick() {
    openDropdown.value = null
}


function closeActionPanels() {
    isEvaluationModalOpen.value = false
    isHireModalOpen.value = false
    isViewModalOpen.value = false
    isDeleteModalOpen.value = false
    isScheduleModalOpen.value = false

    viewApplicantData.value = null
    deleteApplicantData.value = null
}


function saveApplicant() {
    const value = (id) =>
        document.getElementById(id)?.value?.trim() || ''

    const firstName = value('firstName')
    const lastName = value('lastName')
    const schoolName = value('schoolName')
    const educationLevel = value('educationLevel')

    /* Required validation */
    if (!firstName) {
        alert('Please enter the First Name.')
        return
    }

    if (!lastName) {
        alert('Please enter the Last Name.')
        return
    }

    if (!schoolName) {
        alert('Please select a School Name.')
        return
    }

    /* Generate applicant number */
    const nextNumber = applicants.value.length + 1

    const applicant = {
        applicantNo:
            `A-${new Date().getFullYear()}-${String(nextNumber).padStart(3, '0')}`,

        firstName,
        middleName: value('middleName'),
        lastName,
        suffix: value('suffix'),
        email: value('email'),
        contactNumber: value('contactNumber'),
        houseAddress: value('houseAddress'),
        schoolName,
        educationLevel,
        program: educationLevel || schoolName,
        coordName: value('coordName'),
        requiredHours: Number(value('requiredHours')) || 0,
        guardianName: value('guardianName'),
        guardianContact: value('guardianContact'),

        requirements: [false, false, false, false, false, false],

        accepted: false,
        rejected: false,
        hired: false,
        orientation: null,

        createdAt: new Date().toISOString()
    }

    applicants.value.push(applicant)

    persist()

    /* Reset form */
    document.getElementById('ojtForm')?.reset()

    isApplicantModalOpen.value = false

    alert('Applicant saved successfully.')
}


/* ---- Requirements ---- */

function openRequirementsModal(applicant) {
    openDropdown.value = null

    reqModalApplicantNo.value = applicant.applicantNo

    const reqs =
        applicant.requirements ||
        [false, false, false, false, false, false]

    reqChecks.value = reqLabels.map((_, idx) => !!reqs[idx])

    isRequirementsModalOpen.value = true
}


function closeRequirementsModal() {
    isRequirementsModalOpen.value = false

    reqModalApplicantNo.value = null
}


function saveRequirements() {
    const applicant = applicants.value.find(
        a => a.applicantNo === reqModalApplicantNo.value
    )

    if (!applicant) {
        closeRequirementsModal()
        return
    }

    applicant.requirements = [...reqChecks.value]

    persist()

    const count = reqCount(applicant)

    closeRequirementsModal()

    if (
        count === 6 &&
        !applicant.accepted &&
        !applicant.rejected
    ) {
        const confirmAccept = confirm(
            'All requirements are completed for this applicant. Accept the applicant and move to scheduling?'
        )

        if (confirmAccept) {
            applicant.accepted = true
            applicant.rejected = false

            persist()
        }
    }
}


/* ---- Evaluation ---- */

function openEvaluation(applicant) {
    closeActionPanels()

    evalApplicantNo.value = applicant.applicantNo
    evalOffice.value = applicant.applicantOffice || ''
    evalRemarks.value = applicant.remarks || ''

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
    const m = String(now.getMonth() + 1).padStart(2, '0')
    const d = String(now.getDate()).padStart(2, '0')

    const seq = applicants.value.filter(a => a.requestNo).length + 1

    return `RQ-${y}${m}${d}-${String(seq).padStart(3, '0')}`
}


function saveEvaluation() {
    if (!evalOffice.value) {
        alert('Please select an office.')
        return
    }

    const applicantNo = evalApplicantNo.value

    pendingAction = {
        applicantNo,
        office: evalOffice.value,
        remarks: evalRemarks.value,
        requestNo: nextRequestNo(),
        date: new Date().toISOString().slice(0, 10)
    }

    /* closeEvaluationModal() and closeActionPanels() don't touch pendingAction,
       but openHire()'s closeHireModal() would, so pass it as preset data. */
    const preset = pendingAction

    closeEvaluationModal()

    openHire(
        applicants.value.find(a => a.applicantNo === applicantNo),
        preset
    )
}


/* ---- Hire ---- */

function openHire(applicant, presetData = {}) {
    if (!applicant) return

    closeActionPanels()

    hireApplicantNo.value = applicant.applicantNo
    hireRequestNo.value = presetData.requestNo || ''
    hireDate.value = presetData.date || ''

    isHireModalOpen.value = true
}


function closeHireModal() {
    isHireModalOpen.value = false

    hireApplicantNo.value = null
    hireRequestNo.value = ''

    pendingAction = null
}


function openHireDatePicker() {
    const el = hireDatePickerEl.value

    if (!el) return

    if (hireDate.value && !isNaN(Date.parse(hireDate.value))) {
        el.value = hireDate.value
    }

    if (typeof el.showPicker === 'function') {
        el.showPicker()
    } else {
        el.click()
    }
}


function onHireDatePicked(e) {
    hireDate.value = e.target.value
}


function submitHire() {
    if (!hireRequestNo.value || !hireDate.value) {
        alert('Request number and date are required.')
        return
    }

    if (isNaN(Date.parse(hireDate.value))) {
        alert('Please enter a valid date.')
        return
    }

    const applicant = applicants.value.find(
        a => a.applicantNo === hireApplicantNo.value
    )

    if (applicant) {
        if (pendingAction && pendingAction.office) {
            applicant.applicantOffice = pendingAction.office
        }

        if (pendingAction && pendingAction.remarks) {
            applicant.remarks = pendingAction.remarks
        }

        applicant.requestNo = hireRequestNo.value
        applicant.actionDate = hireDate.value
        applicant.accepted = true
        applicant.hired = true
        applicant.rejected = false

        persist()
    }

    closeHireModal()
}


/* ---- View / Delete ---- */

function viewApplicantDetails(applicant) {
    closeActionPanels()

    viewApplicantData.value = applicant

    isViewModalOpen.value = true
}


function closeViewModal() {
    isViewModalOpen.value = false

    viewApplicantData.value = null
}


function deleteApplicant(applicant) {
    closeActionPanels()

    deleteApplicantData.value = applicant

    isDeleteModalOpen.value = true
}


function closeDeleteModal() {
    isDeleteModalOpen.value = false

    deleteApplicantData.value = null
}


function confirmDeleteApplicant() {
    const applicant = deleteApplicantData.value

    if (!applicant) {
        closeDeleteModal()
        return
    }

    applicants.value = applicants.value.filter(
        a => a.applicantNo !== applicant.applicantNo
    )

    persist()

    closeDeleteModal()
}


/* ---- Schedule Orientation functions ---- */

async function loadScheduleOffices() {
    officesLoading.value = true

    try {
        const res = await fetch(OFFICES_JSON)

        if (!res.ok) throw new Error(`HTTP ${res.status}`)

        const data = await res.json()
        const raw = Array.isArray(data) ? data : (data.offices || [])

        const list = raw
            .map(o =>
                typeof o === 'string'
                    ? o
                    : (o.name || o.office || o.officeName || '')
            )
            .filter(Boolean)

        scheduleOffices.value = list.length ? list : [...FALLBACK_OFFICES]
    } catch (error) {
        console.error('Failed to load offices.json:', error)

        scheduleOffices.value = [...FALLBACK_OFFICES]
    } finally {
        officesLoading.value = false
    }
}


function scheduleLabel(applicant) {
    const o = applicant.orientation

    if (!o || !o.date) return 'Not scheduled'

    return o.time ? `${o.date} ${o.time}` : o.date
}


function openScheduleModal(applicant) {
    if (!applicant) return

    closeActionPanels()

    schedApplicantNo.value = applicant.applicantNo
    orientationDate.value = applicant.orientation?.date || ''
    orientationTime.value = applicant.orientation?.time || ''
    orientationOffice.value =
        applicant.orientation?.office ||
        applicant.applicantOffice ||
        ''

    isScheduleModalOpen.value = true
}


function closeScheduleModal() {
    isScheduleModalOpen.value = false

    schedApplicantNo.value = null
    orientationDate.value = ''
    orientationTime.value = ''
    orientationOffice.value = ''
}


function saveSchedule() {
    if (!orientationDate.value || !orientationOffice.value) {
        alert('Please select a date and an office.')
        return
    }

    const applicant = applicants.value.find(
        a => a.applicantNo === schedApplicantNo.value
    )

    if (!applicant) {
        closeScheduleModal()
        return
    }

    applicant.orientation = {
        date: orientationDate.value,
        time: orientationTime.value,
        office: orientationOffice.value,
        confirmed: applicant.orientation?.confirmed || false
    }

    persist()

    closeScheduleModal()
}


function confirmOrientation(applicant) {
    if (!applicant || !applicant.orientation?.date) return

    const ok = confirm(
        `Confirm orientation for ${applicant.lastName}, ${applicant.firstName}?`
    )

    if (!ok) return

    applicant.orientation.confirmed = true

    persist()
}


onMounted(() => {
    readTabFromUrl()
    loadApplicants()
    loadScheduleOffices()

    document.addEventListener('click', handleOutsideClick)
    window.addEventListener('hashchange', readTabFromUrl)
})


onBeforeUnmount(() => {
    document.removeEventListener('click', handleOutsideClick)
    window.removeEventListener('hashchange', readTabFromUrl)
})
</script>
<template>
  <section class="content">
    <!-- ================= API-backed module ================= -->
    <div v-if="config.api" class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">{{ config.title }}</h3>
          <p v-if="config.subtitle" class="ojt-table-subtitle">{{ config.subtitle }}</p>
        </div>
        <div class="ojt-table-tools">
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="load">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
          <button v-if="!config.hiring" type="button" class="btn btn-primary btn-sm" :disabled="isLoading" @click="openCreate">
            <i class="fas fa-plus me-1"></i> Add {{ config.singular }}
          </button>
        </div>
      </div>

      <div v-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="search" type="search" class="form-control form-control-sm"
                 :placeholder="`Search ${(config.title || '').toLowerCase()}`" :aria-label="`Search ${config.title}`" />
        </div>
        <select v-if="statusOptions.length > 1" v-model="statusFilter" class="form-select form-select-sm" aria-label="Filter by status">
          <option value="">All statuses</option>
          <option v-for="s in statusOptions" :key="s" :value="s">{{ statusLabel(s) }}</option>
        </select>
        <span class="ojt-toolbar-count">{{ visibleRows.length }} of {{ rows.length }} {{ (config.title || '').toLowerCase() }}</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th v-for="col in displayColumns" :key="col.prop" scope="col" :class="col.cls">{{ col.label }}</th>
              <th v-if="config.hasActions" scope="col" class="col-actions">Actions</th>
              <th v-if="config.clickableRows" scope="col" class="col-actions" aria-label="Open profile"></th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="tableColspan" :loading="isLoading" :empty="!isLoading && visibleRows.length === 0"
                        :icon="config.emptyIcon || 'far fa-folder-open'"
                        :title="rows.length ? `No ${(config.title || '').toLowerCase()} match your filters` : `No ${(config.title || '').toLowerCase()} yet`"
                        :text="rows.length ? 'Try a different search or status.' : (config.emptyText || '')" />
            <!-- In the Intern List the whole row is clickable: it opens
                 the intern profile. -->
            <template v-if="!isLoading">
              <tr
                v-for="row in visibleRows"
                :key="row.id"
                :class="{ 'ojt-row-link': config.clickableRows }"
                :tabindex="config.clickableRows ? 0 : undefined"
                @click="viewInternDetails(row.id)"
                @keydown.enter="viewInternDetails(row.id)"
              >
                <td v-for="col in displayColumns" :key="col.prop" :class="col.cls">
                  <StatusPill v-if="col.pill" :status="row[col.prop] || ''" :label="col.pillLabel ? col.pillLabel(row) : ''" />
                  <template v-else-if="isBlank(cell(row, col))">
                    <span class="ojt-na">{{ col.fallback || 'Not set' }}</span>
                  </template>
                  <template v-else>
                    <span :class="col.secondary || col.clickable ? 'ojt-cell-primary' : ''">{{ cell(row, col) }}</span>
                    <span v-if="col.secondary" class="ojt-cell-secondary">{{ secondaryText(row, col) }}</span>
                  </template>
                </td>
                <td v-if="config.hasActions" class="col-actions">
                  <button type="button" class="ojt-icon-btn" title="Edit" :aria-label="`Edit ${row.name || row.id}`"
                          :disabled="isSaving" @click.stop="openEdit(row)">
                    <i class="fas fa-pen"></i>
                  </button>
                  <button type="button" class="ojt-icon-btn ojt-icon-btn--danger" title="Delete" :aria-label="`Delete ${row.name || row.id}`"
                          :disabled="isSaving" @click.stop="confirmDelete(row)">
                    <i class="fas fa-trash"></i>
                  </button>
                </td>
                <td v-if="config.clickableRows" class="col-actions">
                  <i class="fas fa-chevron-right intern-chevron" aria-hidden="true"></i>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ================= Not built yet ================= -->
    <div v-else class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">{{ title }}</h3>
        <button type="button" class="btn btn-outline-secondary btn-sm" @click="goHome">
          <i class="fas fa-arrow-left me-1"></i> Back to Dashboard
        </button>
      </div>
      <div class="card-body">
        <div class="text-center py-5">
          <i class="fas fa-tools fa-3x text-muted mb-3"></i>
          <h5 class="mb-2">{{ title }}</h5>
          <p class="text-muted mb-0">This module is registered and reachable, but its screen has not been built yet.</p>
        </div>
      </div>
    </div>

    <!-- ================= Add / Edit modal ================= -->
    <div ref="formModalEl" class="modal fade" id="moduleFormModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <form novalidate @submit.prevent="save">
            <div class="modal-header">
              <h5 class="modal-title">{{ isEditing ? `Edit ${config.singular}` : `Add ${config.singular}` }}</h5>
              <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeForm"></button>
            </div>
            <div class="modal-body">
              <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>

              <template v-if="config?.fromApplicants && !isEditing">
                <div class="mb-3">
                  <label for="internApplicantSelect" class="form-label fw-semibold">Select/Search Applicant</label>
                  <div class="position-relative">
                    <input
                      id="internApplicantSelect"
                      v-model="applicantQuery"
                      type="search"
                      class="form-control"
                      placeholder="Search by Applicant No., Full Name, or Program"
                      autocomplete="off"
                      :disabled="isSaving"
                      @focus="openApplicantSearch"
                      @input="onApplicantInput"
                      @keydown.esc="applicantSearchOpen = false"
                    />
                    <div
                      v-if="applicantSearchOpen"
                      class="position-absolute w-100 bg-white border rounded shadow-sm mt-1"
                      style="z-index: 1055; max-height: 220px; overflow-y: auto;"
                    >
                      <div v-if="applicantsLoading" class="p-2 text-muted small">Loading applicants...</div>
                      <div v-else-if="!filteredApplicants.length" class="p-2 text-muted small">No qualified applicants found.</div>
                      <button
                        v-for="app in filteredApplicants"
                        :key="app.id"
                        type="button"
                        class="btn btn-light text-start w-100 border-0 rounded-0"
                        @mousedown.prevent
                        @click="applyApplicant(app)"
                      >
                        <div class="d-flex justify-content-between">
                          <strong>{{ formatApplicantNo(app.applicantNo) }}</strong>
                          <span class="text-muted small">{{ app.program || '-' }}</span>
                        </div>
                        <div class="small">{{ app.lastName }}, {{ app.firstName }}{{ app.middleName ? ' ' + app.middleName : '' }}{{ app.suffix ? ' ' + app.suffix : '' }}</div>
                      </button>
                    </div>
                  </div>
                  <div class="form-text">Filters to applicants not yet hired.</div>
                </div>

                <div v-if="selectedApplicant" class="card bg-light border-0 mb-3">
                  <div class="card-body py-2 px-3">
                    <div class="fw-semibold mb-1">Applicant Summary</div>
                    <div class="row g-1 small">
                      <div class="col-6"><span class="text-muted">Applicant No.:</span> {{ formatApplicantNo(selectedApplicant.applicantNo) }}</div>
                      <div class="col-6"><span class="text-muted">Full Name:</span> {{ buildFullName(selectedApplicant) }}</div>
                      <div class="col-6"><span class="text-muted">School:</span> {{ selectedApplicant.schoolName || selectedApplicant.school || '-' }}</div>
                      <div class="col-6"><span class="text-muted">Program/Course:</span> {{ selectedApplicant.program || '-' }}</div>
                      <div class="col-6"><span class="text-muted">Email:</span> {{ selectedApplicant.email || '-' }}</div>
                      <div class="col-6"><span class="text-muted">Contact No.:</span> {{ selectedApplicant.contactNumber || '-' }}</div>
                      <div class="col-6"><span class="text-muted">Required Hours:</span> {{ selectedApplicant.requiredHours ?? selectedApplicant.hoursRequired ?? '-' }}</div>
                    </div>
                  </div>
                </div>
              </template>

              <div v-for="col in config.columns" :key="col.prop" class="mb-3">
                <label :for="`m-${col.prop}`" class="form-label fw-semibold">{{ col.label }}</label>

                <!-- Request dropdown: only requests that still have open slots. -->
                <select v-if="col.prop === 'requestId'" :id="`m-${col.prop}`" v-model="form.requestId"
                        class="form-select" :class="{ 'is-invalid': submitted && !form.requestId }"
                        :disabled="isSaving || isEditing">
                  <option value="">Select an intern request</option>
                  <option v-for="r in availableRequests" :key="r.id" :value="r.id">
                    #{{ r.id }} &mdash; {{ r.officeName }} ({{ r.remaining }} slot{{ r.remaining === 1 ? '' : 's' }} left)
                  </option>
                </select>
                <div v-if="col.prop === 'requestId'" class="invalid-feedback">Please select an intern request.</div>

                <template v-else-if="col.prop === 'status'">
                  <!-- Status field: uneditable when editing, defaults to 'Hired' when creating -->
                  <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" type="text" class="form-control"
                         :class="{ 'is-invalid': submitted && !form[col.prop] }" :maxlength="col.max"
                         :placeholder="`Input ${col.label}`" :disabled="isSaving || isEditing" />
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                  <div v-if="isEditing" class="form-text text-muted">Status cannot be changed after hiring.</div>
                </template>

                <template v-else-if="col.prop === 'school' && !(config?.fromApplicants && !isEditing)">
                  <select :id="`m-${col.prop}`" v-model="form.school"
                          class="form-select" :class="{ 'is-invalid': submitted && !form.school }"
                          :disabled="isSaving || schoolsLoading">
                    <option value="" disabled>
                      {{ schoolsLoading ? 'Loading schools...' : 'Select a school' }}
                    </option>
                    <option v-if="form.school && !schoolOptions.includes(form.school)" :value="form.school">
                      {{ form.school }}
                    </option>
                    <option v-for="name in schoolOptions" :key="name" :value="name">
                      {{ name }}
                    </option>
                  </select>
                  <div class="invalid-feedback">Please select a school.</div>
                  <div v-if="!schoolsLoading && !schoolOptions.length" class="form-text text-danger">
                    No schools found. Please add a school first.
                  </div>
                </template>

                <template v-else-if="col.prop === 'department'">
                  <select :id="`m-${col.prop}`" v-model.trim="form.department"
                          class="form-select" :disabled="isSaving">
                    <option value="">Select Department (optional)</option>
                    <option v-for="name in departmentList" :key="name" :value="name">
                      {{ name }}
                    </option>
                  </select>
                  <div class="form-text text-muted">Optional, but the supervisor portal only shows interns, applicants, and slots for this department.</div>
                </template>

                <template v-else-if="col.prop === 'position'">
                  <select :id="`m-${col.prop}`" v-model.trim="form.position"
                          class="form-select" :class="{ 'is-invalid': submitted && !form.position }"
                          :disabled="isSaving">
                    <option value="" disabled hidden>Select Position</option>
                    <option value="supervisor">Supervisor</option>
                    <option value="assistant supervisor">Assistant Supervisor</option>
                  </select>
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                </template>

                <template v-else-if="col.prop === 'fullName' && !(config?.fromApplicants && !isEditing)">
                  <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" type="text" class="form-control"
                         :class="{ 'is-invalid': submitted && !form[col.prop] }" :maxlength="col.max"
                         :placeholder="`Input ${col.label}`" :disabled="isSaving" />
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                </template>

                <template v-else-if="col.prop === 'approvalScope'">
                  <select :id="`m-${col.prop}`" v-model="form.approvalScope" class="form-select" :disabled="isSaving">
                    <option value="Both">Certificates of Completion and DTRs</option>
                    <option value="Certificate">Certificates of Completion only</option>
                    <option value="DTR">DTRs only</option>
                  </select>
                  <div class="form-text">Which documents this officer can approve and sign.</div>
                </template>

                <!-- Plain text/email fields (e.g. signatory Name and Email). -->
                <template v-else-if="!(config?.fromApplicants && !isEditing) && ['name', 'email', 'programCode', 'programName'].includes(col.prop)">
                  <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" :type="col.type === 'email' ? 'email' : 'text'"
                         class="form-control" :class="{ 'is-invalid': submitted && !col.optional && !form[col.prop] }"
                         :maxlength="col.max" :placeholder="col.placeholder || `Input ${col.label}`" :disabled="isSaving" />
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                  <div v-if="col.help" class="form-text">{{ col.help }}</div>
                </template>
              </div>

              <template v-if="config?.fromApplicants && !isEditing">
                <div class="mb-3">
                  <label for="internDept" class="form-label fw-semibold">Assigned Department</label>
                  <select id="internDept" v-model="form.requestId" class="form-select" :disabled="isSaving || isEditing">
                    <option value="">Select Department</option>
                    <option v-for="r in availableRequests" :key="r.id" :value="r.id">
                      #{{ r.id }} &mdash; {{ r.officeName }} ({{ r.remaining }} slot{{ r.remaining === 1 ? '' : 's' }} left)
                    </option>
                  </select>
                  <div class="form-text">Select the intern request / department assignment.</div>
                </div>

                <div class="mb-3">
                  <label for="internRequiredHours" class="form-label fw-semibold">Required Hours</label>
                  <input id="internRequiredHours" v-model.number="form.requiredHours" type="number" min="0" class="form-control" :disabled="isSaving" />
                </div>

                <div class="row g-2 mb-3">
                  <div class="col-md-6">
                    <label for="internStartDate" class="form-label fw-semibold">Estimated Start Date</label>
                    <input id="internStartDate" v-model="form.startDate" type="date" class="form-control" :disabled="isSaving" />
                  </div>
                  <div class="col-md-6">
                    <label for="internEndDate" class="form-label fw-semibold">Estimated End Date</label>
                    <input id="internEndDate" v-model="form.endDate" type="date" class="form-control" :disabled="isSaving" />
                  </div>
                </div>
              </template>
              <div v-if="isEditing && form.updatedAt" class="mb-3">
                <label class="form-label fw-semibold">Last Updated</label>
                <input type="text" class="form-control form-control-plaintext bg-light" readonly
                       :value="formatDate(form.updatedAt)" />
              </div>
            </div>
            <div class="modal-footer">
              <button type="submit" class="btn btn-primary" :disabled="isSaving">
                <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
                {{ isEditing ? 'Update' : 'Submit' }}
              </button>
              <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeForm">Close</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- ================= Delete modal ================= -->
    <div ref="deleteModalEl" class="modal fade" id="moduleDeleteModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Delete</h5>
            <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeDelete"></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">Delete <strong class="text-danger">{{ pending?.name || pending?.id }}</strong>? This cannot be undone.</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-danger" :disabled="isSaving" @click="doDelete">
              <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
              Delete
            </button>
            <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeDelete">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= View Intern Modal ================= -->
    <Teleport to="body">
      <div v-if="viewIntern"
           class="intern-modal-backdrop"
           role="dialog" aria-modal="true" aria-label="Intern Details" @click.self="closeView">
        <div class="intern-modal-card">
          <div class="intern-modal-head">
            <div>
              <span class="intern-modal-eyebrow">Intern profile</span>
              <h5 class="intern-modal-title">Intern Details</h5>
            </div>
            <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeView"></button>
          </div>

          <div class="intern-modal-body">
            <div class="intern-section">
              <div class="intern-fields">
                <div class="intern-field">
                  <span class="intern-field-label">Applicant No.</span>
                  <div class="intern-field-value intern-field-value--strong">{{ viewIntern.applicantNo || '-' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Full Name</span>
                  <div class="intern-field-value intern-field-value--strong">{{ viewIntern.fullName || '-' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">School</span>
                  <div class="intern-field-value">{{ viewIntern.SchoolAbbreviation || viewIntern.school || '-' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Program</span>
                  <div class="intern-field-value">{{ viewIntern.program || '-' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Contact No.</span>
                  <div class="intern-field-value">{{ viewIntern.contactNumber || '-' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Department</span>
                  <div class="intern-field-value">{{ viewIntern.requestOffice || (viewIntern.requestId ? `#${viewIntern.requestId}` : '-') }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Hours Rendered</span>
                  <div class="intern-field-value intern-field-value--strong">{{ internHours(viewIntern) }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">OJT Period</span>
                  <div class="intern-field-value">{{ formatOJTPeriod(viewIntern) }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Fridays</span>
                  <div class="intern-field-value">{{ viewIntern.excludeFriday ? 'Excluded' : 'Included' }}</div>
                </div>
                <div class="intern-field">
                  <span class="intern-field-label">Status</span>
                  <div class="intern-field-value">
                    <span class="badge" :class="statusBadge(viewIntern.status)">{{ statusLabel(viewIntern.status) || '-' }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Permanent issuance history: documents issued to the intern. -->
            <div class="intern-section">
              <div class="intern-section-head">
                <h6 class="intern-section-title">
                  <i class="fas fa-file-alt"></i> Generated Documents
                </h6>
                <span v-if="viewDocumentsLoading" class="spinner-border spinner-border-sm text-secondary" role="status"></span>
              </div>
              <div v-if="viewDocuments.length" class="table-responsive">
                <table class="table table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Document</th>
                      <th>Issued</th>
                      <th>Details</th>
                      <th>Signatory</th>
                      <th>Recorded</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="doc in viewDocuments" :key="doc.id">
                      <td>
                        <span class="badge" :class="doc.documentType === 'ID' ? 'bg-primary' : 'bg-info text-dark'">{{ doc.documentType }}</span>
                      </td>
                      <td class="text-nowrap">{{ formatDateOnly(doc.dateIssued) || '-' }}</td>
                      <td class="small">
                        <template v-if="doc.documentType === 'ID'">
                          ID No. {{ doc.idNumber || '-' }}<span v-if="doc.validUntil"> &middot; valid until {{ formatDateOnly(doc.validUntil) }}</span>
                        </template>
                        <template v-else>
                          {{ doc.hoursRendered }} of {{ doc.requiredHours }} hrs<span v-if="doc.recipientEmail"> &middot; emailed to {{ doc.recipientEmail }}</span>
                        </template>
                      </td>
                      <td class="small">{{ doc.signatoryName || '-' }}</td>
                      <td class="text-nowrap small text-muted">{{ formatDate(doc.createdAt) }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div v-else-if="!viewDocumentsLoading" class="intern-empty">
                No documents have been generated for this intern yet.
              </div>
            </div>
          </div>

          <div class="intern-modal-foot">
            <button v-if="viewIntern.status === 'Completed'" type="button" class="intern-btn intern-btn--maroon" :disabled="isSaving" @click="generateCocFromView">
              <i class="fas fa-certificate"></i> Generate COC
            </button>
            <button v-if="viewIntern.status === 'Hired' || viewIntern.status === 'Completed'" type="button" class="intern-btn intern-btn--amber" :disabled="isSaving" @click="generateIdFromView">
              <i class="far fa-id-card"></i> Generate ID
            </button>
            <button type="button" class="intern-btn intern-btn--edit" :disabled="isSaving" @click="editFromView">
              <i class="fas fa-pen"></i> Edit
            </button>
            <button type="button" class="intern-btn intern-btn--danger-ghost" :disabled="isSaving" @click="deleteFromView">
              <i class="fas fa-trash"></i> Delete
            </button>
            <button type="button" class="intern-btn intern-btn--ghost" @click="closeView">
              <i class="fas fa-times"></i> Close
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ================= Toast ================= -->
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div ref="toastEl" class="toast align-items-center text-bg-success border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
          <div class="toast-body">{{ message }}</div>
          <button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close" @click="toast.hide()"></button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { Modal, Toast } from 'bootstrap'
import { go } from '@/utils/navigation'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { getApiErrorMessage, hrAttendanceAPI, internRequestsAPI, internsAPI, programsAPI, schoolsAPI, signatoriesAPI, applicantsAPI, dtrAPI } from '@/services/api'

/* page -> module config. Pages not listed here show "not built yet". */
const MODULES = {
  'internlist.html': {
    title: 'Intern List', singular: 'Intern', api: internsAPI, hiring: true, needsSchools: true, fromApplicants: true,
    subtitle: 'All hired interns. Select a row to open the intern profile.',
    emptyIcon: 'fas fa-user-graduate',
    emptyText: 'Hire an applicant to add them to this list.',
    searchProps: ['fullName', 'email', 'schoolAbbreviation', 'school', 'requestOffice', 'applicantNo'],
    /* The row itself is clickable and opens the intern profile. */
    clickableRows: true,
    columns: [
      { prop: 'fullName', label: 'Full Name', max: 200 },
      { prop: 'school', label: 'School', max: 200 },
      { prop: 'email', label: 'Email', type: 'email', max: 200 },
      { prop: 'requestId', label: 'Request ID', type: 'text' },
      { prop: 'status', label: 'Status', max: 20 }
    ],
    /* Extra column taken from the profile: it does not appear in the form. */
    display: [
      { prop: 'fullName', label: 'Intern', clickable: true, secondary: (row) => row.email || 'No email on file' },
      { prop: 'schoolAbbreviation', label: 'School', format: (v, row) => v || row.school || '' },
      { prop: 'requiredHours', label: 'Required Hours', cls: 'col-num', fallback: '0' },
      { prop: 'requestOffice', label: 'Department', fallback: 'Unassigned' },
      { prop: 'startDate', label: 'Estimated Start', cls: 'col-date', fallback: 'TBD', format: (v) => shortDate(v) },
      { prop: 'endDate', label: 'Estimated End', cls: 'col-date', fallback: 'TBD', format: (v) => shortDate(v) },
      /* Last column: the intern's status (Active / Completed / etc.).
         The row itself is clickable and opens the profile. */
      { prop: 'status', label: 'Status', pill: true }
    ]
  },
  'programs.html': {
    title: 'Programs / Strands', singular: 'Program', api: programsAPI,
    columns: [{ prop: 'programCode', label: 'Program Code', max: 50 }, { prop: 'programName', label: 'Program Name', max: 200 }]
  },
  'signatories.html': {
    title: 'Signatories', singular: 'Signatory', api: signatoriesAPI, needsDepartment: true, hasActions: true,
    subtitle: 'Approving officers. Each signatory gets a supervisor portal account scoped to their department.',
    emptyIcon: 'fas fa-signature',
    emptyText: 'Add a signatory to give a department supervisor portal access.',
    searchProps: ['name', 'position', 'department', 'email'],
    statusProp: 'accountStatus',
    /* Reload after saving so the linked-account status is refreshed. */
    reloadAfterSave: true,
    columns: [
      { prop: 'name', label: 'Name', max: 200 },
      { prop: 'position', label: 'Position', max: 150 },
      { prop: 'department', label: 'Department', max: 200 },
      {
        prop: 'email', label: 'Email', type: 'email', max: 200,
        help: 'A portal account is created for this email (or linked, if one exists) and the temporary password is emailed.'
      },
      { prop: 'approvalScope', label: 'Approves', default: 'Both' }
    ],
    display: [
      { prop: 'name', label: 'Signatory', secondary: (row) => row.email || 'No email on file' },
      { prop: 'position', label: 'Position', format: (v) => titleCase(v) },
      { prop: 'department', label: 'Department', fallback: 'No department' },
      { prop: 'approvalScope', label: 'Approves', format: (v) => APPROVAL_LABELS[v] || APPROVAL_LABELS.Both },
      {
        prop: 'accountStatus', label: 'Portal Account', pill: true,
        pillLabel: (row) => ({ Active: 'Active', Invited: 'Invited', Unlinked: 'No account' })[row.accountStatus] || ''
      }
    ]
  },
  'forms.html': { title: 'Issuance of COC' },
  'dtr.html': {
    title: 'Daily Time Records', singular: 'Record', api: dtrAPI,
    columns: [
      { prop: 'date', label: 'Date', type: 'date' },
      { prop: 'shiftType', label: 'Shift', max: 20 },
      { prop: 'timeIn', label: 'Time In', type: 'time' },
      { prop: 'timeOut', label: 'Time Out', type: 'time' },
      { prop: 'status', label: 'Status', max: 50 }
    ]
  },
  'utility.html': { title: 'Utility' },
  'contact.html': { title: 'Contact Us' }
}

/* The page comes in as a prop (from the router) so the screen updates
   when the user switches module. Reading it from window.location
   would keep showing the old module, because Vue reuses the
   same component instance. */
const props = defineProps({
  modulePage: { type: String, default: '' }
})

const entry = computed(() => MODULES[props.modulePage] || {})
const config = computed(() => entry.value || {})
const title = computed(() => entry.value.title || 'Module')

/* Columns shown in the table. Default: all of config.columns.
   A module can set its own "display" list to add a
   computed column (for example, OJT period) that is not in the form. */
const displayColumns = computed(() => entry.value.display || entry.value.columns || [])

/* Value shown in a cell. If the column has a "format", use it
   (for example, a date, or "#id - Office Name"). */
function cell(row, col) {
  const value = row[col.prop]
  if (col.format) return col.format(value, row)
  return value
}

/* Blank cells render a muted fallback (col.fallback) instead of a dash. */
function isBlank(value) {
  return value === null || value === undefined || String(value).trim() === ''
}

function secondaryText(row, col) {
  return typeof col.secondary === 'function' ? col.secondary(row) : row[col.secondary]
}

const APPROVAL_LABELS = {
  Both: 'COC & DTR',
  Certificate: 'COC only',
  DTR: 'DTR only'
}

function titleCase(value) {
  return String(value || '').replace(/\b\w/g, (c) => c.toUpperCase())
}

function shortDate(value) {
  const d = value ? new Date(value) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : ''
}

const rows = ref([])
const search = ref('')
const statusFilter = ref('')

/* Prop used by the status dropdown: the module's statusProp, else "status". */
const statusProp = computed(() => config.value.statusProp || 'status')

const statusOptions = computed(() => {
  const hasStatusColumn = displayColumns.value.some((c) => c.prop === statusProp.value)
  if (!hasStatusColumn) return []
  return [...new Set(rows.value.map((r) => r[statusProp.value]).filter(Boolean))].sort()
})

const visibleRows = computed(() => {
  const term = search.value.trim().toLowerCase()
  const props = config.value.searchProps || displayColumns.value.map((c) => c.prop)
  return rows.value.filter((row) => {
    if (statusFilter.value && row[statusProp.value] !== statusFilter.value) return false
    if (!term) return true
    return props.some((p) => String(row[p] ?? '').toLowerCase().includes(term))
  })
})

const tableColspan = computed(() =>
  displayColumns.value.length + (config.value.hasActions ? 1 : 0) + (config.value.clickableRows ? 1 : 0))
const availableRequests = ref([])
const departments = ref([])
const schools = ref([])
const schoolsLoading = ref(false)
const applicantsList = ref([])
const applicantsLoading = ref(false)
const applicantQuery = ref('')
const applicantSearchOpen = ref(false)
const selectedApplicantId = ref(null)
const selectedApplicant = ref(null)
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const formError = ref('')
const submitted = ref(false)
const pending = ref({ id: null, name: '' })
const viewIntern = ref(null)
const message = ref('')

const form = reactive({ id: 0 })
const isEditing = computed(() => form.id > 0)

/* Generated-document history (issuance record) for the open intern. */
const viewDocuments = ref([])
const viewDocumentsLoading = ref(false)

const formModalEl = ref(null)
const deleteModalEl = ref(null)
const toastEl = ref(null)
let formModal = null
let deleteModal = null
let toast = null
let toastTimer = null

function blankForm() {
  form.id = 0
  form.requestId = ''
  form.updatedAt = null
  if (config.value?.columns) config.value.columns.forEach((c) => { form[c.prop] = c.default ?? '' })
  applicantQuery.value = ''
  applicantSearchOpen.value = false
  selectedApplicantId.value = null
  selectedApplicant.value = null
  submitted.value = false
  formError.value = ''
}

function notify(text, variant = 'success') {
  message.value = text
  const el = toastEl.value
  el.classList.remove('text-bg-success', 'text-bg-danger')
  el.classList.add(`text-bg-${variant}`)
  toast.show()
  window.clearTimeout(toastTimer)
  toastTimer = window.setTimeout(() => toast.hide(), 4000)
}

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleString(undefined, { year: 'numeric', month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })
: '-'
}

function removeModalBackdrops() {
  if (typeof document === 'undefined') return
  document.querySelectorAll?.('.modal-backdrop')?.forEach?.(el => el.remove?.())
  document.body?.classList?.remove('modal-open')
  document.body.style.overflow = ''
  document.body.style.paddingRight = ''
}

async function load() {
  isLoading.value = true
  loadError.value = ''
  try {
    if (!config.value?.api) {
      // No config for this module; render empty state and hide loader
      rows.value = []
      return
    }
    rows.value = await config.value.api.getAll()
    if (config.value.hiring || config.value.needsDepartment) {
      availableRequests.value = await internRequestsAPI.available()
      // Fetch all requests to map requestId -> full office name for Department column
      const allRequests = await internRequestsAPI.getAll()
      const requestMap = new Map(allRequests.map(r => [r.id, r.officeName]))
      rows.value.forEach(row => {
        row.requestOffice = requestMap.get(row.requestId) || null
      })
    }
    // Load departments from shared source for Signatory modal dropdown
    if (config.value.needsDepartment) {
      try {
        const response = await fetch('/assets/json/offices.json')
        const data = await response.json()
        departments.value = data.offices || []
      } catch {
        departments.value = []
      }
    }

    // Load registered schools for the Intern modal dropdown.
    if (config.value.needsSchools) {
      schoolsLoading.value = true
      try {
        schools.value = await schoolsAPI.getAll()
      } catch {
        schools.value = []
      } finally {
        schoolsLoading.value = false
      }
    }

    // Load applicants for Intern creation from applicants table.
    if (config.value.fromApplicants) {
      applicantsLoading.value = true
      try {
        const list = await applicantsAPI.getAll()
        applicantsList.value = (list || []).filter((a) => !a.hired)
      } catch {
        applicantsList.value = []
      } finally {
        applicantsLoading.value = false
      }
    }
  } catch (e) {
    loadError.value = getApiErrorMessage(e, `Failed to load ${title.value}.`)
  } finally {
    isLoading.value = false
  }
}

const departmentList = computed(() => {
  const seen = new Set()
  return departments.value
    .filter(d => d.name && !seen.has(d.name) && seen.add(d.name))
    .map(d => d.name)
})

const schoolOptions = computed(() => {
  const seen = new Set()
  return schools.value
    .filter(s => s.name && !seen.has(s.name) && seen.add(s.name))
    .map(s => s.name)
})

const filteredApplicants = computed(() => {
  const query = applicantQuery.value.trim().toLowerCase()
  if (!query) return applicantsList.value.slice(0, 15)

  return applicantsList.value
    .filter((app) => {
      const fullName = `${app.lastName || ''}, ${app.firstName || ''} ${app.middleName || ''} ${app.suffix || ''}`.toLowerCase()
      const applicantNo = (app.applicantNo || '').replace(/^A-/, '').toLowerCase()
      const queryNo = query.replace(/^a-/, '')
      return (
        applicantNo.includes(queryNo) ||
        (app.applicantNo || '').toLowerCase().includes(query) ||
        fullName.includes(query) ||
        (app.program || '').toLowerCase().includes(query) ||
        (app.schoolName || '').toLowerCase().includes(query)
      )
    })
    .slice(0, 15)
})

function openApplicantSearch() {
  if (config.value?.fromApplicants && !isEditing.value) {
    applicantSearchOpen.value = true
  }
}

function onApplicantInput() {
  selectedApplicantId.value = null
  selectedApplicant.value = null
  if (config.value?.fromApplicants && !isEditing.value) {
    applicantSearchOpen.value = true
  }
}

function formatApplicantNo(no) {
  if (!no) return '-'
  return String(no).replace(/^A-/, '')
}

function buildFullName(a) {
  if (!a) return ''
  const last = a.lastName || ''
  const first = a.firstName || ''
  const middle = a.middleName || ''
  const suffix = a.suffix || ''
  if (last && first) {
    let name = `${last}, ${first}${middle ? ' ' + middle : ''}`
    return suffix ? `${name} ${suffix}` : name
  }
  if (a.fullName) {
    return suffix && !String(a.fullName).includes(suffix) ? `${a.fullName} ${suffix}` : a.fullName
  }
  const parts = [last, first, middle].filter(Boolean)
  let name = parts.join(' ')
  return suffix ? `${name} ${suffix}` : name
}

function applyApplicant(app) {
  selectedApplicantId.value = app.id
  selectedApplicant.value = app
  applicantQuery.value = `${formatApplicantNo(app.applicantNo) || ''} - ${buildFullName(app)}`.replace(/^- /, '')
  applicantSearchOpen.value = false

  form.fullName = buildFullName(app)
  form.school = app.schoolName || app.school || form.school
  form.program = app.program || form.program
  form.applicantNo = formatApplicantNo(app.applicantNo) || form.applicantNo
  form.email = app.email || form.email

  const hours = app.requiredHours ?? app.hoursRequired ?? app.totalHours ?? app.required_hours ?? app.hours_required
  if (hours !== null && hours !== undefined && hours !== '') {
    form.requiredHours = hours
  } else if (selectedApplicant.value?.requiredHours !== null && selectedApplicant.value?.requiredHours !== undefined) {
    form.requiredHours = selectedApplicant.value.requiredHours
  } else if (selectedApplicant.value?.hoursRequired !== null && selectedApplicant.value?.hoursRequired !== undefined) {
    form.requiredHours = selectedApplicant.value.hoursRequired
  }

  form.applicantOffice = form.requestId ? form.requestId : form.applicantOffice
}

function openCreate() {
  blankForm()
  form.status = 'Hired'
  formModal.show()
}
function openEdit(row) {
  blankForm()
  form.id = row.id
  config.value.columns.forEach((c) => { form[c.prop] = row[c.prop] ?? c.default ?? '' })
  // Copy updatedAt for display in the modal (not in config.columns)
  form.updatedAt = row.updatedAt
  formModal.show()
}
function closeForm() { if (!isSaving.value) { formModal.hide(); removeModalBackdrops() } }

async function save() {
  submitted.value = true
  formError.value = ''

  if (config.value.hiring && availableRequests.value.length === 0) {
    formError.value = 'There is no available intern request. Please create a request first.'
    return
  }

  if (config.value.fromApplicants && !isEditing.value && !selectedApplicantId.value) {
    formError.value = 'Please select an applicant from the list.'
    return
  }

  const requiredColumns = config.value.columns.filter(c => c.prop !== 'department' && !c.optional && c.default === undefined)
  const missing = requiredColumns.some((c) => !form[c.prop])
  if (missing) { formError.value = 'Please fill in all required fields.'; return }

  isSaving.value = true
  const api = config.value.api

  /* Send every field listed in config.columns (for example fullName,
     school, programCode), not just a signatory's name and position,
     so the backend can check the right required fields. */
  const payload = { id: form.id || 0 }
  config.value.columns.forEach((c) => { payload[c.prop] = form[c.prop] ?? '' })
  if ('requestId' in payload) payload.requestId = Number(payload.requestId) || null
  if (config.value.hiring) payload.status = 'Hired'
  if (config.value.fromApplicants && !isEditing.value && selectedApplicantId.value) {
    payload.applicantId = selectedApplicantId.value
    payload.applicantNo = form.applicantNo || selectedApplicant.value?.applicantNo || ''
  }
  if (form.requiredHours !== undefined && form.requiredHours !== '') payload.requiredHours = Number(form.requiredHours) || 0
  if (form.startDate) payload.startDate = form.startDate
  if (form.endDate) payload.endDate = form.endDate

  try {
    if (isEditing.value) {
      await api.update(form.id, payload)
      const idx = rows.value.findIndex(r => r.id === form.id)
      if (idx !== -1) rows.value[idx] = { ...rows.value[idx], ...payload }
      notify('Record updated.')
    } else {
      const created = await api.create(payload)
      rows.value.unshift(created)
      notify(config.value.reloadAfterSave && payload.email
        ? 'Signatory saved. Their portal account is linked and login details were emailed if the account is new.'
        : 'Record created.')
    }
    formModal.hide()
    removeModalBackdrops()
    if (config.value.reloadAfterSave) load()
  } catch (e) {
    const msg = getApiErrorMessage(e, 'Failed to save.')
    // 409 Conflict - duplicate record. Use the API's message if there is one,
    // because each module checks a different field (name, fullName, etc.).
    if (e?.response?.status === 409 || msg.toLowerCase().includes('already exists') || msg.toLowerCase().includes('conflict')) {
      formError.value = e?.response?.data?.message
        || `A ${config.value.singular.toLowerCase()} with the same name already exists. Please choose a different one.`
    } else {
      formError.value = msg
    }
    removeModalBackdrops()
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(row) {
  // Intern rows have no `name` field, so use fullName/applicantNo
  // (and other modules' name) before falling back to the id.
  pending.value = { id: row.id, name: row.name || row.fullName || row.applicantNo || row.id }
  deleteModal.show()
}
function closeDelete() { if (!isSaving.value) { deleteModal.hide(); pending.value = { id: null, name: '' } } }

/* Open the intern profile when a row in the Intern List is clicked. */
function viewInternDetails(id) {
  if (!config.value?.clickableRows) return
  const row = rows.value.find((item) => Number(item.id) === Number(id))
  if (row) openView(row)
}

function openView(row) {
  viewIntern.value = row
  document.body.style.overflow = 'hidden'
  loadViewDocuments(row.id)
}

/* History of documents already issued (ID / COC), from the API. */
async function loadViewDocuments(internId) {
  viewDocuments.value = []
  if (!Number.isFinite(Number(internId)) || Number(internId) <= 0) return
  viewDocumentsLoading.value = true
  try {
    viewDocuments.value = await hrAttendanceAPI.getGeneratedDocuments(internId)
  } catch {
    viewDocuments.value = []
  } finally {
    viewDocumentsLoading.value = false
  }
}

function closeView() {
  if (isSaving.value) return
  viewIntern.value = null
  viewDocuments.value = []
  document.body.style.overflow = ''
}
function editFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  openEdit(row)
}
function deleteFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  confirmDelete(row)
}
function generateCocFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  /* Navigate to COC generation page with the intern pre-selected */
  go(`/coc-generate.html?internId=${row.id}`)
}

function generateIdFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  /* Navigate to ID generation page with the intern pre-selected */
  go(`/idgenerate.html?internId=${row.id}`)
}

/* Show "Active" when the saved status is "Hired". */
function statusLabel(status) {
  if (!status) return ''
  return status === 'Hired' ? 'Active' : status
}

/* Hours rendered: from the recalculated field, if there is one. */
function internHours(row) {
  if (row?.hoursRendered !== null && row?.hoursRendered !== undefined) return row.hoursRendered
  return row?.durationDays != null ? row.durationDays * 8 : '-'
}

function formatDateOnly(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : ''
}

function formatOJTPeriod(row) {
  const s = formatDate(row.startDate)
  const e = formatDate(row.endDate)
  return s || e ? `${s} \u2013 ${e}` : '-'
}
function statusBadge(status) {
  switch (status) {
    case 'Hired':
    case 'Active': return 'bg-success'
    case 'Pending': return 'bg-warning text-dark'
    case 'Completed': return 'bg-info text-dark'
    case 'Terminated': return 'bg-danger'
    default: return 'bg-secondary'
  }
}

async function doDelete() {
  if (!pending.value?.id) return
  isSaving.value = true
  try {
    await config.value.api.delete(pending.value.id)
    rows.value = rows.value.filter(r => r.id !== pending.value.id)
    notify('Record deleted.')
    deleteModal.hide()
    removeModalBackdrops()
    pending.value = { id: null, name: '' }
  } catch (e) {
    notify(getApiErrorMessage(e, 'Failed to delete.'), 'danger')
  } finally {
    isSaving.value = false
  }
}

function goHome() {
  go('/dashboard.html')
}

onMounted(() => {
  formModal = new Modal(formModalEl.value)
  deleteModal = new Modal(deleteModalEl.value)
  // Toast is the right class for the `.toast` element, not Modal, because
  // it has no `.modal-dialog` inside, so Modal would throw a querySelector error.
  toast = new Toast(toastEl.value, { autohide: false })
  window.addEventListener('ojt:intern-profile', handleInternProfileEvent)
  load().then(openInternFromRoute)
})

watch(() => props.modulePage, () => {
  search.value = ''
  statusFilter.value = ''
  load().then(openInternFromRoute)
})

/* Deep link (for example, from a notification): ?internId=12 on the Intern List.
   Opens that intern's profile once the table has loaded. */
function routeInternId() {
  try {
    const params = new URLSearchParams(window.location.search)
    const id = Number(params.get('internId'))
    return Number.isFinite(id) && id > 0 ? id : null
  } catch {
    return null
  }
}

function openInternFromRoute() {
  if (props.modulePage !== 'internlist.html') return
  const id = routeInternId()
  if (!id) return
  const row = rows.value.find((r) => Number(r.id) === id)
  if (row) openView(row)
}

/* A notification was clicked while the Intern List is already open. */
function handleInternProfileEvent() {
  if (isLoading.value) return
  openInternFromRoute()
}

onBeforeUnmount(() => {
  formModal?.dispose(); deleteModal?.dispose(); toast?.dispose()
  window.clearTimeout(toastTimer)
  window.removeEventListener('ojt:intern-profile', handleInternProfileEvent)
  document.body.style.overflow = ''
})
</script>

<style scoped>
/* Trailing chevron on clickable Intern List rows: muted by default,
   orange and stronger on hover. Row hover comes from data-table.css. */
.intern-chevron {
  color: #b8a99f;
  opacity: 0.45;
  transition: color 0.2s ease, opacity 0.2s ease, transform 0.2s ease;
}

tr.ojt-row-link:hover .intern-chevron {
  color: #d97706;
  opacity: 1;
  transform: translateX(2px);
}

/* ===== Intern Details modal ===== */
.intern-modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1055;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 0.75rem;
  background-color: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
}

.intern-modal-card {
  display: flex;
  flex-direction: column;
  width: 720px;
  max-width: 100%;
  max-height: calc(100vh - 1.5rem);
  overflow: hidden;
  background-color: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
}

.intern-modal-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.5rem;
  border-bottom: 1px solid #f1f5f9;
}

.intern-modal-eyebrow {
  display: block;
  margin-bottom: 0.15rem;
  color: #b45309;
  font-size: 0.65rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.intern-modal-title {
  margin: 0;
  color: #7f1d1d;
  font-size: 1.15rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.intern-modal-body {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 1rem;
  min-height: 0;
  padding: 1.25rem 1.5rem;
  overflow-y: auto;
  background-color: #f8fafc;
}

.intern-section {
  padding: 1.1rem 1.25rem;
  background-color: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
}

.intern-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem 1.5rem;
}

.intern-field {
  min-width: 0;
}

.intern-field-label {
  display: block;
  margin-bottom: 0.2rem;
  color: #64748b;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.intern-field-value {
  color: #0f172a;
  font-size: 0.875rem;
  font-weight: 500;
  overflow-wrap: anywhere;
}

.intern-field-value--strong {
  font-size: 1rem;
  font-weight: 700;
}

.intern-section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.85rem;
  padding-bottom: 0.65rem;
  border-bottom: 1px solid #f1f5f9;
}

.intern-section-title {
  margin: 0;
  color: #7f1d1d;
  font-size: 0.9rem;
  font-weight: 700;
}

.intern-section-title > i {
  margin-right: 0.5rem;
  color: #d97706;
}

.intern-empty {
  margin: 0;
  color: #64748b;
  font-size: 0.8rem;
}

.intern-modal-foot {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 0.6rem;
  padding: 1rem 1.5rem;
  border-top: 1px solid #f1f5f9;
  background-color: #fff;
}

.intern-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.55rem 1.25rem;
  border: 1px solid transparent;
  border-radius: 999px;
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.2;
  transition: background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
}

.intern-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.intern-btn:focus-visible {
  outline: 2px solid #d97706;
  outline-offset: 2px;
}

/* Primary action: warm amber solid. */
.intern-btn--amber {
  color: #fff;
  background-color: #d97706;
  box-shadow: 0 6px 14px rgba(217, 119, 6, 0.28);
}

.intern-btn--amber:hover:not(:disabled) {
  background-color: #b45309;
}

/* Certificate action: deep maroon solid. */
.intern-btn--maroon {
  color: #fff;
  background-color: #7f1d1d;
}

.intern-btn--maroon:hover:not(:disabled) {
  background-color: #6b1515;
}

/* Edit: warm amber accent, lighter than the primary action. */
.intern-btn--edit {
  color: #92400e;
  background-color: #fffbeb;
  border-color: #fcd34d;
}

.intern-btn--edit:hover:not(:disabled) {
  background-color: #fef3c7;
  border-color: #f59e0b;
}

/* Secondary / muted outline actions. */
.intern-btn--ghost {
  color: #475569;
  background-color: #fff;
  border-color: #e2e8f0;
}

.intern-btn--ghost:hover:not(:disabled) {
  background-color: #f8fafc;
  border-color: #cbd5e1;
}

.intern-btn--danger-ghost {
  color: #7f1d1d;
  background-color: #fff;
  border-color: #e2e8f0;
}

.intern-btn--danger-ghost:hover:not(:disabled) {
  background-color: #fef2f2;
  border-color: #f5c2c2;
}

@media (max-width: 575.98px) {
  .intern-fields {
    grid-template-columns: minmax(0, 1fr);
  }

  .intern-modal-head,
  .intern-modal-body,
  .intern-modal-foot {
    padding-right: 1rem;
    padding-left: 1rem;
  }
}
</style>



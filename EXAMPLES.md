# Real-World Examples

Complete examples you can copy directly for each entity.

---

## Example 1: Schools Page

### Step 1: Model (`Models/School.cs`)
```csharp
namespace OJTMISApi.Models
{
	public class School
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Address { get; set; }
		public string Principal { get; set; }
		public string Phone { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}
```

### Step 2: DbSet (`Data/ApplicationDbContext.cs`)
```csharp
public DbSet<School> Schools { get; set; }
```

### Step 3: Controller (`Controllers/SchoolsController.cs`)
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SchoolsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<SchoolsController> _logger;

		public SchoolsController(ApplicationDbContext context, ILogger<SchoolsController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<School>>> GetSchools()
		{
			try
			{
				var schools = await _context.Schools.ToListAsync();
				return Ok(schools);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error fetching schools: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<School>> GetSchool(int id)
		{
			try
			{
				var school = await _context.Schools.FindAsync(id);
				if (school == null)
					return NotFound();
				return Ok(school);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error fetching school: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpPost]
		public async Task<ActionResult<School>> PostSchool(School school)
		{
			try
			{
				_context.Schools.Add(school);
				await _context.SaveChangesAsync();
				return CreatedAtAction("GetSchool", new { id = school.Id }, school);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating school: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutSchool(int id, School school)
		{
			try
			{
				if (id != school.Id)
					return BadRequest();

				_context.Entry(school).State = EntityState.Modified;
				await _context.SaveChangesAsync();
				return Ok(school);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error updating school: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteSchool(int id)
		{
			try
			{
				var school = await _context.Schools.FindAsync(id);
				if (school == null)
					return NotFound();

				_context.Schools.Remove(school);
				await _context.SaveChangesAsync();
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deleting school: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		private bool SchoolExists(int id)
		{
			return _context.Schools.Any(e => e.Id == id);
		}
	}
}
```

### Step 4: API Service (Add to `src/services/api.js`)
```javascript
export const schoolsAPI = {
  getAll: async () => {
	const response = await apiClient.get('/schools')
	return response.data
  },
  getById: async (id) => {
	const response = await apiClient.get(`/schools/${id}`)
	return response.data
  },
  create: async (data) => {
	const response = await apiClient.post('/schools', data)
	return response.data
  },
  update: async (id, data) => {
	const response = await apiClient.put(`/schools/${id}`, data)
	return response.data
  },
  delete: async (id) => {
	const response = await apiClient.delete(`/schools/${id}`)
	return response.data
  }
}
```

### Step 5: Component (`pages/Schools.vue`)
```vue
<template>
  <section class="content">
	<div class="card modern-card">
	  <div class="card-header modern-card-header">
		<h3 class="card-title mb-0">Schools</h3>
		<div class="card-tools">
		  <button 
			type="button" 
			class="btn btn-primary btn-sm modern-btn intern-request-action"
			@click="openCreate"
			:disabled="isLoading"
		  >
			<i class="fas fa-plus"></i> Add School
		  </button>
		</div>
	  </div>

	  <div class="card-body">
		<div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
		  {{ errorMessage }}
		  <button type="button" class="close" @click="errorMessage = ''">
			<span>&times;</span>
		  </button>
		</div>

		<div v-if="isLoading" class="text-center py-4">
		  <div class="spinner-border" role="status">
			<span class="visually-hidden">Loading...</span>
		  </div>
		</div>

		<div v-if="!isLoading" class="table-responsive">
		  <table class="table table-hover table-bordered">
			<thead>
			  <tr class="text-center">
				<th>SCHOOL NAME</th>
				<th>ADDRESS</th>
				<th>PRINCIPAL</th>
				<th>PHONE</th>
				<th>ACTION</th>
			  </tr>
			</thead>
			<tbody>
			  <tr v-for="school in schools" :key="school.id" class="text-center">
				<td>{{ school.name }}</td>
				<td>{{ school.address }}</td>
				<td>{{ school.principal }}</td>
				<td>{{ school.phone }}</td>
				<td>
				  <button 
					type="button" 
					class="btn btn-outline-primary btn-sm modern-action mr-1"
					@click="openEdit(school)"
				  >
					<i class="fas fa-edit"></i> Edit
				  </button>
				  <button 
					type="button" 
					class="btn btn-outline-danger btn-sm modern-action"
					@click="removeSchool(school)"
				  >
					<i class="fas fa-times"></i> Delete
				  </button>
				</td>
			  </tr>
			  <tr v-if="schools.length === 0">
				<td colspan="5" class="text-center text-muted py-4">No schools found.</td>
			  </tr>
			</tbody>
		  </table>
		</div>
	  </div>
	</div>

	<div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
	<div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true">
	  <div class="modal-dialog modal-dialog-centered modal-md">
		<div class="modal-content">
		  <div class="modal-header bg-orange py-2">
			<h5 class="modal-title">
			  {{ editingSchool === null ? 'Add New School' : 'Edit School' }}
			</h5>
			<button type="button" class="close" @click="closeModal">
			  <span>&times;</span>
			</button>
		  </div>

		  <form @submit.prevent="saveSchool">
			<div class="modal-body">
			  <div class="form-group mb-3">
				<label for="schoolName" class="form-label">
				  School Name <span class="text-danger">*</span>
				</label>
				<input 
				  id="schoolName"
				  v-model="form.name" 
				  type="text"
				  class="form-control"
				  placeholder="Enter school name"
				  required
				/>
			  </div>

			  <div class="form-group mb-3">
				<label for="schoolAddress" class="form-label">
				  Address <span class="text-danger">*</span>
				</label>
				<textarea 
				  id="schoolAddress"
				  v-model="form.address" 
				  class="form-control"
				  placeholder="Enter school address"
				  rows="3"
				  required
				></textarea>
			  </div>

			  <div class="form-group mb-3">
				<label for="schoolPrincipal" class="form-label">
				  Principal Name
				</label>
				<input 
				  id="schoolPrincipal"
				  v-model="form.principal" 
				  type="text"
				  class="form-control"
				  placeholder="Enter principal name"
				/>
			  </div>

			  <div class="form-group mb-3">
				<label for="schoolPhone" class="form-label">
				  Phone Number
				</label>
				<input 
				  id="schoolPhone"
				  v-model="form.phone" 
				  type="tel"
				  class="form-control"
				  placeholder="Enter phone number"
				/>
			  </div>
			</div>

			<div class="modal-footer">
			  <button type="button" class="btn btn-outline-secondary modern-action" @click="closeModal">
				<i class="fas fa-times"></i> Close
			  </button>
			  <button type="submit" class="btn btn-primary modern-action">
				<i class="fas fa-save"></i> Save
			  </button>
			</div>
		  </form>
		</div>
	  </div>
	</div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { schoolsAPI } from '../src/services/api'

const schools = ref([])
const isModalOpen = ref(false)
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const editingSchool = ref(null)
const form = ref({ name: '', address: '', principal: '', phone: '' })

async function loadSchools() {
  isLoading.value = true
  errorMessage.value = ''
  try {
	const data = await schoolsAPI.getAll()
	schools.value = Array.isArray(data) ? data : (data.data || [])
	console.log('Schools loaded:', schools.value)
  } catch (error) {
	console.error('Failed to load schools:', error)
	errorMessage.value = `Failed to load schools: ${error.message}`
  } finally {
	isLoading.value = false
  }
}

function openCreate() {
  editingSchool.value = null
  form.value = { name: '', address: '', principal: '', phone: '' }
  isModalOpen.value = true
}

function openEdit(school) {
  editingSchool.value = school.id
  form.value = { 
	name: school.name, 
	address: school.address,
	principal: school.principal,
	phone: school.phone
  }
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
  form.value = { name: '', address: '', principal: '', phone: '' }
}

async function saveSchool() {
  if (!form.value.name.trim() || !form.value.address.trim()) {
	alert('Please fill in school name and address')
	return
  }

  isSaving.value = true
  try {
	if (editingSchool.value === null) {
	  await schoolsAPI.create(form.value)
	} else {
	  await schoolsAPI.update(editingSchool.value, form.value)
	}
	await loadSchools()
	closeModal()
  } catch (error) {
	console.error('Failed to save school:', error)
	alert(`Failed to save school: ${error.message}`)
  } finally {
	isSaving.value = false
  }
}

async function removeSchool(school) {
  if (!window.confirm(`Are you sure you want to delete "${school.name}"?`)) return
  try {
	await schoolsAPI.delete(school.id)
	await loadSchools()
  } catch (error) {
	console.error('Failed to delete school:', error)
	alert(`Failed to delete school: ${error.message}`)
  }
}

onMounted(loadSchools)
</script>

<style scoped>
</style>
```

---

## Example 2: Read-Only Dashboard

If you want a **read-only page** (no edit/delete), simplify like this:

```vue
<template>
  <section class="content">
	<div class="card modern-card">
	  <div class="card-header modern-card-header">
		<h3 class="card-title mb-0">Dashboard Stats</h3>
	  </div>

	  <div class="card-body">
		<div v-if="isLoading" class="text-center py-4">
		  <div class="spinner-border"></div>
		</div>

		<div v-if="!isLoading" class="row">
		  <div class="col-md-3">
			<div class="card">
			  <div class="card-body">
				<h5 class="card-title">Total Programs</h5>
				<p class="card-text">{{ stats.totalPrograms }}</p>
			  </div>
			</div>
		  </div>
		  <div class="col-md-3">
			<div class="card">
			  <div class="card-body">
				<h5 class="card-title">Total Schools</h5>
				<p class="card-text">{{ stats.totalSchools }}</p>
			  </div>
			</div>
		  </div>
		  <!-- Add more stats -->
		</div>
	  </div>
	</div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { dashboardAPI } from '../src/services/api'

const stats = ref({ totalPrograms: 0, totalSchools: 0 })
const isLoading = ref(false)

async function loadStats() {
  isLoading.value = true
  try {
	const data = await dashboardAPI.getStats()
	stats.value = data
  } catch (error) {
	console.error('Failed to load stats:', error)
  } finally {
	isLoading.value = false
  }
}

onMounted(loadStats)
</script>
```

---

## Tip: Copy the RIGHT Example

When adding a new page, check if it should be:

- **CRUD** (Create, Read, Update, Delete) → Use Example 1 template
- **Read-Only** (Just display) → Use Example 2 template

Most of your pages will be **CRUD**, so start with Example 1.

---

## List of Pages to Add

| Page | Type | Model | Controller |
|------|------|-------|------------|
| Schools | CRUD | `School` | `SchoolsController` |
| Signatories | CRUD | `Signatory` | `SignatoriesController` |
| Interns | CRUD | `Intern` | `InternsController` |
| Requests | CRUD | `Request` | `RequestsController` |
| Dashboard | Read-Only | - | `DashboardController` |
| DTR | Hybrid | `DTR` | `DTRController` |

---

Good luck! Copy these examples and customize for your needs! 🚀

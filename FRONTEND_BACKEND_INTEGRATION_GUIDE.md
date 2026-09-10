# Frontend-Backend Integration Guide & Cheatsheet

A complete guide to connect Vue components to your ASP.NET backend API with full CRUD operations (Create, Read, Update, Delete).

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Setup Checklist](#setup-checklist)
3. [Step-by-Step Integration](#step-by-step-integration)
4. [Component Template Examples](#component-template-examples)
5. [Complete Working Example](#complete-working-example)
6. [Common Patterns](#common-patterns)
7. [Testing & Debugging](#testing--debugging)
8. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
┌─────────────────────┐
│   Vue Component     │  (pages/Programs.vue)
│   (Frontend)        │
└──────────┬──────────┘
		   │
		   │ imports
		   ↓
┌─────────────────────┐
│   API Service       │  (src/services/api.js)
│   (axios wrapper)   │
└──────────┬──────────┘
		   │
		   │ HTTP calls
		   ↓
┌─────────────────────┐
│  ASP.NET Controller │  (Controllers/ProgramsController.cs)
│  (Backend API)      │
└──────────┬──────────┘
		   │
		   │ queries
		   ↓
┌─────────────────────┐
│   SQL Database      │  (LocalDB)
│   (Persistent)      │
└─────────────────────┘
```

**Flow:**
- Component → imports API service → makes HTTP request → Backend → Database
- Database → sends response → Frontend → Component updates UI

---

## Setup Checklist

Before starting integration for any new page:

- [ ] Backend has a Controller file (e.g., `Controllers/ProgramsController.cs`)
- [ ] Controller has `[ApiController]` and `[Route("api/[controller]")]` attributes
- [ ] Controller has methods: `GetAll()`, `GetById(id)`, `Create(data)`, `Update(id, data)`, `Delete(id)`
- [ ] Backend is running: `http://localhost:5081`
- [ ] Frontend `.env.development` has: `VITE_API_URL=http://localhost:5081/api`
- [ ] Database migrations are up to date: `dotnet ef database update`
- [ ] API Service has methods exported: `export const itemsAPI = { ... }`
- [ ] Vue component imports the API service

---

## Step-by-Step Integration

### Step 1: Add API Functions to `src/services/api.js`

If not already there, add your API endpoints:

```javascript
// Example: Add this to src/services/api.js

export const scheolsAPI = {
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

**Pattern:** Replace `schools` with your entity name (programs, interns, requests, etc.)

---

### Step 2: Create Vue Component

Create or update your component file (e.g., `pages/Schools.vue`):

```vue
<template>
  <section class="content">
	<div class="card modern-card">
	  <div class="card-header modern-card-header">
		<h3 class="card-title mb-0">Schools</h3>
		<div class="card-tools">
		  <button 
			type="button" 
			class="btn btn-primary btn-sm modern-btn"
			@click="openCreate"
			:disabled="isLoading"
		  >
			<i class="fas fa-plus"></i> Add School
		  </button>
		</div>
	  </div>

	  <div class="card-body">
		<!-- Error Message -->
		<div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
		  {{ errorMessage }}
		  <button type="button" class="close" @click="errorMessage = ''">
			<span>&times;</span>
		  </button>
		</div>

		<!-- Loading Spinner -->
		<div v-if="isLoading" class="text-center py-4">
		  <div class="spinner-border" role="status">
			<span class="visually-hidden">Loading...</span>
		  </div>
		</div>

		<!-- Table -->
		<div v-if="!isLoading" class="table-responsive">
		  <table class="table table-hover table-bordered">
			<thead>
			  <tr class="text-center">
				<th>SCHOOL NAME</th>
				<th>ADDRESS</th>
				<th>ACTION</th>
			  </tr>
			</thead>
			<tbody>
			  <tr v-for="school in schools" :key="school.id" class="text-center">
				<td>{{ school.name }}</td>
				<td>{{ school.address }}</td>
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
				<td colspan="3" class="text-center text-muted py-4">No schools found.</td>
			  </tr>
			</tbody>
		  </table>
		</div>
	  </div>
	</div>

	<!-- Modal for Create/Edit -->
	<div v-if="isModalOpen" class="modal-backdrop fade show"></div>
	<div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog">
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
				<label for="schoolName" class="form-label">School Name <span class="text-danger">*</span></label>
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
				<label for="schoolAddress" class="form-label">Address <span class="text-danger">*</span></label>
				<input 
				  id="schoolAddress"
				  v-model="form.address" 
				  type="text"
				  class="form-control"
				  placeholder="Enter school address"
				  required
				/>
			  </div>
			</div>

			<div class="modal-footer">
			  <button type="button" class="btn btn-outline-secondary" @click="closeModal">
				<i class="fas fa-times"></i> Close
			  </button>
			  <button type="submit" class="btn btn-primary">
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

// State
const schools = ref([])
const isModalOpen = ref(false)
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const editingSchool = ref(null)
const form = ref({ name: '', address: '' })

// Load all schools from backend
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

// Open add schools modal
function openCreate() {
  editingSchool.value = null
  form.value = { name: '', address: '' }
  isModalOpen.value = true
}

// Open edit school modal
function openEdit(school) {
  editingSchool.value = school.id
  form.value = { name: school.name, address: school.address }
  isModalOpen.value = true
}

// Close modal
function closeModal() {
  isModalOpen.value = false
  form.value = { name: '', address: '' }
}

// Save school (create or update)
async function saveSchool() {
  if (!form.value.name.trim() || !form.value.address.trim()) {
	alert('Please fill in all fields')
	return
  }

  isSaving.value = true
  try {
	if (editingSchool.value === null) {
	  // Create new school
	  console.log('Creating new school:', form.value)
	  await schoolsAPI.create(form.value)
	} else {
	  // Update existing school
	  console.log('Updating school:', editingSchool.value, form.value)
	  await schoolsAPI.update(editingSchool.value, form.value)
	}

	// Reload all schools
	await loadSchools()
	closeModal()
  } catch (error) {
	console.error('Failed to save school:', error)
	alert(`Failed to save school: ${error.message}`)
  } finally {
	isSaving.value = false
  }
}

// Delete school
async function removeSchool(school) {
  if (!window.confirm(`Are you sure you want to delete "${school.name}"?`)) return

  try {
	console.log('Deleting school:', school.id)
	await schoolsAPI.delete(school.id)
	await loadSchools()
  } catch (error) {
	console.error('Failed to delete school:', error)
	alert(`Failed to delete school: ${error.message}`)
  }
}

// Load on mount
onMounted(loadSchools)
</script>

<style scoped>
/* Add any custom styles here */
</style>
```

---

## Component Template Examples

### Quick Reference: Template Structure

```vue
<!-- Template -->
<template>
  <section class="content">
	<!-- Error Alert -->
	<div v-if="errorMessage" class="alert alert-danger">
	  {{ errorMessage }}
	</div>

	<!-- Loading Spinner -->
	<div v-if="isLoading" class="text-center py-4">
	  <div class="spinner-border"></div>
	</div>

	<!-- Data Table -->
	<div v-if="!isLoading" class="table-responsive">
	  <table class="table table-hover table-bordered">
		<thead>
		  <tr>
			<th v-for="col in columns" :key="col">{{ col }}</th>
			<th>ACTION</th>
		  </tr>
		</thead>
		<tbody>
		  <tr v-for="item in items" :key="item.id">
			<!-- Display columns -->
			<td v-for="col in columns" :key="col">{{ item[col.toLowerCase()] }}</td>
			<td>
			  <button @click="openEdit(item)">Edit</button>
			  <button @click="removeItem(item)">Delete</button>
			</td>
		  </tr>
		  <tr v-if="items.length === 0">
			<td :colspan="columns.length + 1">No items found.</td>
		  </tr>
		</tbody>
	  </table>
	</div>

	<!-- Add Button -->
	<button @click="openCreate" class="btn btn-primary">
	  <i class="fas fa-plus"></i> Add Item
	</button>

	<!-- Modal -->
	<div v-if="isModalOpen" class="modal show" style="display: block">
	  <div class="modal-content">
		<h5>{{ editingItem === null ? 'Add' : 'Edit' }} Item</h5>
		<form @submit.prevent="saveItem">
		  <!-- Form inputs -->
		  <button type="submit">Save</button>
		  <button type="button" @click="closeModal">Cancel</button>
		</form>
	  </div>
	</div>
  </section>
</template>
```

---

## Complete Working Example

Here's the **Programs.vue** that's already implemented. Use it as reference:

### File: `pages/Programs.vue`

Your Programs component already has:
- ✅ Load all programs on mount
- ✅ Add new program (modal form + POST)
- ✅ Edit existing program (populate form + PUT)
- ✅ Delete program (confirmation + DELETE)
- ✅ Error handling
- ✅ Loading states

**View the actual file** to see all the patterns implemented.

---

## Common Patterns

### Pattern 1: Load Data on Page Mount

```javascript
import { onMounted, ref } from 'vue'
import { itemsAPI } from '../src/services/api'

const items = ref([])

async function loadItems() {
  try {
	const data = await itemsAPI.getAll()
	items.value = Array.isArray(data) ? data : data.data || []
  } catch (error) {
	console.error('Error:', error)
  }
}

onMounted(loadItems)
```

---

### Pattern 2: Handle Form Submission

```javascript
async function saveItem() {
  // Validation
  if (!form.value.name.trim()) {
	alert('Name is required')
	return
  }

  try {
	if (editingItem.value === null) {
	  // CREATE
	  await itemsAPI.create(form.value)
	} else {
	  // UPDATE
	  await itemsAPI.update(editingItem.value, form.value)
	}

	// Refresh data
	await loadItems()
	closeModal()
  } catch (error) {
	alert(`Error: ${error.message}`)
  }
}
```

---

### Pattern 3: Delete with Confirmation

```javascript
async function removeItem(item) {
  // Ask user first
  if (!window.confirm(`Delete "${item.name}"?`)) return

  try {
	await itemsAPI.delete(item.id)
	await loadItems()
  } catch (error) {
	alert(`Failed to delete: ${error.message}`)
  }
}
```

---

### Pattern 4: Open Modal for Edit

```javascript
function openEdit(item) {
  editingItem.value = item.id  // Store ID for later update
  form.value = {               // Copy data to form
	name: item.name,
	address: item.address
  }
  isModalOpen.value = true     // Show modal
}
```

---

## Testing & Debugging

### Test Checklist for Every Page

After integrating a page:

- [ ] **Read (GET):** Page loads and shows items
- [ ] **Create (POST):** Add new item, verify it appears in table
- [ ] **Refresh:** Reload page, item is still there
- [ ] **Update (PUT):** Edit item, verify changes appear
- [ ] **Delete (DELETE):** Remove item, verify it's gone
- [ ] **Error Handling:** Backend down → shows error message
- [ ] **Loading State:** Shows spinner during API calls
- [ ] **Empty State:** Shows "No items found" when empty

### Debug Using DevTools

1. **Open DevTools:** F12
2. **Network Tab:** See all API calls
   - Should see `GET /api/schools`
   - Should see `POST /api/schools` (when creating)
   - Should see `PUT /api/schools/1` (when updating)
   - Should see `DELETE /api/schools/1` (when deleting)
3. **Response:** Click each request to see response data
4. **Console:** Check for errors

### Manual API Test

Open PowerShell and test:

```powershell
# Test Read
curl http://localhost:5081/api/schools

# Test Create
$body = @{name="Test School"; address="123 Main St"} | ConvertTo-Json
Invoke-WebRequest -Uri http://localhost:5081/api/schools `
  -Method POST -Headers @{"Content-Type"="application/json"} -Body $body

# Test Update
$body = @{name="Updated School"; address="456 Oak St"} | ConvertTo-Json
Invoke-WebRequest -Uri http://localhost:5081/api/schools/1 `
  -Method PUT -Headers @{"Content-Type"="application/json"} -Body $body

# Test Delete
Invoke-WebRequest -Uri http://localhost:5081/api/schools/1 -Method DELETE
```

---

## Troubleshooting

### Issue: "Failed to load items: Network Error"

**Solution:**
1. Check backend is running: `http://localhost:5081`
2. Verify `.env.development` has correct URL
3. Check CORS is enabled in backend

### Issue: "Status 500 Error"

**Solution:**
1. Check backend console for error details
2. Database might not exist: Run `dotnet ef database update`
3. Missing migration: Run `dotnet ef migrations add NewMigration`

### Issue: "Timeout"

**Solution:**
1. Increase timeout in `src/services/api.js`:
```javascript
const apiClient = axios.create({
  timeout: 30000,  // 30 seconds instead of 10
  ...
})
```

### Issue: "Model has pending changes"

**Solution:**
```powershell
# Create migration
dotnet ef migrations add DescribingYourChanges

# Apply migration
dotnet ef database update
```

### Issue: Data doesn't persist after refresh

**Solution:**
1. Verify status code is `201` (create) or `200` (update) in Network tab
2. Check database has data: Open SQL Server Object Explorer in VS
3. Verify GET endpoint returns the data

---

## Quick Integration Checklist

When adding a new page (e.g., Signatories):

### Frontend
- [ ] Create `pages/Signatories.vue`
- [ ] Import API: `import { signatoriesAPI } from '../src/services/api'`
- [ ] Copy template from working example (Programs.vue)
- [ ] Update variable names (schools → signatories)
- [ ] Update API calls (schoolsAPI → signatoriesAPI)
- [ ] Test in browser

### Backend
- [ ] Verify `Controllers/SignatoriesController.cs` exists
- [ ] Verify methods: GetAll, GetById, Create, Update, Delete
- [ ] Check `[ApiController]` and `[Route("api/[controller]")]` attributes
- [ ] Run `dotnet ef migrations add Signatories`
- [ ] Run `dotnet ef database update`
- [ ] Restart backend: `dotnet run`

### Testing
- [ ] Load page → shows "No items found"
- [ ] Add item → appears in table
- [ ] Refresh page → item still there ✅
- [ ] Edit item → changes save
- [ ] Delete item → removed from table

---

## API Service Template

When adding new endpoints to `src/services/api.js`:

```javascript
// TEMPLATE - Copy and modify
export const itemsAPI = {
  getAll: async () => {
	const response = await apiClient.get('/items')
	return response.data
  },

  getById: async (id) => {
	const response = await apiClient.get(`/items/${id}`)
	return response.data
  },

  create: async (data) => {
	const response = await apiClient.post('/items', data)
	return response.data
  },

  update: async (id, data) => {
	const response = await apiClient.put(`/items/${id}`, data)
	return response.data
  },

  delete: async (id) => {
	const response = await apiClient.delete(`/items/${id}`)
	return response.data
  }
}
```

**Just replace:**
- `items` with your entity name
- `/items` with `/yourcontroller`

---

## COMPLETE CODE TEMPLATES

### Template 1: Full Vue Component (Copy-Paste Ready)

**File:** `pages/YourPage.vue`

```vue
<template>
  <section class="content">
	<div class="card modern-card">
	  <div class="card-header modern-card-header">
		<h3 class="card-title mb-0">YOUR_PAGE_TITLE</h3>
		<div class="card-tools">
		  <button 
			type="button" 
			class="btn btn-primary btn-sm modern-btn intern-request-action"
			@click="openCreate"
			:disabled="isLoading"
		  >
			<i class="fas fa-plus"></i> Add YOUR_ITEM_NAME
		  </button>
		</div>
	  </div>

	  <div class="card-body">
		<!-- Error Alert -->
		<div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
		  {{ errorMessage }}
		  <button type="button" class="close" @click="errorMessage = ''">
			<span>&times;</span>
		  </button>
		</div>

		<!-- Loading Spinner -->
		<div v-if="isLoading" class="text-center py-4">
		  <div class="spinner-border" role="status">
			<span class="visually-hidden">Loading...</span>
		  </div>
		</div>

		<!-- Data Table -->
		<div v-if="!isLoading" class="table-responsive">
		  <table class="table table-hover table-bordered">
			<thead>
			  <tr class="text-center">
				<th>COLUMN 1</th>
				<th>COLUMN 2</th>
				<th>ACTION</th>
			  </tr>
			</thead>
			<tbody>
			  <tr v-for="item in items" :key="item.id" class="text-center">
				<td>{{ item.property1 }}</td>
				<td>{{ item.property2 }}</td>
				<td>
				  <button 
					type="button" 
					class="btn btn-outline-primary btn-sm modern-action mr-1"
					@click="openEdit(item)"
				  >
					<i class="fas fa-edit"></i> Edit
				  </button>
				  <button 
					type="button" 
					class="btn btn-outline-danger btn-sm modern-action"
					@click="removeItem(item)"
				  >
					<i class="fas fa-times"></i> Delete
				  </button>
				</td>
			  </tr>
			  <tr v-if="items.length === 0">
				<td colspan="3" class="text-center text-muted py-4">No items found.</td>
			  </tr>
			</tbody>
		  </table>
		</div>
	  </div>
	</div>

	<!-- Modal -->
	<div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
	<div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true">
	  <div class="modal-dialog modal-dialog-centered modal-md">
		<div class="modal-content">
		  <div class="modal-header bg-orange py-2">
			<h5 class="modal-title">
			  {{ editingItem === null ? 'Add New Item' : 'Edit Item' }}
			</h5>
			<button type="button" class="close" @click="closeModal">
			  <span>&times;</span>
			</button>
		  </div>

		  <form @submit.prevent="saveItem">
			<div class="modal-body">
			  <div class="form-group mb-3">
				<label for="itemProperty1" class="form-label">
				  Property 1 <span class="text-danger">*</span>
				</label>
				<input 
				  id="itemProperty1"
				  v-model="form.property1" 
				  type="text"
				  class="form-control"
				  placeholder="Enter property 1"
				  required
				/>
			  </div>

			  <div class="form-group mb-3">
				<label for="itemProperty2" class="form-label">
				  Property 2 <span class="text-danger">*</span>
				</label>
				<textarea 
				  id="itemProperty2"
				  v-model="form.property2" 
				  class="form-control"
				  placeholder="Enter property 2"
				  rows="3"
				  required
				></textarea>
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
import { itemsAPI } from '../src/services/api'

// State Management
const items = ref([])
const isModalOpen = ref(false)
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const editingItem = ref(null)
const form = ref({ property1: '', property2: '' })

// Load all items
async function loadItems() {
  isLoading.value = true
  errorMessage.value = ''
  try {
	const data = await itemsAPI.getAll()
	items.value = Array.isArray(data) ? data : (data.data || [])
	console.log('Items loaded:', items.value)
  } catch (error) {
	console.error('Failed to load items:', error)
	errorMessage.value = `Failed to load items: ${error.message}`
  } finally {
	isLoading.value = false
  }
}

// Open add modal
function openCreate() {
  editingItem.value = null
  form.value = { property1: '', property2: '' }
  isModalOpen.value = true
}

// Open edit modal
function openEdit(item) {
  editingItem.value = item.id
  form.value = { property1: item.property1, property2: item.property2 }
  isModalOpen.value = true
}

// Close modal
function closeModal() {
  isModalOpen.value = false
  form.value = { property1: '', property2: '' }
}

// Save (create or update)
async function saveItem() {
  if (!form.value.property1.trim() || !form.value.property2.trim()) {
	alert('Please fill in all fields')
	return
  }

  isSaving.value = true
  try {
	if (editingItem.value === null) {
	  await itemsAPI.create(form.value)
	} else {
	  await itemsAPI.update(editingItem.value, form.value)
	}
	await loadItems()
	closeModal()
  } catch (error) {
	console.error('Failed to save item:', error)
	alert(`Failed to save item: ${error.message}`)
  } finally {
	isSaving.value = false
  }
}

// Delete
async function removeItem(item) {
  if (!window.confirm(`Are you sure you want to delete "${item.property1}"?`)) return
  try {
	await itemsAPI.delete(item.id)
	await loadItems()
  } catch (error) {
	console.error('Failed to delete item:', error)
	alert(`Failed to delete item: ${error.message}`)
  }
}

onMounted(loadItems)
</script>

<style scoped>
/* Custom styles here */
</style>
```

---

### Template 2: Backend Controller (Copy-Paste Ready)

**File:** `Controllers/ItemsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ItemsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<ItemsController> _logger;

		public ItemsController(ApplicationDbContext context, ILogger<ItemsController> logger)
		{
			_context = context;
			_logger = logger;
		}

		// GET: api/items
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Item>>> GetItems()
		{
			try
			{
				var items = await _context.Items.ToListAsync();
				return Ok(items);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error fetching items: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		// GET: api/items/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Item>> GetItem(int id)
		{
			try
			{
				var item = await _context.Items.FindAsync(id);
				if (item == null)
				{
					return NotFound(new { message = "Item not found" });
				}
				return Ok(item);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error fetching item: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		// POST: api/items
		[HttpPost]
		public async Task<ActionResult<Item>> PostItem(Item item)
		{
			try
			{
				if (item == null)
				{
					return BadRequest(new { message = "Item data is required" });
				}

				_context.Items.Add(item);
				await _context.SaveChangesAsync();

				return CreatedAtAction("GetItem", new { id = item.Id }, item);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating item: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		// PUT: api/items/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutItem(int id, Item item)
		{
			try
			{
				if (id != item.Id)
				{
					return BadRequest(new { message = "ID mismatch" });
				}

				_context.Entry(item).State = EntityState.Modified;
				await _context.SaveChangesAsync();

				return Ok(item);
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ItemExists(id))
				{
					return NotFound(new { message = "Item not found" });
				}
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error updating item: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		// DELETE: api/items/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteItem(int id)
		{
			try
			{
				var item = await _context.Items.FindAsync(id);
				if (item == null)
				{
					return NotFound(new { message = "Item not found" });
				}

				_context.Items.Remove(item);
				await _context.SaveChangesAsync();

				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deleting item: {ex.Message}");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		private bool ItemExists(int id)
		{
			return _context.Items.Any(e => e.Id == id);
		}
	}
}
```

---

### Template 3: Model Class (Copy-Paste Ready)

**File:** `Models/Item.cs`

```csharp
namespace OJTMISApi.Models
{
	public class Item
	{
		public int Id { get; set; }

		public string Property1 { get; set; }

		public string Property2 { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}
```

**Then add to `Data/ApplicationDbContext.cs`:**

```csharp
public DbSet<Item> Items { get; set; }
```

---

### Template 4: Form Input Fields

**Text Input:**
```vue
<div class="form-group mb-3">
  <label for="textField" class="form-label">
	Text Field <span class="text-danger">*</span>
  </label>
  <input 
	id="textField"
	v-model="form.textField" 
	type="text"
	class="form-control"
	placeholder="Enter text"
	required
  />
</div>
```

**Email Input:**
```vue
<div class="form-group mb-3">
  <label for="email" class="form-label">
	Email <span class="text-danger">*</span>
  </label>
  <input 
	id="email"
	v-model="form.email" 
	type="email"
	class="form-control"
	placeholder="Enter email"
	required
  />
</div>
```

**Number Input:**
```vue
<div class="form-group mb-3">
  <label for="number" class="form-label">
	Number <span class="text-danger">*</span>
  </label>
  <input 
	id="number"
	v-model.number="form.number" 
	type="number"
	class="form-control"
	placeholder="Enter number"
	required
  />
</div>
```

**Textarea:**
```vue
<div class="form-group mb-3">
  <label for="description" class="form-label">
	Description <span class="text-danger">*</span>
  </label>
  <textarea 
	id="description"
	v-model="form.description" 
	class="form-control"
	placeholder="Enter description"
	rows="4"
	required
  ></textarea>
</div>
```

**Select Dropdown:**
```vue
<div class="form-group mb-3">
  <label for="status" class="form-label">
	Status <span class="text-danger">*</span>
  </label>
  <select 
	id="status"
	v-model="form.status"
	class="form-control"
	required
  >
	<option value="">-- Select Status --</option>
	<option value="Active">Active</option>
	<option value="Inactive">Inactive</option>
  </select>
</div>
```

**Date Input:**
```vue
<div class="form-group mb-3">
  <label for="dateField" class="form-label">
	Date <span class="text-danger">*</span>
  </label>
  <input 
	id="dateField"
	v-model="form.dateField" 
	type="date"
	class="form-control"
	required
  />
</div>
```

**Checkbox:**
```vue
<div class="form-check mb-3">
  <input 
	id="isActive"
	v-model="form.isActive" 
	type="checkbox"
	class="form-check-input"
  />
  <label for="isActive" class="form-check-label">
	Is Active?
  </label>
</div>
```

---

### Template 5: API Service (Copy-Paste Ready)

**Add this to `src/services/api.js` at the end:**

```javascript
// ============ ITEMS API ============
export const itemsAPI = {
  getAll: async () => {
	const response = await apiClient.get('/items')
	return response.data
  },

  getById: async (id) => {
	const response = await apiClient.get(`/items/${id}`)
	return response.data
  },

  create: async (data) => {
	const response = await apiClient.post('/items', data)
	return response.data
  },

  update: async (id, data) => {
	const response = await apiClient.put(`/items/${id}`, data)
	return response.data
  },

  delete: async (id) => {
	const response = await apiClient.delete(`/items/${id}`)
	return response.data
  }
}
```

---

## Adding Your First New Page (Step-by-Step with Templates)

### Step 1: Create Model
Copy Template 3, modify property names, save to `Models/Item.cs`

### Step 2: Add DbSet
In `Data/ApplicationDbContext.cs`, add:
```csharp
public DbSet<Item> Items { get; set; }
```

### Step 3: Create Migration
```powershell
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi
dotnet ef migrations add AddItems
dotnet ef database update
```

### Step 4: Create Controller
Copy Template 2, modify all `Item` → `YourModel`, save to `Controllers/ItemsController.cs`

### Step 5: Add API Service
Copy Template 5 to end of `src/services/api.js`

### Step 6: Create Component
Copy Template 1 to `pages/YourPage.vue`, update variable names

### Step 7: Test
```
1. dotnet run (backend)
2. npm run dev (frontend)
3. Navigate to page
4. Add item → Refresh → Should persist ✅
```

---

## Environment Variables

Your configuration files:

### `.env.development` (Development/Local)
```
VITE_API_URL=http://localhost:5081/api
```

### `.env.production` (Production/Deployed)
```
VITE_API_URL=https://your-domain.com/api
```

When you build for production: `npm run build`

---

## Summary

**Steps to add a new page:**

1. **Add API service** to `src/services/api.js` (copy template)
2. **Copy component** from `pages/Programs.vue`
3. **Update variable names** to match your entity
4. **Verify backend controller** exists with all CRUD methods
5. **Run migrations** if needed
6. **Test all CRUD operations**
7. ✅ Done!

---

## Questions?

Refer back to this guide, or check:
- `pages/Programs.vue` — working example
- `src/services/api.js` — API definitions
- Backend console output — error details
- Browser DevTools Network tab — HTTP requests/responses

Good luck! 🚀

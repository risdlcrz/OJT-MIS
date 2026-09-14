# Frontend-Backend Integration: Quick Reference Card

Print this or bookmark for quick lookup!

---

## 🚀 Quick Start: Add a New Page in 5 Steps

### Step 1: Add API Service (1 min)
**File:** `src/services/api.js`
```javascript
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

### Step 2: Create Vue Component (5 min)
**File:** `pages/Items.vue`

Copy from: `pages/Programs.vue` and change:
- `programsAPI` → `itemsAPI`
- `programs` → `items`
- `program` → `item`
- Column names in template

---

### Step 3: Verify Backend Controller (2 min)
**File:** `Controllers/ItemsController.cs`

Must have:
```csharp
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
	[HttpGet]
	public async Task<ActionResult<IEnumerable<Item>>> GetItems() { ... }

	[HttpPost]
	public async Task<ActionResult<Item>> PostItem(Item item) { ... }

	[HttpPut("{id}")]
	public async Task<IActionResult> PutItem(int id, Item item) { ... }

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteItem(int id) { ... }
}
```

---

### Step 4: Update Database (2 min)
**PowerShell:**
```powershell
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi

# Add migration
dotnet ef migrations add AddItems

# Apply to database
dotnet ef database update
```

---

### Step 5: Restart & Test (1 min)
```powershell
# Restart backend
dotnet run

# Test in browser
# http://localhost:5173/items
```

✅ **Done! You should see "No items found"**

---

## 📋 Component Script Template

```javascript
import { onMounted, ref } from 'vue'
import { itemsAPI } from '../src/services/api'

const items = ref([])
const isLoading = ref(false)
const errorMessage = ref('')
const isModalOpen = ref(false)
const editingItem = ref(null)
const form = ref({ name: '', description: '' })

// READ - Get all items
async function loadItems() {
  isLoading.value = true
  try {
	const data = await itemsAPI.getAll()
	items.value = Array.isArray(data) ? data : data.data || []
  } catch (error) {
	errorMessage.value = error.message
  } finally {
	isLoading.value = false
  }
}

// CREATE - Show add modal
function openCreate() {
  editingItem.value = null
  form.value = { name: '', description: '' }
  isModalOpen.value = true
}

// UPDATE - Show edit modal
function openEdit(item) {
  editingItem.value = item.id
  form.value = { name: item.name, description: item.description }
  isModalOpen.value = true
}

// SAVE - Create or Update
async function saveItem() {
  try {
	if (editingItem.value === null) {
	  await itemsAPI.create(form.value)
	} else {
	  await itemsAPI.update(editingItem.value, form.value)
	}
	await loadItems()
	closeModal()
  } catch (error) {
	alert(error.message)
  }
}

// DELETE - Remove item
async function removeItem(item) {
  if (!window.confirm(`Delete "${item.name}"?`)) return
  try {
	await itemsAPI.delete(item.id)
	await loadItems()
  } catch (error) {
	alert(error.message)
  }
}

function closeModal() {
  isModalOpen.value = false
  form.value = { name: '', description: '' }
}

onMounted(loadItems)
```

---

## 🧪 Testing Checklist

| Test | Command/Action | Expected Result |
|------|---|---|
| **READ** | Navigate to page | Shows items or "No items found" |
| **CREATE** | Click "Add", fill form, save | Item appears in table |
| **PERSIST** | Refresh page (F5) | Item still there ✅ |
| **UPDATE** | Click "Edit", change value, save | Changes show in table |
| **DELETE** | Click "Delete", confirm | Item removed from table |
| **ERROR** | Stop backend | Shows error message |
| **LOADING** | During API call | Shows spinner |

---

## 🔍 Debugging

### Check Network Requests (F12 → Network Tab)

| Operation | Expected URL | Expected Status |
|-----------|---|---|
| Load page | `GET /api/items` | `200 OK` |
| Create | `POST /api/items` | `201 Created` |
| Update | `PUT /api/items/1` | `200 OK` |
| Delete | `DELETE /api/items/1` | `200 OK` or `204 No Content` |

### Test API Manually
```powershell
# Read all
curl http://localhost:5081/api/items

# Create
$body = '{"name":"Test","description":"Test"}' 
Invoke-WebRequest -Uri http://localhost:5081/api/items -Method POST `
  -Headers @{"Content-Type"="application/json"} -Body $body

# Read one
curl http://localhost:5081/api/items/1

# Update
Invoke-WebRequest -Uri http://localhost:5081/api/items/1 -Method PUT `
  -Headers @{"Content-Type"="application/json"} -Body $body

# Delete
Invoke-WebRequest -Uri http://localhost:5081/api/items/1 -Method DELETE
```

---

## 🐛 Common Issues & Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| Network Error | Backend not running | `dotnet run` in backend directory |
| 500 Error | Server crash (check console) | Look at backend terminal for error |
| Timeout | DB not responding | Run `dotnet ef database update` |
| 404 Not Found | Wrong API endpoint | Check controller name matches |
| Data disappears after refresh | API not working | Check GET returns same data |
| Modal won't close | onClick not firing | Check `@click` bindings |
| Form not clearing | Ref not reset | Call `closeModal()` after save |

---

## 📁 File Structure

```
OJT-MIS/
├── pages/
│   ├── Programs.vue        ← Copy this as template
│   ├── Schools.vue         ← New page
│   ├── Signatories.vue     ← New page
│   └── ...
├── src/
│   └── services/
│       └── api.js          ← Add API functions here
├── .env.development        ← Check API_URL
└── OJTMISApi/
	└── OJTMISApi/
		├── Controllers/
		│   ├── ProgramsController.cs
		│   ├── SchoolsController.cs
		│   └── ...
		└── Program.cs      ← Should have app.MapControllers()
```

---

## 🎯 Variables to Always Replace

When copying code:

```
programs         → items
programsAPI      → itemsAPI
program          → item
editingProgram   → editingItem
Program Name     → Item Name
/api/programs    → /api/items
```

---

## 🔧 CODE TEMPLATES (Copy & Paste Ready)

### ✨ Template 1: Complete Vue Component (Copy to pages/)

```vue
<template>
  <section class="content">
	<div class="card modern-card">
	  <div class="card-header modern-card-header">
		<h3 class="card-title mb-0">YOUR_TITLE_HERE</h3>
		<div class="card-tools">
		  <button 
			type="button" 
			class="btn btn-primary btn-sm modern-btn intern-request-action"
			@click="openCreate"
			:disabled="isLoading"
		  >
			<i class="fas fa-plus"></i> Add YOUR_ITEM
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

		<!-- Table -->
		<div v-if="!isLoading" class="table-responsive">
		  <table class="table table-hover table-bordered">
			<thead>
			  <tr class="text-center">
				<th>COLUMN_NAME_1</th>
				<th>COLUMN_NAME_2</th>
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

	<!-- Modal Backdrop -->
	<div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>

	<!-- Modal -->
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
				<label for="itemName" class="form-label">
				  Item Name <span class="text-danger">*</span>
				</label>
				<input 
				  id="itemName"
				  v-model="form.name" 
				  type="text"
				  class="form-control"
				  placeholder="Enter item name"
				  required
				/>
			  </div>

			  <div class="form-group mb-3">
				<label for="itemDesc" class="form-label">
				  Description <span class="text-danger">*</span>
				</label>
				<textarea 
				  id="itemDesc"
				  v-model="form.description" 
				  class="form-control"
				  placeholder="Enter description"
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

// State
const items = ref([])
const isModalOpen = ref(false)
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const editingItem = ref(null)
const form = ref({ name: '', description: '' })

// Load all items from backend
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

// Open add item modal
function openCreate() {
  editingItem.value = null
  form.value = { name: '', description: '' }
  isModalOpen.value = true
}

// Open edit item modal
function openEdit(item) {
  editingItem.value = item.id
  form.value = { name: item.name, description: item.description }
  isModalOpen.value = true
}

// Close modal
function closeModal() {
  isModalOpen.value = false
  form.value = { name: '', description: '' }
}

// Save item (create or update)
async function saveItem() {
  if (!form.value.name.trim() || !form.value.description.trim()) {
	alert('Please fill in all fields')
	return
  }

  isSaving.value = true
  try {
	if (editingItem.value === null) {
	  // Create new item
	  console.log('Creating new item:', form.value)
	  await itemsAPI.create(form.value)
	} else {
	  // Update existing item
	  console.log('Updating item:', editingItem.value, form.value)
	  await itemsAPI.update(editingItem.value, form.value)
	}

	// Reload all items
	await loadItems()
	closeModal()
  } catch (error) {
	console.error('Failed to save item:', error)
	alert(`Failed to save item: ${error.message}`)
  } finally {
	isSaving.value = false
  }
}

// Delete item
async function removeItem(item) {
  if (!window.confirm(`Are you sure you want to delete "${item.name}"?`)) return

  try {
	console.log('Deleting item:', item.id)
	await itemsAPI.delete(item.id)
	await loadItems()
  } catch (error) {
	console.error('Failed to delete item:', error)
	alert(`Failed to delete item: ${error.message}`)
  }
}

// Load on mount
onMounted(loadItems)
</script>

<style scoped>
/* Add any custom styles here */
</style>
```

**👉 HOW TO USE:**
1. Copy entire template to `pages/YOUR_PAGE_NAME.vue`
2. Replace `YOUR_TITLE_HERE` with your title (e.g., "Schools")
3. Replace `YOUR_ITEM` with singular name (e.g., "School")
4. Replace `itemsAPI` → `schoolsAPI` (match your API)
5. Replace `property1`, `property2` → your field names
6. Add/remove form fields as needed
7. Done!

---

### ✨ Template 2: API Service (Copy to src/services/api.js)

**Add this entire block to `src/services/api.js` at the end:**

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

**👉 REPLACEMENTS:**
- `ITEMS` → `SCHOOLS`, `SIGNATORIES`, `INTERNS`, etc.
- `items` → `schools`, `signatories`, `interns`, etc.
- `/items` → `/schools`, `/signatories`, `/interns`, etc. (match controller name)

---

### ✨ Template 3: Backend Controller (Copy to Controllers/)

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

**👉 REPLACEMENTS:**
- `Items` → `Schools`, `Signatories`, etc.
- `Item` → `School`, `Signatory`, etc.
- `_context.Items` → `_context.Schools`, etc.
- `ItemsController` → `SchoolsController`, etc.

---

### ✨ Template 4: Model Class (Copy to Models/)

**File:** `Models/Item.cs`

```csharp
namespace OJTMISApi.Models
{
	public class Item
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}
```

**👉 NEXT STEP:** Add to `Data/ApplicationDbContext.cs`:

```csharp
public DbSet<Item> Items { get; set; }
```

---

### 📝 Template 5: Form Input Fields (Paste into modal-body)

**Text Input:**
```vue
<div class="form-group mb-3">
  <label for="fieldName" class="form-label">
	Field Name <span class="text-danger">*</span>
  </label>
  <input 
	id="fieldName"
	v-model="form.fieldName" 
	type="text"
	class="form-control"
	placeholder="Enter field name"
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
  <label for="age" class="form-label">
	Age <span class="text-danger">*</span>
  </label>
  <input 
	id="age"
	v-model.number="form.age" 
	type="number"
	class="form-control"
	placeholder="Enter age"
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
	rows="3"
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
  <label for="birthDate" class="form-label">
	Birth Date <span class="text-danger">*</span>
  </label>
  <input 
	id="birthDate"
	v-model="form.birthDate" 
	type="date"
	class="form-control"
	required
  />
</div>
```

---

### 🗂️ Template 6: When You Get DbSet Error

**File:** `Data/ApplicationDbContext.cs`

Find the section with other DbSets and add:

```csharp
public DbSet<Item> Items { get; set; }
```

**Example:**
```csharp
public DbSet<OJTProgram> Programs { get; set; }
public DbSet<School> Schools { get; set; }  ← Add here
public DbSet<Signatory> Signatories { get; set; }
```

---

### 🚀 Template 7: Full Integration Checklist

Follow these steps IN ORDER:

**1️⃣ Create Model** (`Models/Item.cs`)
- Copy Template 4

**2️⃣ Add DbSet** (`Data/ApplicationDbContext.cs`)
```csharp
public DbSet<Item> Items { get; set; }
```

**3️⃣ Create Migration**
```powershell
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi
dotnet ef migrations add AddItems
```

**4️⃣ Apply Migration**
```powershell
dotnet ef database update
```

**5️⃣ Create Controller** (`Controllers/ItemsController.cs`)
- Copy Template 3

**6️⃣ Add API Service** (`src/services/api.js`)
- Scroll to bottom, add Template 2

**7️⃣ Create Component** (`pages/Items.vue`)
- Copy Template 1
- Update variables

**8️⃣ Test:**
```
1. Start backend: dotnet run
2. Start frontend: npm run dev
3. Go to http://localhost:5173/items
4. Click "Add Item"
5. Fill form, click Save
6. Refresh page (F5)
7. Item should still be there! ✅
```

---



---

## 🚦 Status Codes to Expect

| Code | Meaning | Action |
|------|---------|--------|
| 200 | OK (Read, Update, Delete success) | Data loaded/saved |
| 201 | Created (Create success) | Item created, reload list |
| 400 | Bad Request | Check form data |
| 401 | Unauthorized | Check authentication |
| 404 | Not Found | Check endpoint URL |
| 500 | Server Error | Check backend console |
| Timeout | Backend not responding | Restart backend/DB |

---

## 💾 Database Commands

```powershell
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi

# See pending migrations
dotnet ef migrations list

# Create new migration
dotnet ef migrations add DescribeYourChange

# Apply migrations
dotnet ef database update

# Remove last migration (undo)
dotnet ef migrations remove

# Update to specific migration
dotnet ef database update MigrationName
```

---

## 🌐 Environment URLs

| Environment | Frontend | Backend |
|---|---|---|
| **Development** | http://localhost:5173 | http://localhost:5081 |
| **Production** | your-domain.com | your-domain.com (same server) |

Change in `.env.development` or `.env.production`

---

## ✅ Final Checklist Before Deploying New Page

- [ ] API service added to `src/services/api.js`
- [ ] Vue component created in `pages/`
- [ ] Backend controller has all CRUD methods
- [ ] Backend has `app.MapControllers()` in `Program.cs`
- [ ] Database migration created and applied
- [ ] Backend running without errors
- [ ] Can READ (page loads)
- [ ] Can CREATE (add new item)
- [ ] Can UPDATE (edit item)
- [ ] Can DELETE (remove item)
- [ ] Persist test passes (refresh shows data)
- [ ] Error handling works (shows messages)
- [ ] Loading states display correctly

---

## 📞 Quick Help

**Backend won't start?**
```powershell
dotnet clean
dotnet build
dotnet run
```

**Database error?**
```powershell
dotnet ef database update
```

**Port already in use?**
- Kill process or change port in `launchSettings.json`

**Forgot API URL?**
- Check `.env.development`: `VITE_API_URL=http://localhost:5081/api`

**Data not showing?**
1. Check Network tab → Is GET returning data?
2. Check DevTools Console → Any JS errors?
3. Check Backend Console → Any server errors?

---

**Happy coding!** 🚀

# Copy-Paste Integration Checklist

**Use this when adding a new page (Schools, Signatories, etc.)**

---

## Step 1: Create Model
**File**: `OJTMISApi/OJTMISApi/Models/YourModel.cs`

Replace all instances of:
- `Item` → `Schools` (or your model name)

Copy from guide Template 3: **Backend Controller (Copy-Paste Ready)**

```csharp
namespace OJTMISApi.Models
{
	public class School
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Address { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}
```

---

## Step 2: Add DbSet
**File**: `OJTMISApi/OJTMISApi/Data/ApplicationDbContext.cs`

Find the section with other DbSets and add:
```csharp
public DbSet<School> Schools { get; set; }
```

---

## Step 3: Create Controller
**File**: `OJTMISApi/OJTMISApi/Controllers/SchoolsController.cs`

Copy from guide Template 2: **Backend Controller (Copy-Paste Ready)**

Replace every instance of:
- `Items` → `Schools`
- `Item` → `School`
- `items` → `schools`
- `item` → `school`
- `ItemsController` → `SchoolsController`

---

## Step 4: Run Migrations

**PowerShell - IN BACKEND DIRECTORY:**
```powershell
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi

# Create migration
dotnet ef migrations add AddSchools

# Apply to database
dotnet ef database update
```

**Expected output:**
```
Applying migration '20240115123456_AddSchools'.
Done. Database updated.
```

---

## Step 5: Add API Service
**File**: `src/services/api.js`

Scroll to the **VERY END** of the file and add:

```javascript
// ============ SCHOOLS API ============
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

Replace all instances of:
- `schools` → `yourcontroller` 
- `SCHOOLS` → `YOUR_CONTROLLER`
- `schoolsAPI` → `yourcontrollerAPI`

---

## Step 6: Create Vue Component
**File**: `pages/Schools.vue`

Copy from guide Template 1: **Full Vue Component (Copy-Paste Ready)**

Replace every instance of:
- `Your_Page_Title` → `Schools`
- `YOUR_ITEM_NAME` → `School`
- `itemsAPI` → `schoolsAPI`
- `items` → `schools`
- `item` → `school`
- `editingItem` → `editingSchool`
- `property1` → `name`
- `property2` → `address`
- `COLUMN 1` → `SCHOOL NAME`
- `COLUMN 2` → `ADDRESS`

**Also update the form fields:**

Replace this:
```vue
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
```

With this:
```vue
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
```

Also update the form initialization:
```javascript
const form = ref({ name: '', address: '' })
```

And the confirmations:
```javascript
if (!window.confirm(`Are you sure you want to delete "${school.name}"?`)) return
```

---

## Step 7: Restart Backend

**PowerShell:**
```powershell
# Kill the running backend (Ctrl+C)
# Then restart it
cd C:\Users\patricia\source\repos\OJT-MIS\OJTMISApi\OJTMISApi
dotnet run
```

Wait for:
```
Now listening on: http://localhost:5081
```

---

## Step 8: Test in Browser

1. Go to `http://localhost:5173/schools`
2. You should see: **"No schools found."**
3. Click **"+ Add School"**
4. Fill in:
   - School Name: `Test School`
   - Address: `123 Main Street`
5. Click **"Save"**
6. Should see the school in the table ✅
7. Press **F5** (refresh)
8. School still there? ✅ **SUCCESS!**

---

## Variables Cheat Sheet

When copying code, replace these:

| Template | Replace With |
|----------|---|
| `Items` | `Schools`, `Signatories`, `Interns` |
| `Item` | `School`, `Signatory`, `Intern` |
| `items` | `schools`, `signatories`, `interns` |
| `item` | `school`, `signatory`, `intern` |
| `itemsAPI` | `schoolsAPI`, `signatoriesAPI`, `internsAPI` |
| `ItemsController` | `SchoolsController`, `SignatoriesController` |
| `property1` | Your first field (name, abbrev, etc.) |
| `property2` | Your second field (address, level, etc.) |
| `/items` | `/schools`, `/signatories`, `/interns` |
| `Item` (model) | `School`, `Signatory`, `Intern` |

---

## Common Mistakes

❌ **WRONG:**
- Forget to add `DbSet` to ApplicationDbContext
- Forget to run migrations
- API service has wrong controller name
- Form fields don't match model properties
- Forgot `@click="openCreate"` on button
- Controller class doesn't have `[ApiController]` attribute

✅ **RIGHT:**
- Add `DbSet<School> Schools { get; set; }` 
- Run `dotnet ef migrations add AddSchools`
- Run `dotnet ef database update`
- Check property names match: `form.name` = `School.Name`
- All buttons have proper `@click` handlers
- Controller has `[ApiController]` and `[Route("api/[controller]")]`

---

## Quick Verification

After completing all steps:

**Backend**
- [ ] Model class created
- [ ] DbSet added 
- [ ] Migration created and applied
- [ ] Controller has all CRUD methods
- [ ] Backend running without errors

**Frontend**
- [ ] API service added to `api.js`
- [ ] Component created in `pages/`
- [ ] All variable names updated
- [ ] Form fields match your model

**Testing**
- [ ] Page loads: "No items found" ✓
- [ ] Add item works
- [ ] Item visible in table
- [ ] Refresh page (F5)
- [ ] Item still there ✓ **DONE!**

---

## If Something Broke

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

**API returning 404?**
- Check controller name matches URL
- Is `[ApiController]` attribute there?
- Does it have `[Route("api/[controller]")]`?

**Frontend shows error?**
- Check F12 DevTools Network tab
- Is the request going to correct URL?
- Is backend returning data?

**Data not saving?**
- Check Network tab - is POST returning 201?
- Check backend console for errors
- Is DbSet spelled correctly?

---

## Next Pages to Add

Use this same process for:
1. **Signatories** - `models/Signatory.cs`, `controllers/SignatoriesController.cs`
2. **Interns** - `models/Intern.cs`, `controllers/InternsController.cs`
3. **Requests** - `models/Request.cs`, `controllers/RequestsController.cs`
4. **Dashboard** - Adjust for read-only (no edit/delete)

---

## Support

- **Full Guide**: See `FRONTEND_BACKEND_INTEGRATION_GUIDE.md`
- **Quick Ref**: See `QUICK_REFERENCE_CARD.md`
- **Working Example**: Look at `pages/Programs.vue`
- **API Template**: See `src/services/api.js`

---

**Good luck! You've got this! 🚀**

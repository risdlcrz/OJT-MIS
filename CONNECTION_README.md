# OJT MIS - Frontend to API Connection Guide

## ✅ What's Been Done

The OJTMISApi has been successfully connected to the OJT MIS frontend. Here's what was implemented:

### Backend Setup (ASP.NET Core)
- ✅ Created database models for all entities:
  - Applicant
  - OJTProgram
  - School
  - Signatory
  - Intern
  - InternRequest
- ✅ Updated ApplicationDbContext with DbSets for all models
- ✅ Created REST API controllers for:
  - ApplicantsController
  - ProgramsController
  - SchoolsController
  - SignatoriesController
  - InternsController
  - RequestsController
  - DashboardController
- ✅ Implemented full CRUD operations (GET, POST, PUT, DELETE)
- ✅ Enabled CORS for cross-origin requests
- ✅ Added error handling and logging
- ✅ Updated appsettings.json with CORS configuration

### Frontend Setup (Vue 3)
- ✅ Added axios dependency to package.json
- ✅ Created centralized API service (`src/services/api.js`)
- ✅ Configured environment variables (.env files)
- ✅ Set up Vite proxy for development
- ✅ Created example refactored component using API service

## 🚀 Getting Started

### Step 1: Run the Backend

```bash
cd OJTMISApi/OJTMISApi

# Install dependencies
dotnet restore

# Create and apply database migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the API server
dotnet run
```

The API will be available at:
- https://localhost:7000 (HTTPS)
- http://localhost:5000 (HTTP)

### Step 2: Run the Frontend

```bash
# In the project root

# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at:
- http://localhost:5173

## 📝 Using the API Service in Vue Components

### Basic Import
```javascript
import { applicantsAPI } from '@/services/api'
```

### Complete Example: Fetch and Display Data
```vue
<script setup>
import { ref, onMounted } from 'vue'
import { applicantsAPI } from '@/services/api'

const applicants = ref([])
const loading = ref(false)
const error = ref(null)

onMounted(async () => {
  await fetchApplicants()
})

async function fetchApplicants() {
  loading.value = true
  error.value = null
  try {
	const response = await applicantsAPI.getAll()
	applicants.value = response.data
  } catch (err) {
	error.value = err.message
  } finally {
	loading.value = false
  }
}
</script>

<template>
  <div>
	<div v-if="loading">Loading...</div>
	<div v-if="error" class="alert alert-danger">{{ error }}</div>
	<ul>
	  <li v-for="applicant in applicants" :key="applicant.id">
		{{ applicant.firstName }} {{ applicant.lastName }}
	  </li>
	</ul>
  </div>
</template>
```

## 🔌 Available API Methods

### Applicants API
```javascript
import { applicantsAPI } from '@/services/api'

// Get all applicants
const response = await applicantsAPI.getAll()

// Get single applicant
const response = await applicantsAPI.getById(id)

// Create applicant
const response = await applicantsAPI.create({
  firstName: 'John',
  lastName: 'Doe',
  emailAddress: 'john@example.com',
  // ... other fields
})

// Update applicant
await applicantsAPI.update(id, {
  firstName: 'Updated Name',
  // ... other fields to update
})

// Delete applicant
await applicantsAPI.delete(id)
```

### Programs API
```javascript
import { programsAPI } from '@/services/api'

await programsAPI.getAll()
await programsAPI.getById(id)
await programsAPI.create({ name: '...', ... })
await programsAPI.update(id, { ... })
await programsAPI.delete(id)
```

### Schools API
```javascript
import { schoolsAPI } from '@/services/api'

await schoolsAPI.getAll()
await schoolsAPI.getById(id)
await schoolsAPI.create({ ... })
await schoolsAPI.update(id, { ... })
await schoolsAPI.delete(id)
```

### Signatories API
```javascript
import { signatoriesAPI } from '@/services/api'

await signatoriesAPI.getAll()
await signatoriesAPI.getById(id)
await signatoriesAPI.create({ ... })
await signatoriesAPI.update(id, { ... })
await signatoriesAPI.delete(id)
```

### Interns API
```javascript
import { internsAPI } from '@/services/api'

await internsAPI.getAll()
await internsAPI.getById(id)
await internsAPI.create({ ... })
await internsAPI.update(id, { ... })
await internsAPI.delete(id)
```

### Requests API
```javascript
import { requestsAPI } from '@/services/api'

await requestsAPI.getAll()
await requestsAPI.getById(id)
await requestsAPI.create({ ... })
await requestsAPI.update(id, { ... })
await requestsAPI.delete(id)
```

### Dashboard API
```javascript
import { dashboardAPI } from '@/services/api'

const stats = await dashboardAPI.getStats()
// Returns: {
//   totalApplicants: number,
//   totalPrograms: number,
//   totalSchools: number,
//   totalInterns: number,
//   pendingRequests: number,
//   approvedRequests: number
// }
```

## 📂 Refactoring Existing Components

To convert existing Vue components to use the API service:

1. **Import the API service:**
   ```javascript
   import { applicantsAPI } from '@/services/api'
   ```

2. **Replace data loading:**
   ```javascript
   // Before: load from localStorage or hardcoded
   onMounted(() => {
	 applicants.value = JSON.parse(localStorage.getItem('applicants') || '[]')
   })

   // After: load from API
   onMounted(async () => {
	 const response = await applicantsAPI.getAll()
	 applicants.value = response.data
   })
   ```

3. **Replace save operations:**
   ```javascript
   // Before: save to localStorage
   localStorage.setItem('applicants', JSON.stringify(applicants.value))

   // After: save to API
   await applicantsAPI.create(newApplicant)
   applicants.value.push(response.data)
   ```

4. **Add loading and error states:**
   ```javascript
   const loading = ref(false)
   const error = ref(null)

   async function fetchData() {
	 loading.value = true
	 error.value = null
	 try {
	   const response = await applicantsAPI.getAll()
	   applicants.value = response.data
	 } catch (err) {
	   error.value = err.message
	 } finally {
	   loading.value = false
	 }
   }
   ```

## 📋 Component Refactoring Checklist

- [ ] Replace hardcoded data with API calls
- [ ] Add loading state (`ref(false)`)
- [ ] Add error state (`ref(null)`)
- [ ] Use try/catch for API calls
- [ ] Display loading spinner in template
- [ ] Display error message if API fails
- [ ] Update form submission to call API
- [ ] Update delete operations to call API
- [ ] Verify CORS headers if needed
- [ ] Test with actual API running

## 🧪 Testing the Connection

### 1. Check if API is responding
```bash
curl http://localhost:5000/api/applicants
```

You should get a JSON array (possibly empty):
```json
[]
```

### 2. Create a test applicant via API
```bash
curl -X POST http://localhost:5000/api/applicants \
  -H "Content-Type: application/json" \
  -d '{
	"applicantNo": "A-2024-001",
	"firstName": "Test",
	"lastName": "User",
	"emailAddress": "test@example.com"
  }'
```

### 3. Check frontend console
- Open browser DevTools (F12)
- Check Network tab for API requests
- Check Console tab for errors

## ⚙️ Configuration

### Environment Variables
- **Development**: `.env.development` - Uses `http://localhost:5000/api`
- **Production**: `.env.production` - Uses `/api` (same server)

### CORS Settings
Edit `OJTMISApi/OJTMISApi/appsettings.json`:
```json
{
  "Cors": {
	"AllowedOrigins": [
	  "http://localhost:5173",
	  "http://localhost:3000",
	  "http://localhost:5000"
	]
  }
}
```

## 🔒 Authentication (Future)

The API service has interceptors ready for authentication:

```javascript
// Token is automatically added via interceptor in src/services/api.js
localStorage.setItem('authToken', 'your_jwt_token')

// Token will be sent as Authorization header:
// Authorization: Bearer your_jwt_token
```

## 📚 Files Modified/Created

### Backend Files (Created)
- `OJTMISApi/OJTMISApi/Models/Applicant.cs`
- `OJTMISApi/OJTMISApi/Models/OJTProgram.cs` (renamed from Program.cs)
- `OJTMISApi/OJTMISApi/Models/School.cs`
- `OJTMISApi/OJTMISApi/Models/Signatory.cs`
- `OJTMISApi/OJTMISApi/Models/Intern.cs`
- `OJTMISApi/OJTMISApi/Models/InternRequest.cs`
- `OJTMISApi/OJTMISApi/Controllers/ApplicantsController.cs`
- `OJTMISApi/OJTMISApi/Controllers/ProgramsController.cs`
- `OJTMISApi/OJTMISApi/Controllers/SchoolsController.cs`
- `OJTMISApi/OJTMISApi/Controllers/SignatoriesController.cs`
- `OJTMISApi/OJTMISApi/Controllers/InternsController.cs`
- `OJTMISApi/OJTMISApi/Controllers/RequestsController.cs`
- `OJTMISApi/OJTMISApi/Controllers/DashboardController.cs`

### Backend Files (Modified)
- `OJTMISApi/OJTMISApi/Program.cs` - Added CORS configuration
- `OJTMISApi/OJTMISApi/Data/ApplicationDbContext.cs` - Added DbSets
- `OJTMISApi/OJTMISApi/appsettings.json` - Added CORS settings

### Frontend Files (Created)
- `src/services/api.js` - API service with axios client
- `.env` - Default environment configuration
- `.env.development` - Development configuration
- `.env.production` - Production configuration
- `pages/applicants-api-example.vue` - Example refactored component
- `vite.config.js` - Updated with API proxy

### Documentation (Created)
- `API_SETUP_GUIDE.md` - Detailed setup guide
- `DATABASE_MIGRATIONS.md` - Database migration commands
- `CONNECTION_README.md` - This file

## 🐛 Common Issues

### Issue: CORS Error in Console
**Symptoms**: `Access to XMLHttpRequest blocked by CORS policy`

**Solutions**:
1. Ensure API is running on correct port
2. Check allowed origins in `appsettings.json`
3. Verify frontend URL matches allowed origins
4. Clear browser cache and retry

### Issue: 404 Not Found
**Symptoms**: `GET /api/applicants 404`

**Solutions**:
1. Verify API is running (`dotnet run`)
2. Check port number (should be 5000)
3. Verify controller routes are correct
4. Check if database is initialized

### Issue: Database Error
**Symptoms**: `InvalidOperationException: Connection string not found`

**Solutions**:
1. Run migrations: `dotnet ef database update`
2. Check SQL Server is running
3. Verify connection string in `appsettings.json`

## 📞 Next Steps

1. **Update all Vue components** to use the API service (see refactoring checklist)
2. **Set up authentication** if needed
3. **Add validation** to form submissions
4. **Implement pagination** for large datasets
5. **Add caching** for frequently accessed data
6. **Deploy** to production with proper environment configuration

## 📖 Documentation References

- [Axios Documentation](https://axios-http.com/)
- [Vue 3 Composition API](https://vuejs.org/guide/extras/composition-api-faq.html)
- [ASP.NET Core CORS](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

---

**Status**: ✅ API Connection Complete  
**Last Updated**: 2024  
**Version**: 1.0

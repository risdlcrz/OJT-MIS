# OJT MIS API-Frontend Connection - Quick Reference Card

## 🚀 QUICK START (2 minutes)

### Terminal 1: Start Backend
```bash
cd OJTMISApi/OJTMISApi
dotnet ef database update
dotnet run
```
✅ API running at: `http://localhost:5000`

### Terminal 2: Start Frontend
```bash
npm install
npm run dev
```
✅ Frontend running at: `http://localhost:5173`

---

## 📝 API Endpoints Quick Reference

| Resource | GET | POST | PUT | DELETE |
|----------|-----|------|-----|--------|
| Applicants | `/api/applicants` | Create | `/api/applicants/{id}` | Delete |
| Programs | `/api/programs` | Create | `/api/programs/{id}` | Delete |
| Schools | `/api/schools` | Create | `/api/schools/{id}` | Delete |
| Signatories | `/api/signatories` | Create | `/api/signatories/{id}` | Delete |
| Interns | `/api/interns` | Create | `/api/interns/{id}` | Delete |
| Requests | `/api/requests` | Create | `/api/requests/{id}` | Delete |
| Dashboard | `/api/dashboard/stats` | - | - | - |

---

## 💡 Common Code Patterns

### Fetch Data
```javascript
import { applicantsAPI } from '@/services/api'

const applicants = ref([])

onMounted(async () => {
  applicants.value = (await applicantsAPI.getAll()).data
})
```

### Create Data
```javascript
const response = await applicantsAPI.create({
  firstName: 'John',
  lastName: 'Doe',
  emailAddress: 'john@example.com'
})
applicants.value.push(response.data)
```

### Update Data
```javascript
await applicantsAPI.update(id, {
  firstName: 'Jane'
})
// Refresh list
applicants.value = (await applicantsAPI.getAll()).data
```

### Delete Data
```javascript
if (confirm('Delete?')) {
  await applicantsAPI.delete(id)
  applicants.value = applicants.value.filter(a => a.id !== id)
}
```

### Error Handling
```javascript
const error = ref(null)
const loading = ref(false)

try {
  loading.value = true
  applicants.value = (await applicantsAPI.getAll()).data
} catch (err) {
  error.value = err.message
} finally {
  loading.value = false
}
```

---

## 🔧 Configuration Files

### Frontend URLs
- **Dev**: `.env.development` → `http://localhost:5000/api`
- **Prod**: `.env.production` → `/api`
- **Debug**: `vite.config.js` proxy → `/api → http://localhost:5000`

### Backend CORS
- **File**: `OJTMISApi/OJTMISApi/appsettings.json`
- **Allowed Origins**: `http://localhost:5173`, `http://localhost:3000`, `http://localhost:5000`

### Database
- **File**: `OJTMISApi/OJTMISApi/appsettings.json`
- **Connection**: LocalDB (default)
- **Migrate**: `dotnet ef database update`

---

## 📚 Available Services

```javascript
import {
  applicantsAPI,      // /api/applicants
  programsAPI,        // /api/programs
  schoolsAPI,         // /api/schools
  signatoriesAPI,     // /api/signatories
  internsAPI,         // /api/interns
  requestsAPI,        // /api/requests
  dashboardAPI        // /api/dashboard
} from '@/services/api'

// All follow same pattern:
// .getAll() → Array<T>
// .getById(id) → T
// .create(data) → T
// .update(id, data) → T
// .delete(id) → void
```

---

## ✅ Testing Checklist

- [ ] Backend runs without errors
- [ ] Frontend runs on localhost:5173
- [ ] Can see API calls in DevTools Network tab
- [ ] GET /api/applicants returns empty array []
- [ ] Can create an applicant via curl
- [ ] Frontend can fetch applicants without CORS errors
- [ ] Components load data from API

---

## 🐛 Quick Fixes

### Port 5000 Already in Use
```bash
# Find and kill process
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

### Database Error
```bash
dotnet ef database drop
dotnet ef database update
```

### Frontend Can't Connect
```bash
# Check .env file
cat .env

# Restart dev server
npm run dev
```

### Clear Node Cache
```bash
rm -r node_modules
npm install
```

---

## 📌 Files to Know

| Path | Purpose |
|------|---------|
| `src/services/api.js` | Main API client |
| `pages/applicants-api-example.vue` | Example component |
| `OJTMISApi/OJTMISApi/Controllers/` | API endpoints |
| `OJTMISApi/OJTMISApi/Models/` | Database models |
| `.env*` | Environment config |
| `vite.config.js` | Frontend build config |
| `appsettings.json` | Backend config |

---

## 🎯 Next: Refactor Components

For each existing Vue component:

1. Import API service: `import { applicantsAPI } from '@/services/api'`
2. Replace hardcoded data with API calls
3. Add loading & error states
4. Wrap API calls in try/catch
5. Test with DevTools open

**Reference**: `pages/applicants-api-example.vue`

---

## 📞 Docs Location

- 📖 **Setup Guide**: `API_SETUP_GUIDE.md`
- 📚 **Database Guide**: `DATABASE_MIGRATIONS.md`
- 📋 **Full Implementation**: `CONNECTION_README.md`
- ✅ **Completion Summary**: `IMPLEMENTATION_COMPLETE.md`

---

## ⚡ Pro Tips

🔹 Use `response.data` when accessing API response data  
🔹 Always use `.catch()` or try/catch for error handling  
🔹 Store auth token: `localStorage.setItem('authToken', token)`  
🔹 Check DevTools Network tab for API issues  
🔹 Use browser console to debug API calls  
🔹 Keep `.env` files out of version control  

---

**Last Updated**: 2024 | **Version**: 1.0 | **Status**: ✅ Ready

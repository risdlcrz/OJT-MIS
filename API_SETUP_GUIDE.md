# OJT MIS - API to Frontend Connection Setup Guide

## Project Structure

### Backend (ASP.NET Core)
- **Location**: `OJTMISApi/OJTMISApi`
- **API Port**: `http://localhost:5000`
- **Controllers**: Located in `Controllers/` directory
  - `ApplicantsController` - Manage applicants
  - `ProgramsController` - Manage programs
  - `SchoolsController` - Manage schools
  - `SignatoriesController` - Manage signatories
  - `InternsController` - Manage interns
  - `RequestsController` - Manage intern requests
  - `DashboardController` - Dashboard statistics

### Frontend (Vue 3)
- **Location**: Root directory
- **Dev Port**: `http://localhost:5173` (Vite default)
- **Build Tool**: Vite
- **API Service**: `src/services/api.js`

## Quick Start

### 1. Backend Setup

```bash
cd OJTMISApi/OJTMISApi

# Restore packages
dotnet restore

# Create database and run migrations
dotnet ef database update

# Run the API
dotnet run
```

The API will start on `https://localhost:7000` and `http://localhost:5000`

### 2. Frontend Setup

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

## API Endpoints

### Applicants
- `GET /api/applicants` - Get all applicants
- `GET /api/applicants/{id}` - Get specific applicant
- `POST /api/applicants` - Create new applicant
- `PUT /api/applicants/{id}` - Update applicant
- `DELETE /api/applicants/{id}` - Delete applicant

### Programs
- `GET /api/programs` - Get all programs
- `GET /api/programs/{id}` - Get specific program
- `POST /api/programs` - Create new program
- `PUT /api/programs/{id}` - Update program
- `DELETE /api/programs/{id}` - Delete program

### Schools
- `GET /api/schools` - Get all schools
- `GET /api/schools/{id}` - Get specific school
- `POST /api/schools` - Create new school
- `PUT /api/schools/{id}` - Update school
- `DELETE /api/schools/{id}` - Delete school

### Signatories
- `GET /api/signatories` - Get all signatories
- `GET /api/signatories/{id}` - Get specific signatory
- `POST /api/signatories` - Create new signatory
- `PUT /api/signatories/{id}` - Update signatory
- `DELETE /api/signatories/{id}` - Delete signatory

### Interns
- `GET /api/interns` - Get all interns
- `GET /api/interns/{id}` - Get specific intern
- `POST /api/interns` - Create new intern
- `PUT /api/interns/{id}` - Update intern
- `DELETE /api/interns/{id}` - Delete intern

### Requests
- `GET /api/requests` - Get all requests
- `GET /api/requests/{id}` - Get specific request
- `POST /api/requests` - Create new request
- `PUT /api/requests/{id}` - Update request
- `DELETE /api/requests/{id}` - Delete request

### Dashboard
- `GET /api/dashboard/stats` - Get dashboard statistics

## Using the API Service in Vue Components

### Example: Fetching Applicants

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
	console.error('Error fetching applicants:', err)
  } finally {
	loading.value = false
  }
}

async function createApplicant(applicantData) {
  try {
	const response = await applicantsAPI.create(applicantData)
	applicants.value.push(response.data)
  } catch (err) {
	console.error('Error creating applicant:', err)
  }
}

async function updateApplicant(id, applicantData) {
  try {
	await applicantsAPI.update(id, applicantData)
	const index = applicants.value.findIndex(a => a.id === id)
	if (index !== -1) {
	  applicants.value[index] = { ...applicants.value[index], ...applicantData }
	}
  } catch (err) {
	console.error('Error updating applicant:', err)
  }
}

async function deleteApplicant(id) {
  try {
	await applicantsAPI.delete(id)
	applicants.value = applicants.value.filter(a => a.id !== id)
  } catch (err) {
	console.error('Error deleting applicant:', err)
  }
}
</script>
```

## CORS Configuration

CORS is enabled for the following origins in development:
- `http://localhost:5173` (Vite dev server)
- `http://localhost:3000` (Alternative dev server)
- `http://localhost:5000` (API debug server)

For production, update `appsettings.json` and configure appropriate origins.

## Database Models

### Applicant
```csharp
public class Applicant
{
	public int Id { get; set; }
	public string ApplicantNo { get; set; }
	public string LastName { get; set; }
	public string FirstName { get; set; }
	public string? MiddleName { get; set; }
	public string? EmailAddress { get; set; }
	public string? PhoneNumber { get; set; }
	public string? Program { get; set; }
	public string? EducationLevel { get; set; }
	public string? Status { get; set; }
	public DateTime? DateApplied { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
	public bool IsApproved { get; set; }
	public bool HasCOC { get; set; }
	public bool HasDTR { get; set; }
	public bool HasMedicalExam { get; set; }
	public bool HasParentalConsent { get; set; }
	public bool HasDAForm137 { get; set; }
	public bool HasNBI { get; set; }
}
```

### OJTProgram
```csharp
public class OJTProgram
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string? Description { get; set; }
	public string? Code { get; set; }
	public string? DurationMonths { get; set; }
	public string? Status { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
}
```

### School
```csharp
public class School
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string? Address { get; set; }
	public string? ContactPerson { get; set; }
	public string? ContactNumber { get; set; }
	public string? EmailAddress { get; set; }
	public string? Status { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
}
```

### Signatory
```csharp
public class Signatory
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string? Position { get; set; }
	public string? Department { get; set; }
	public string? Signature { get; set; }
	public string? Status { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
}
```

### Intern
```csharp
public class Intern
{
	public int Id { get; set; }
	public string InternNo { get; set; }
	public string LastName { get; set; }
	public string FirstName { get; set; }
	public string? MiddleName { get; set; }
	public string? EmailAddress { get; set; }
	public string? PhoneNumber { get; set; }
	public string? Program { get; set; }
	public string? School { get; set; }
	public string? Status { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
}
```

### InternRequest
```csharp
public class InternRequest
{
	public int Id { get; set; }
	public string RequestNo { get; set; }
	public string? SchoolName { get; set; }
	public string? ProgramName { get; set; }
	public int? NumberOfSlots { get; set; }
	public string? Status { get; set; }
	public DateTime? RequestDate { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
}
```

## Environment Variables

### Frontend (.env)
```
VITE_API_URL=http://localhost:5000/api
```

### Frontend (.env.production)
```
VITE_API_URL=/api
```

## Troubleshooting

### CORS Errors
- Ensure the API is running on the correct port
- Check that the frontend URL is in the allowed origins list in `appsettings.json`
- Browser console should show specific CORS error messages

### API Not Responding
- Verify the API is running with `dotnet run` in the API folder
- Check that port 5000/7000 is not in use by another process
- Review API logs for error messages

### Database Errors
- Run migrations: `dotnet ef database update`
- Check connection string in `appsettings.json`
- Ensure SQL Server is running

## Next Steps

1. ✅ Models and Controllers created
2. ✅ API endpoints configured
3. ✅ CORS enabled
4. 📝 Update Vue components to use API service (in progress)
5. 📝 Deploy to production
6. 📝 Configure authentication/authorization

# Excel Import/Export - Manual Test Plan

> **Version:** 1.0
> **Created:** 2026-01-17
> **Feature:** Phase G - Excel Import/Export

## Prerequisites

Before testing, ensure:
1. Backend is running (`dotnet run` in `src/Cadence.WebApi`)
2. Frontend is running (`npm run dev` in `src/frontend`)
3. Database has been migrated (`dotnet ef database update`)
4. At least one exercise exists with an active MSEL
5. Sample Excel files are available (see [Test Data](#test-data) section)

---

## Test Data

### Sample MSEL Excel File

Create a file named `test-msel.xlsx` with these columns in Row 1:

| A | B | C | D | E | F | G | H | I | J |
|---|---|---|---|---|---|---|---|---|---|
| Inject # | Title | Description | Scheduled Time | From | To | Delivery Method | Track | Phase | Expected Action |

Add sample data rows:

| Row | Inject # | Title | Description | Scheduled Time | From | To | Delivery Method | Track | Phase | Expected Action |
|-----|----------|-------|-------------|----------------|------|-----|-----------------|-------|-------|-----------------|
| 2 | 1 | Hurricane Warning | NWS issues hurricane warning | 09:00 | NWS | EOC | Phone | EOC | Initial Response | Acknowledge and activate |
| 3 | 2 | Shelter Activation | Red Cross requests shelter | 09:30 | Red Cross | ESF-6 | Email | Sheltering | Initial Response | Coordinate shelter locations |
| 4 | 3 | Road Closure | DOT reports flooding | 10:00 | DOT | EOC | Radio | Transportation | Response | Update road status board |
| 5 | INVALID | Bad Row | Missing time | | | | | | | |
| 6 | 4 | Power Outage | Utility reports outage | 10:30 | Power Co | ESF-12 | Phone | Utilities | Response | Track outage areas |

### Invalid Test Files

1. **empty.xlsx** - Empty workbook with no data
2. **password.xlsx** - Password-protected file
3. **wrong-format.txt** - Text file renamed to .xlsx
4. **large-file.xlsx** - File > 10MB (for size limit testing)
5. **no-headers.xlsx** - Data starting in row 1 with no headers

---

## Test Cases

### 1. File Upload Step

#### TC-1.1: Valid Excel File Upload
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Import page | Import wizard displays with file upload step |
| 2 | Drag and drop `test-msel.xlsx` onto upload zone | File is accepted, progress indicator shows |
| 3 | Wait for analysis | Worksheet list displays with row/column counts |
| 4 | Verify worksheet info | Shows worksheet name, row count (5), column count (10) |

#### TC-1.2: Valid CSV File Upload
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Browse" button | File picker opens |
| 2 | Select a valid CSV file | File is accepted |
| 3 | Wait for analysis | Single "worksheet" shown (CSV has one sheet) |

#### TC-1.3: Invalid File Type Rejection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Attempt to upload `wrong-format.txt` | Error message: "Unsupported file format" |
| 2 | Attempt to upload `.doc` file | Error message with supported formats list |

#### TC-1.4: File Size Limit
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Attempt to upload `large-file.xlsx` (>10MB) | Error message: "File size exceeds maximum" |

#### TC-1.5: Empty File Handling
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Upload `empty.xlsx` | Warning: "No data found in file" or empty worksheet list |

---

### 2. Worksheet Selection Step

#### TC-2.1: Single Worksheet Selection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | After uploading multi-sheet workbook | Worksheet list displays |
| 2 | Click on a worksheet | Worksheet is highlighted/selected |
| 3 | View preview | First 5 rows display in preview table |
| 4 | Click "Next" | Advances to Column Mapping step |

#### TC-2.2: MSEL Auto-Detection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Upload file with sheet named "MSEL" | MSEL sheet is pre-selected |
| 2 | Verify confidence indicator | Shows high confidence score |

#### TC-2.3: Preview Row Configuration
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Change "Header Row" to 2 | Preview updates to use row 2 as headers |
| 2 | Change "Data Start Row" to 3 | Preview shows data starting from row 3 |

---

### 3. Column Mapping Step

#### TC-3.1: Auto-Mapping Detection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Reach Column Mapping step | Auto-mapped columns show with confidence indicators |
| 2 | Verify "Title" maps to "Title" column | Green checkmark, high confidence |
| 3 | Verify "Description" maps correctly | Auto-detected based on header name |

#### TC-3.2: Manual Column Mapping
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Find unmapped required field | Shows "Not mapped" with warning icon |
| 2 | Click dropdown for field | Available columns list displays |
| 3 | Select appropriate column | Mapping updates, preview shows sample values |

#### TC-3.3: Required Field Validation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Leave "Title" unmapped | Warning indicator on field |
| 2 | Attempt to proceed | Error: "Required fields must be mapped" |
| 3 | Map the required field | Can proceed to next step |

#### TC-3.4: Sample Values Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Hover over mapped column | Tooltip shows 3 sample values |
| 2 | View column info | Data type and fill rate displayed |

---

### 4. Validation Step

#### TC-4.1: Successful Validation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | With all valid mappings, click "Validate" | Validation runs, progress shown |
| 2 | View results | Summary shows: Total rows, Valid rows, Errors, Warnings |
| 3 | Verify valid rows | Green indicators for valid rows |

#### TC-4.2: Validation Errors
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | With test file containing invalid row | Row 5 shows error status |
| 2 | Expand error row | Error details: "Invalid time format" or similar |
| 3 | View error count | Error count matches invalid rows |

#### TC-4.3: Validation Warnings
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Import file with optional fields missing | Warnings displayed (not errors) |
| 2 | Verify can proceed | "Import" button enabled despite warnings |

#### TC-4.4: Row-Level Details
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click on any row in validation results | Expands to show field-by-field details |
| 2 | View parsed values | Shows how each field was interpreted |

---

### 5. Import Execution Step

#### TC-5.1: Append Strategy
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "Append" import strategy | Description shows: "Add to existing injects" |
| 2 | Select target exercise | Exercise dropdown populated |
| 3 | Click "Import" | Progress indicator shows |
| 4 | Wait for completion | Success message with count: "4 injects created" |
| 5 | Navigate to exercise | New injects visible in MSEL |

#### TC-5.2: Replace Strategy
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "Replace" strategy | Warning: "This will delete existing injects" |
| 2 | Confirm and import | All existing injects replaced |
| 3 | Verify count | Only imported injects remain |

#### TC-5.3: Skip Error Rows Option
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | With validation errors present | "Skip error rows" checkbox visible |
| 2 | Check "Skip error rows" | Can proceed with import |
| 3 | Execute import | Success with "1 row skipped" message |

#### TC-5.4: Create Missing Phases
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Import file with new phase names | "Create missing phases" option visible |
| 2 | Enable option and import | New phases created automatically |
| 3 | Verify phases | New phases appear in exercise phases list |

#### TC-5.5: Import Cancellation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Start import process | Progress shown |
| 2 | Click "Cancel" | Import stops, session cleaned up |
| 3 | No partial data | Exercise unchanged |

---

### 6. Export to Excel

#### TC-6.1: Basic MSEL Export
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to exercise with injects | Exercise detail page |
| 2 | Click "Export" button | Export dialog opens |
| 3 | Keep default options (Excel format) | Options displayed |
| 4 | Click "Export" | File downloads |
| 5 | Open downloaded file | Excel file with MSEL worksheet |
| 6 | Verify data | All injects present with correct data |

#### TC-6.2: Export with Formatting
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enable "Include formatting" | Checkbox checked |
| 2 | Export file | Download completes |
| 3 | Open file | Headers have blue background, columns sized |
| 4 | Verify auto-filter | Filter dropdowns on header row |

#### TC-6.3: Export without Formatting
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Disable "Include formatting" | Checkbox unchecked |
| 2 | Export file | Download completes |
| 3 | Open file | Plain data, no colors or formatting |

#### TC-6.4: Include Phases Worksheet
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enable "Include Phases worksheet" | Checkbox checked |
| 2 | Export file | Download completes |
| 3 | Open file | Two worksheets: "MSEL" and "Phases" |
| 4 | Verify Phases sheet | All phases with sequence, name, times |

#### TC-6.5: Include Objectives Worksheet
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enable "Include Objectives worksheet" | Checkbox checked |
| 2 | Export file | Download completes |
| 3 | Open file | Three worksheets: "MSEL", "Phases", "Objectives" |
| 4 | Verify Objectives sheet | All objectives with number, name, description |

#### TC-6.6: Include Conduct Data
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enable "Include conduct data" | Checkbox checked |
| 2 | Export file | Download completes |
| 3 | Open file | Additional columns: Status, Fired At, Fired By |
| 4 | Verify fired inject | Shows status "Delivered" with timestamp |

#### TC-6.7: CSV Export
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "CSV" format | Radio button selected |
| 2 | Export file | .csv file downloads |
| 3 | Open in text editor | Comma-separated values, proper escaping |
| 4 | Open in Excel | Data imports correctly |

#### TC-6.8: Template Download
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Download Template" | Template file downloads |
| 2 | Open file | Headers only with one example row |
| 3 | Verify columns | All MSEL columns present |

---

### 7. Round-Trip Testing

#### TC-7.1: Export then Import
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Export MSEL from Exercise A | File downloaded |
| 2 | Import file to Exercise B (Append) | Import succeeds |
| 3 | Compare data | All fields match original |

#### TC-7.2: Modify and Re-Import
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Export MSEL | File downloaded |
| 2 | Edit file in Excel (change descriptions) | File saved |
| 3 | Import with Merge strategy | Changes applied |
| 4 | Verify modifications | Updated descriptions visible |

---

### 8. Error Handling

#### TC-8.1: Network Error During Upload
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Disable network | Network off |
| 2 | Attempt file upload | Error: "Network error" with retry option |

#### TC-8.2: Session Timeout
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Start import, wait 30+ minutes | Session may expire |
| 2 | Attempt to continue | Error: "Session expired" with restart option |

#### TC-8.3: Invalid Exercise Selection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Delete exercise while import in progress | Exercise removed |
| 2 | Attempt to execute import | Error: "Exercise not found" |

---

### 9. API Direct Testing (Postman/curl)

#### TC-9.1: Upload Endpoint
```bash
curl -X POST "http://localhost:5000/api/import/upload" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@test-msel.xlsx"
```
Expected: 200 OK with session ID and worksheet info

#### TC-9.2: Export Endpoint (GET)
```bash
curl -X GET "http://localhost:5000/api/export/exercises/{exerciseId}/msel?format=xlsx" \
  --output export.xlsx
```
Expected: Excel file downloaded

#### TC-9.3: Export Endpoint (POST)
```bash
curl -X POST "http://localhost:5000/api/export/msel" \
  -H "Content-Type: application/json" \
  -d '{"exerciseId":"guid-here","format":"xlsx","includePhases":true}' \
  --output export.xlsx
```
Expected: Excel file downloaded with custom headers

#### TC-9.4: Template Endpoint
```bash
curl -X GET "http://localhost:5000/api/export/template" \
  --output template.xlsx
```
Expected: Template file downloaded

---

## Test Summary Checklist

### Import Features
- [ ] TC-1.1: Valid Excel upload
- [ ] TC-1.2: Valid CSV upload
- [ ] TC-1.3: Invalid file rejection
- [ ] TC-1.4: File size limit
- [ ] TC-1.5: Empty file handling
- [ ] TC-2.1: Worksheet selection
- [ ] TC-2.2: MSEL auto-detection
- [ ] TC-2.3: Preview configuration
- [ ] TC-3.1: Auto-mapping
- [ ] TC-3.2: Manual mapping
- [ ] TC-3.3: Required field validation
- [ ] TC-3.4: Sample values display
- [ ] TC-4.1: Successful validation
- [ ] TC-4.2: Validation errors
- [ ] TC-4.3: Validation warnings
- [ ] TC-4.4: Row-level details
- [ ] TC-5.1: Append strategy
- [ ] TC-5.2: Replace strategy
- [ ] TC-5.3: Skip error rows
- [ ] TC-5.4: Create missing phases
- [ ] TC-5.5: Import cancellation

### Export Features
- [ ] TC-6.1: Basic export
- [ ] TC-6.2: With formatting
- [ ] TC-6.3: Without formatting
- [ ] TC-6.4: Phases worksheet
- [ ] TC-6.5: Objectives worksheet
- [ ] TC-6.6: Conduct data
- [ ] TC-6.7: CSV export
- [ ] TC-6.8: Template download

### Integration
- [ ] TC-7.1: Export then import
- [ ] TC-7.2: Modify and re-import

### Error Handling
- [ ] TC-8.1: Network error
- [ ] TC-8.2: Session timeout
- [ ] TC-8.3: Invalid exercise

### API Testing
- [ ] TC-9.1: Upload endpoint
- [ ] TC-9.2: Export GET endpoint
- [ ] TC-9.3: Export POST endpoint
- [ ] TC-9.4: Template endpoint

---

## Known Limitations

1. **Maximum file size**: 10 MB
2. **Session timeout**: Import sessions expire after 30 minutes of inactivity
3. **Supported formats**: .xlsx, .xls, .csv only
4. **Password protection**: Not supported
5. **Macros**: Excel macros are ignored during import
6. **Merge strategy**: Matches by inject number only

---

## Notes for Testers

1. **Browser compatibility**: Test in Chrome, Firefox, Edge, Safari
2. **Screen sizes**: Test responsive behavior on tablet/mobile
3. **Accessibility**: Verify keyboard navigation through wizard
4. **Performance**: Test with large files (1000+ rows within size limit)
5. **Concurrent users**: Test simultaneous imports to different exercises

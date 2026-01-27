# S05: Excel Export

## Story

**As an** Exercise Director,
**I want** to export exercise data to Excel format,
**So that** I can share with stakeholders, archive for compliance, and perform additional analysis.

## Context

After exercise completion (or during review), directors need to export data for after-action reviews, reports, and documentation. Excel is the standard interchange format, compatible with existing emergency management workflows and HSEEP templates.

## Acceptance Criteria

### Export Options
- [ ] **Given** I am viewing an exercise, **when** I access export options, **then** I see: "Export MSEL", "Export Observations", "Export Full Package"
- [ ] **Given** I have Director or Admin role, **when** viewing export options, **then** all options are enabled
- [ ] **Given** I have Controller or lower role, **when** viewing export options, **then** I cannot access exports (hidden or disabled)

### Export MSEL
- [ ] **Given** I click "Export MSEL", **when** export completes, **then** I download an Excel file named `{ExerciseName}_MSEL_{date}.xlsx`
- [ ] **Given** the MSEL export, **when** opened, **then** it contains all injects with columns: Phase, Number, Scheduled Time, Scenario Time, Title, Description, From, To, Method, Status, Actual Delivery Time, Notes
- [ ] **Given** the MSEL export, **when** viewed, **then** injects are sorted by Scheduled Time
- [ ] **Given** an inject was skipped, **when** exported, **then** Status shows "Skipped" with skip reason in Notes

### Export Observations
- [ ] **Given** I click "Export Observations", **when** export completes, **then** I download an Excel file named `{ExerciseName}_Observations_{date}.xlsx`
- [ ] **Given** the Observations export, **when** opened, **then** it contains columns: Timestamp, Observer, Related Inject, Observation Text, Rating (P/S/M/U), Core Capability, Strengths, Areas for Improvement
- [ ] **Given** an observation with no related inject, **when** exported, **then** Related Inject column shows "General"

### Export Full Package
- [ ] **Given** I click "Export Full Package", **when** export completes, **then** I download a ZIP file named `{ExerciseName}_Package_{date}.zip`
- [ ] **Given** the ZIP package, **when** extracted, **then** it contains: MSEL.xlsx, Observations.xlsx, Summary.json
- [ ] **Given** Summary.json, **when** viewed, **then** it contains exercise metadata: name, date, duration, inject counts, observation counts

### Download Experience
- [ ] **Given** I click an export button, **when** processing, **then** I see a loading indicator
- [ ] **Given** export completes, **when** file is ready, **then** browser initiates download automatically
- [ ] **Given** export fails, **when** error occurs, **then** I see an error message with ability to retry

### File Formatting
- [ ] **Given** exported Excel files, **when** opened, **then** headers are bold with filter enabled
- [ ] **Given** exported Excel files, **when** opened, **then** columns have appropriate widths
- [ ] **Given** exported Excel files, **when** opened, **then** first row is frozen for scrolling

## Out of Scope

- PDF report generation
- Formatted/styled report templates
- Scheduled/automated exports
- Email delivery of exports
- Export history/log

## Dependencies

- Exercise data with injects
- Observations data (if populated)
- Excel library (EPPlus, ClosedXML, or existing import library)
- File download mechanism

## Domain Terms

| Term | Definition |
|------|------------|
| MSEL | Master Scenario Events List - all injects for an exercise |
| P/S/M/U | HSEEP performance ratings: Performed, Satisfactory, Marginal, Unsatisfactory |
| Core Capability | FEMA Core Capability tied to observation |
| Full Package | ZIP containing all export files |

## UI/UX Notes

### Export Button Location
```
┌─────────────────────────────────────────────────────┐
│  Hurricane Response 2025                    [Export ▾] │
│                                                     │
│  Status: Completed  |  Duration: 4h 23m            │
└─────────────────────────────────────────────────────┘
                                             │
                                             ▼
                               ┌─────────────────────┐
                               │ 📊 Export MSEL      │
                               │ 👁️ Export Observa...│
                               │ 📦 Export Full Pkg  │
                               └─────────────────────┘
```

### Export Button States
```
[ Export MSEL ]  →  [ Exporting... ⟳ ]  →  ✓ Download starts
                           │
                           └→  ✗ Export failed [Retry]
```

### MSEL Excel Columns
| Column | Width | Description |
|--------|-------|-------------|
| Phase | 15 | Phase name |
| # | 8 | Inject number |
| Scheduled | 12 | HH:MM delivery time |
| Scenario Time | 15 | Story time (if different) |
| Title | 30 | Inject title |
| Description | 50 | Full inject description |
| From | 20 | Sending entity |
| To | 20 | Receiving entity |
| Method | 15 | Delivery method |
| Status | 12 | Fired/Skipped/Pending |
| Actual Time | 12 | When actually delivered |
| Notes | 40 | Controller notes, skip reason |

## Technical Notes

### API Endpoints
```
GET /api/exercises/{id}/export/msel
  → Returns: Excel file (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)

GET /api/exercises/{id}/export/observations
  → Returns: Excel file

GET /api/exercises/{id}/export/full
  → Returns: ZIP file (application/zip)
```

### Backend Service
```csharp
public interface IExportService
{
    Task<byte[]> ExportMselToExcelAsync(Guid exerciseId);
    Task<byte[]> ExportObservationsToExcelAsync(Guid exerciseId);
    Task<byte[]> ExportFullPackageAsync(Guid exerciseId);
}
```

### Frontend Components
```
ExportButton/
├── ExportButton.tsx          # Dropdown button
├── ExportMenuItem.tsx        # Individual export option
├── useExport.ts              # Hook with loading/error state
└── downloadFile.ts           # Utility to trigger download
```

### Download Utility
```typescript
export async function downloadFile(
  url: string, 
  filename: string
): Promise<void> {
  const response = await fetch(url, { 
    credentials: 'include' 
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  
  const link = document.createElement('a');
  link.href = downloadUrl;
  link.download = filename;
  link.click();
  
  window.URL.revokeObjectURL(downloadUrl);
}
```

### Excel Generation (ClosedXML example)
```csharp
using var workbook = new XLWorkbook();
var worksheet = workbook.Worksheets.Add("MSEL");

// Headers
worksheet.Cell(1, 1).Value = "Phase";
worksheet.Cell(1, 2).Value = "#";
// ... more headers

// Style headers
var headerRange = worksheet.Range(1, 1, 1, 12);
headerRange.Style.Font.Bold = true;
headerRange.SetAutoFilter();

// Freeze first row
worksheet.SheetView.FreezeRows(1);

// Add data
int row = 2;
foreach (var inject in injects)
{
    worksheet.Cell(row, 1).Value = inject.Phase?.Name;
    worksheet.Cell(row, 2).Value = inject.InjectNumber;
    // ... more columns
    row++;
}

// Auto-fit columns
worksheet.Columns().AdjustToContents();

using var stream = new MemoryStream();
workbook.SaveAs(stream);
return stream.ToArray();
```

---

*Story created: 2026-01-23*

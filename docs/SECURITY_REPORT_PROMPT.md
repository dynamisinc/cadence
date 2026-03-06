# Security Assessment Report — Claude Session Prompt

> **Usage:** Copy everything below the line into a new Claude Code session to generate a formal Word document (.docx) security assessment report from the latest scan artifacts. The report is designed to be shared with prospective customers, compliance evaluators, or security auditors.

---

## Prompt

You are generating a formal **Application Security Assessment Report** as a professional Word document for **Cadence** — an HSEEP-compliant MSEL management platform built on .NET 10 / React 19 / Azure. This report may be shared with prospective customers, compliance evaluators, or security auditors. It should look like it came from a security consulting firm — polished, official, and thorough.

### Step 1: Gather Scan Data

Download the latest artifacts from the most recent successful **Security Report** workflow run:

```bash
# Get the latest successful security report run ID
RUN_ID=$(gh api "repos/dynamisinc/cadence/actions/workflows/security-report.yml/runs?status=completed&conclusion=success&per_page=1" \
  --jq '.workflow_runs[0].id')
echo "Latest report run: $RUN_ID"

# Get the run date
RUN_DATE=$(gh api "repos/dynamisinc/cadence/actions/workflows/security-report.yml/runs?status=completed&conclusion=success&per_page=1" \
  --jq '.workflow_runs[0].created_at')
echo "Run date: $RUN_DATE"

# Create working directory
mkdir -p /tmp/security-report

# Download consolidated SARIF
gh api "repos/dynamisinc/cadence/actions/runs/$RUN_ID/artifacts" \
  --jq '.artifacts[] | select(.name == "security-report-sarif") | .archive_download_url' \
  | xargs gh api > /tmp/security-report/sarif.zip && \
  unzip -o /tmp/security-report/sarif.zip -d /tmp/security-report/

# Download consolidated HTML report
gh api "repos/dynamisinc/cadence/actions/runs/$RUN_ID/artifacts" \
  --jq '.artifacts[] | select(.name == "security-report-html") | .archive_download_url' \
  | xargs gh api > /tmp/security-report/html.zip && \
  unzip -o /tmp/security-report/html.zip -d /tmp/security-report/
```

Read the SARIF to understand the findings:

```bash
cat /tmp/security-report/consolidated-security-report.sarif
```

Also gather the latest GitHub Issue summary:

```bash
gh issue list --label "security-report" --state open --json number,title,body --jq '.[0]'
```

### Step 2: Read Context Files

Read these files to understand the application architecture and scan configuration:

```bash
# Application architecture and tech stack
cat CLAUDE.md

# ZAP rule exclusions with rationale
cat .zap/rules.tsv

# ZAP-to-SARIF converter (shows severity/confidence mapping)
cat .zap/json-to-sarif.py

# Infrastructure templates (what IaC findings reference)
ls infrastructure/
```

### Step 3: Understand the Scanning Pipeline

The SARIF file contains merged results from **three independent security tools** run as part of an automated CI/CD security pipeline:

| Tool | Type | What It Scans | Standards |
|------|------|---------------|-----------|
| **Semgrep** | SAST (Static Application Security Testing) | C#, TypeScript, React source code. Rule packs: auto, csharp, typescript, react, secrets, owasp-top-ten. Severity: ERROR + WARNING. | OWASP Top 10, CWE, SARIF 2.1.0 |
| **Checkov** | IaC (Infrastructure as Code) | Azure Bicep templates. Checks misconfigurations, missing encryption, overly permissive access. | CIS Benchmarks, SARIF 2.1.0 |
| **OWASP ZAP** | DAST (Dynamic Application Security Testing) | Live UAT environment — unauthenticated baseline + authenticated API scan (OpenAPI-driven). Tests injection, XSS, header misconfigurations, etc. | OWASP Testing Guide, SARIF 2.1.0 |

**ZAP severity mapping:**
- ZAP High Risk (3) → SARIF `error` (Critical)
- ZAP Medium Risk (2) → SARIF `warning`
- ZAP Low Risk (1) → SARIF `warning`
- ZAP Informational (0) → SARIF `note`

**ZAP confidence scale:** 99=Confirmed, 90=High, 60=Medium, 30=Low, 10=False Positive

**Automated scan schedule:**

| Schedule | Scan Type | Details |
|----------|-----------|---------|
| Every push to `main` | SAST (Semgrep) | Source code analysis |
| Every push (infra paths) | IaC (Checkov) | Bicep template scanning |
| Weekly (Monday 2 AM UTC) | DAST Quick | Authenticated baseline — passive scan |
| Monthly (1st, 3 AM UTC) | DAST Full | OpenAPI API scan — active attacks on every endpoint |
| Weekly (Monday 6 AM UTC) | Consolidated Report | Merges latest SAST + IaC + DAST |
| Monthly (1st, 7 AM UTC) | Consolidated Report | Captures full API scan results |

### Step 4: Generate the Word Document

Write a Python script to `/tmp/generate-security-report.py` that uses `python-docx` to create a professional Word document. Execute it with `/c/Python313/python.exe`.

**The script must:**

1. **Parse the consolidated SARIF** at `/tmp/security-report/consolidated-security-report.sarif`
2. **Extract and deduplicate findings** by `(tool, level, ruleId)` — count instances, collect unique locations
3. **Build the Word document** with the structure and formatting described below
4. **Save to** `docs/Cadence-Security-Assessment-Report.docx`

#### 4.1 Document Setup & Styling

```python
from docx import Document
from docx.shared import Inches, Pt, Cm, RGBColor, Emu
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.section import WD_ORIENT
from docx.oxml.ns import qn, nsdecls
from docx.oxml import parse_xml
import json, os
from datetime import datetime
from collections import Counter, defaultdict

# Brand colors
NAVY = RGBColor(0x1E, 0x3A, 0x5F)
DARK_GRAY = RGBColor(0x33, 0x33, 0x33)
MED_GRAY = RGBColor(0x66, 0x66, 0x66)
LIGHT_GRAY = RGBColor(0xF5, 0xF5, 0xF5)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
RED = RGBColor(0xD3, 0x2F, 0x2F)
ORANGE = RGBColor(0xF5, 0x7C, 0x00)
BLUE = RGBColor(0x19, 0x76, 0xD2)
GREEN = RGBColor(0x2E, 0x7D, 0x32)

SEVERITY_COLORS = {"error": RED, "warning": ORANGE, "note": BLUE, "none": MED_GRAY}
SEVERITY_LABELS = {"error": "Critical/Error", "warning": "Warning", "note": "Note", "none": "Info"}

doc = Document()

# Page margins
for section in doc.sections:
    section.top_margin = Cm(2.54)
    section.bottom_margin = Cm(2.54)
    section.left_margin = Cm(2.54)
    section.right_margin = Cm(2.54)

# Set default font
style = doc.styles['Normal']
font = style.font
font.name = 'Calibri'
font.size = Pt(11)
font.color.rgb = DARK_GRAY

# Style headings
for i in range(1, 4):
    heading_style = doc.styles[f'Heading {i}']
    heading_style.font.color.rgb = NAVY
    heading_style.font.name = 'Calibri'
    if i == 1:
        heading_style.font.size = Pt(20)
        heading_style.paragraph_format.space_before = Pt(24)
        heading_style.paragraph_format.space_after = Pt(12)
    elif i == 2:
        heading_style.font.size = Pt(16)
        heading_style.paragraph_format.space_before = Pt(18)
        heading_style.paragraph_format.space_after = Pt(8)
    else:
        heading_style.font.size = Pt(13)
        heading_style.paragraph_format.space_before = Pt(12)
        heading_style.paragraph_format.space_after = Pt(6)
```

#### 4.2 Helper Functions

```python
def add_styled_table(doc, headers, rows, col_widths=None):
    """Create a table with navy header row and alternating shading."""
    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.style = 'Table Grid'

    # Header row
    hdr = table.rows[0]
    for i, text in enumerate(headers):
        cell = hdr.cells[i]
        cell.text = ''
        p = cell.paragraphs[0]
        run = p.add_run(text)
        run.bold = True
        run.font.color.rgb = WHITE
        run.font.size = Pt(10)
        run.font.name = 'Calibri'
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        # Navy background
        shading = parse_xml(f'<w:shd {nsdecls("w")} w:fill="1E3A5F"/>')
        cell._tc.get_or_add_tcPr().append(shading)

    # Data rows
    for r_idx, row_data in enumerate(rows):
        row = table.rows[r_idx + 1]
        for c_idx, text in enumerate(row_data):
            cell = row.cells[c_idx]
            cell.text = ''
            p = cell.paragraphs[0]
            run = p.add_run(str(text))
            run.font.size = Pt(10)
            run.font.name = 'Calibri'
            run.font.color.rgb = DARK_GRAY
            # Alternating row shading
            if r_idx % 2 == 1:
                shading = parse_xml(f'<w:shd {nsdecls("w")} w:fill="F5F5F5"/>')
                cell._tc.get_or_add_tcPr().append(shading)

    # Column widths
    if col_widths:
        for row in table.rows:
            for i, w in enumerate(col_widths):
                row.cells[i].width = Inches(w)

    doc.add_paragraph()  # spacing after table
    return table


def add_severity_text(paragraph, level):
    """Add colored severity label to a paragraph."""
    color = SEVERITY_COLORS.get(level, MED_GRAY)
    label = SEVERITY_LABELS.get(level, level)
    run = paragraph.add_run(label)
    run.font.color.rgb = color
    run.bold = True
    run.font.size = Pt(10)
    return run


def add_key_value(doc, key, value, bold_value=False):
    """Add a 'Key: Value' line."""
    p = doc.add_paragraph()
    run_k = p.add_run(f"{key}: ")
    run_k.bold = True
    run_k.font.size = Pt(11)
    run_k.font.color.rgb = DARK_GRAY
    run_v = p.add_run(str(value))
    run_v.font.size = Pt(11)
    run_v.font.color.rgb = DARK_GRAY
    if bold_value:
        run_v.bold = True
    return p
```

#### 4.3 Cover Page

```python
# Add some vertical space
for _ in range(6):
    doc.add_paragraph()

# Logo (Cadence app icon)
logo_path = "src/frontend/public/icons/icon-192x192.png"
if os.path.exists(logo_path):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = p.add_run()
    run.add_picture(logo_path, width=Inches(1.5))

# Title
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = p.add_run("Application Security\nAssessment Report")
run.font.size = Pt(32)
run.font.color.rgb = NAVY
run.bold = True
run.font.name = 'Calibri'

doc.add_paragraph()

# Subtitle
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = p.add_run("Cadence — HSEEP MSEL Management Platform")
run.font.size = Pt(16)
run.font.color.rgb = MED_GRAY
run.font.name = 'Calibri'

doc.add_paragraph()
doc.add_paragraph()

# Metadata block
meta_items = [
    ("Report Date", datetime.utcnow().strftime("%B %d, %Y")),
    ("Document Version", "1.0"),
    ("Classification", "CONFIDENTIAL — For Authorized Recipients Only"),
    ("Prepared By", "Dynamis Inc. — Security Engineering"),
]
for key, val in meta_items:
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run_k = p.add_run(f"{key}:  ")
    run_k.bold = True
    run_k.font.size = Pt(11)
    run_k.font.color.rgb = DARK_GRAY
    run_v = p.add_run(val)
    run_v.font.size = Pt(11)
    run_v.font.color.rgb = DARK_GRAY

doc.add_page_break()
```

#### 4.4 Headers & Footers

```python
# Set up header/footer for the section after cover page
section = doc.sections[0]

# Header
header = section.header
header.is_linked_to_previous = False
h_para = header.paragraphs[0]
h_para.text = ""
run_left = h_para.add_run("Cadence — Security Assessment")
run_left.font.size = Pt(8)
run_left.font.color.rgb = MED_GRAY
run_left.font.name = 'Calibri'
h_para.add_run("\t\t")
run_right = h_para.add_run("CONFIDENTIAL")
run_right.font.size = Pt(8)
run_right.font.color.rgb = RED
run_right.bold = True
run_right.font.name = 'Calibri'

# Footer with page numbers
footer = section.footer
footer.is_linked_to_previous = False
f_para = footer.paragraphs[0]
f_para.text = ""
run_left = f_para.add_run("Dynamis Inc.")
run_left.font.size = Pt(8)
run_left.font.color.rgb = MED_GRAY
run_left.font.name = 'Calibri'
f_para.add_run("\t\t")
run_right = f_para.add_run("Page ")
run_right.font.size = Pt(8)
run_right.font.color.rgb = MED_GRAY
# Add PAGE field — each element must be inside its own <w:r> run
run_begin = parse_xml(f'<w:r {nsdecls("w")}><w:fldChar w:fldCharType="begin"/></w:r>')
run_instr = parse_xml(f'<w:r {nsdecls("w")}><w:instrText xml:space="preserve"> PAGE </w:instrText></w:r>')
run_end = parse_xml(f'<w:r {nsdecls("w")}><w:fldChar w:fldCharType="end"/></w:r>')
f_para._p.append(run_begin)
f_para._p.append(run_instr)
f_para._p.append(run_end)
```

#### 4.5 Table of Contents

```python
doc.add_heading("Table of Contents", level=1)
p = doc.add_paragraph("Update this field in Word: right-click → Update Field, or press Ctrl+A then F9")
p.italic = True
p.runs[0].font.color.rgb = MED_GRAY
p.runs[0].font.size = Pt(9)

# Insert TOC field code
p_toc = doc.add_paragraph()
run = p_toc.add_run()
fldChar1 = parse_xml(f'<w:fldChar {nsdecls("w")} w:fldCharType="begin"/>')
instrText = parse_xml(f'<w:instrText {nsdecls("w")} xml:space="preserve"> TOC \\o "1-3" \\h \\z \\u </w:instrText>')
fldChar2 = parse_xml(f'<w:fldChar {nsdecls("w")} w:fldCharType="separate"/>')
fldChar3 = parse_xml(f'<w:fldChar {nsdecls("w")} w:fldCharType="end"/>')
run._r.append(fldChar1)
run._r.append(instrText)
run._r.append(fldChar2)
run._r.append(fldChar3)

doc.add_page_break()
```

#### 4.6 Document Body Sections

**Generate each section using the SARIF data and context from the files you read. Here is the content structure for each section. Write real prose — not placeholders.**

**Section 1: Executive Summary**
- Open with **why this report exists**: Dynamis maintains a continuous automated security assessment program to proactively identify and remediate vulnerabilities. This report provides transparency into the security posture of the Cadence platform.
- Explain the **testing approach at a high level**: three independent tools (SAST, DAST, IaC scanning) running on automated CI/CD schedules — not a one-off audit, but ongoing vigilance. Emphasize defense in depth.
- Summarize the **high-level results** with confidence-building framing: "The assessment demonstrates a mature security posture with..." Note total findings vs. unique rules, and that most findings are informational/best-practice recommendations rather than exploitable vulnerabilities.
- If SAST found 0 source code vulnerabilities at ERROR/WARNING, state this prominently — it means the application code itself has no known static vulnerabilities.
- Close with a forward-looking statement about continuous monitoring and the remediation roadmap.
- **This section is the most important for a non-technical reader** — it should provide confidence that the platform is actively and professionally maintained from a security perspective.

**Section 2: Scope & Methodology**
- 2.1: Assessment scope table (Backend API, Frontend SPA, Infrastructure, Live Environment, Authentication, Real-Time)
- 2.2: Tool descriptions with versions and standards they map to
- 2.3: Scan configurations (Semgrep rule packs, Checkov framework, ZAP rules.tsv exclusions with rationale)
- 2.4: Limitations (no manual pen testing, no social engineering, no physical security; note Dependabot covers dependency scanning separately)

**Section 3: Security Architecture Overview**
- Authentication (JWT Bearer, Azure AD B2C integration)
- Authorization (three-tier role hierarchy: System → Organization → Exercise)
- Multi-tenancy data isolation (OrganizationId scoping, validation interceptor)
- Data protection (Azure SQL encryption at rest, HTTPS/TLS in transit)
- API security (CORS configuration, input validation)
- Deployment security (Azure App Service, managed platform)

**Section 4: Findings Summary**
- 4.1: Overview metrics table (total instances, unique findings, by severity)
- 4.2: Findings by tool table
- 4.3: OWASP Top 10 2021 mapping table — map each finding to its OWASP category using CWE tags where available. If a finding can't be mapped, note "N/A"

**Section 5: Detailed Findings**
- Group by severity (Critical first, then Warning, then Note)
- For EACH unique finding (deduplicated by rule), create a block with:
  - Rule ID, Severity (colored), Tool, CWE (if available), OWASP category (if mappable)
  - Instance count
  - Description (expand the SARIF message.text into a clear technical explanation)
  - Affected locations (file:line for SAST/IaC, endpoint URI for DAST — max 5, then "and N more")
  - Real-world risk assessment in the context of Cadence (is it exploitable? what's the blast radius? multi-tenancy implications?)
  - Specific remediation steps with references to OWASP guides or Azure docs
  - CWE and OWASP reference links

**Section 6: Risk Assessment Matrix**
- Classify each finding into: Critical (Immediate Action), High (Address This Sprint), Medium (Address This Quarter), Low (Monitor/Accept), Informational (Best Practice)
- Most DAST header-related findings are Low/Informational
- Note that risk assessment factors in the application context (e.g., API-only backend, JWT auth)

**Section 7: Remediation Status & Roadmap**
- 7.1: Already mitigated (e.g., Anti-CSRF not needed — JWT bearer auth, cookies managed by Azure platform)
- 7.2: Accepted risks (from .zap/rules.tsv IGNORE entries with business justification)
- 7.3: Remediation priorities table (Priority, Finding, Target, Owner)

**Section 8: Compliance Mapping**
- 8.1: OWASP ASVS mapping table (which ASVS categories are covered by which tool)
- 8.2: NIST SP 800-53 selected controls (SA-11, RA-5, SI-10, SC-8, AC-3, etc.)
- 8.3: Continuous monitoring description (automated schedule, consolidated reporting, GitHub Issue notifications)

**Section 9: References & Further Reading**
- Provide a professional reference section with links so a reader can learn more about each tool and standard
- Group into subsections:
  - **Security Testing Tools:** Semgrep (https://semgrep.dev/docs/), OWASP ZAP (https://www.zaproxy.org/docs/), Checkov (https://www.checkov.io/1.Welcome/What%20is%20Checkov.html)
  - **Standards & Frameworks:** OWASP Top 10 2021 (https://owasp.org/Top10/), OWASP ASVS (https://owasp.org/www-project-application-security-verification-standard/), NIST SP 800-53 (https://csf.tools/reference/nist-sp-800-53/), SARIF 2.1.0 (https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html), CWE (https://cwe.mitre.org/)
  - **Azure Security:** Azure App Service Security (https://learn.microsoft.com/en-us/azure/app-service/overview-security), Azure SQL Security (https://learn.microsoft.com/en-us/azure/azure-sql/database/security-overview)
- Use `add_styled_table` with columns: Resource, Description, URL
- Each link should include a 1-line description of what the reader will find there

**Section 10: Appendices**
- A: Tool versions and configuration
- B: Rule exclusions (full .zap/rules.tsv with rationale)
- C: Scan execution details (timestamps, durations, target URLs)
- D: Glossary (SAST, DAST, IaC, SARIF, OWASP, CWE, HSEEP, MSEL)

### Step 5: Execute & Deliver

```bash
/c/Python313/python.exe /tmp/generate-security-report.py
```

Verify the file was created:
```bash
ls -la docs/Cadence-Security-Assessment-Report.docx
```

Tell the user the file path and size. The document should be 15-30 pages when opened in Word.

### Writing Guidelines

- **Tone:** Professional, reassuring, factual. This is meant to build confidence in the platform's security posture. Frame the automated pipeline as a strength. Don't be alarmist about informational findings.
- **Every finding** must reference CWE ID and OWASP category where available
- **For DAST findings:** note the HTTP method and endpoint path
- **For SAST findings:** note the file path and line number
- **For IaC findings:** note the Bicep resource and specific misconfiguration
- **Distinguish** real exploitable risk from informational/best-practice recommendations
- **Note findings already mitigated** by the application architecture (e.g., CSRF irrelevant for JWT API, cookies managed by Azure)
- If a tool found **0 findings**, state it positively: "No vulnerabilities detected at the configured severity threshold"
- If a scan source was **unavailable**, note it was not included in this assessment period
- The report must be **self-contained** — a reader should not need repo access to understand it
- Note this is a **point-in-time assessment** backed by continuous automated monitoring
- Do NOT include raw SARIF JSON — only interpreted findings
- All tables should use the `add_styled_table` helper for consistent navy headers
- Keep the Python script clean and well-organized — one function per section

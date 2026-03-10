# Security Assessment Report — Generation Guide

> **Usage:** Copy the prompt below into a new Claude Code session to generate a formal Word document (.docx) security assessment report from the latest scan artifacts.
>
> **Prerequisites:** `python-docx` must be installed (`pip install python-docx`).

---

## Prompt

You are generating a formal **Application Security Assessment Report** as a professional Word document for **Cadence** — an HSEEP-compliant MSEL management platform built on .NET 10 / React 19 / Azure. This report may be shared with prospective customers, compliance evaluators, or security auditors. It should be polished, professional, and **honest about both strengths and limitations**. This is a vendor self-assessment — it must not read like an independent third-party audit.

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

# The report generation script (understand current mappings)
cat scripts/generate-security-report.py
```

### Step 3: Understand the Scanning Pipeline

The SARIF file contains merged results from **three independent security tools** run as part of an automated CI/CD security pipeline:

| Tool | Type | What It Scans | Standards | Limitations |
|------|------|---------------|-----------|-------------|
| **Semgrep OSS** | SAST | C#, TypeScript, React source code. Rule packs: auto, csharp, typescript, react, secrets, owasp-top-ten. Severity: ERROR + WARNING. | OWASP Top 10 2025, CWE, SARIF 2.1.0 | Limited C# depth compared to commercial SAST tools. No taint tracking or cross-file data flow for C#. |
| **Checkov** | IaC | Azure Bicep templates. Checks misconfigurations, missing encryption, overly permissive access. | CIS Benchmarks, SARIF 2.1.0 | Reports ALL findings as SARIF `error` regardless of actual risk. Scans templates only — runtime drift not detected. |
| **OWASP ZAP** | DAST | Live UAT environment — unauthenticated baseline + authenticated API scan (OpenAPI-driven). Tests injection, XSS, header misconfigurations, etc. | OWASP Testing Guide, SARIF 2.1.0 | Single user context (no privilege escalation testing). No SignalR/WebSocket coverage. No complex multi-step workflow testing. |

**Semgrep OWASP version:** The `p/owasp-top-ten` rule pack is **not pinned** to a specific year. Semgrep updated all rules to use OWASP Top 10 2025 mappings. ZAP and Checkov use their own native rule IDs (ZAP alert IDs, CKV checks) mapped to CWE — the OWASP crosswalk is applied in the report generation script via CWE→OWASP mapping tables.

**CRITICAL — Dual severity model:**
- **Tool severity** = raw SARIF level (error/warning/note). Checkov reports all findings as `error`. This is **secondary metadata only**.
- **Assessed risk** = contextual classification (Critical/High/Medium/Low/Informational/Mitigated) considering exploitability, existing mitigations, and application architecture. This is the **primary classification** used throughout the report.

**ZAP severity mapping (tool → SARIF):**
- ZAP High Risk (3) → SARIF `error`
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

### Step 4: Review and Update the Generation Script

The report generation script lives at `scripts/generate-security-report.py`. Before running it:

1. **Read the script** to understand the current OWASP mappings, Checkov descriptions, risk classifications, and remediation text.
2. **Update `SCAN_DATE`** if the SARIF path or scan date needs to change.
3. **Review mappings** — if new Checkov rules or ZAP alerts appear in the SARIF that aren't in the script's dictionaries, add them with appropriate OWASP/CWE mappings, descriptions, risk classifications, and remediation text.
4. **Check for stale references** — ensure tool versions, Azure product names (e.g., Microsoft Entra ID, not Azure AD B2C), and OWASP version references (2025, not 2021) are current.
5. **Verify assessed risk classifications** — new findings need a `RISK_CLASSIFICATION` entry. Do not default everything to Low/Informational without justification.

Key data structures in the script that may need updating:

| Dictionary | Purpose | When to Update |
|-----------|---------|----------------|
| `CWE_TO_OWASP` | Maps CWE IDs → OWASP Top 10 2025 categories | New CWEs appear in ZAP findings |
| `CHECKOV_OWASP` | Maps Checkov rule IDs → OWASP/CWE | New Checkov rules appear |
| `CHECKOV_DESCRIPTIONS` | Human-readable explanations per Checkov rule | New Checkov rules appear |
| `RISK_CLASSIFICATION` | **Assessed risk** per rule ID (Critical/High/Medium/Low/Informational/Mitigated) | New rules, or risk reassessment |
| `RISK_ORDER` | Sort order for assessed risk levels | Only if new risk levels are added |
| `RISK_COLORS` | Display colors for assessed risk levels | Only if new risk levels are added |
| `TOOL_SEVERITY_LABELS` | Labels for raw SARIF levels (Error/Warning/Note/Info) | Only if SARIF levels change |
| `_get_risk_assessment()` | Contextual risk text per rule | New rules need assessment prose |
| `_get_remediation()` | Specific fix steps per rule | New rules need remediation guidance |

### Step 5: Run the Script

```bash
# Copy SARIF to a path accessible by the Python interpreter
cp /tmp/security-report/consolidated-security-report.sarif /c/temp/consolidated-security-report.sarif

# Run from the repo root
cd /c/Code/dynamis/cadence
SARIF_PATH="/c/temp/consolidated-security-report.sarif" /c/Python313/python.exe scripts/generate-security-report.py
```

Verify the file was created:

```bash
ls -la docs/Cadence-Security-Assessment-Report.docx
```

Tell the user the file path and size. The document should be 15–30 pages when opened in Word.

### Document Structure

The script generates a 10-section report:

| Section | Content |
|---------|---------|
| Cover Page | Logo, title, metadata (including "Assessment Type: Internal Automated Security Assessment"), confidentiality notice |
| 1. Executive Summary | Vendor self-assessment disclaimer, high-level results with tempered claims, threat model reference, forward-looking statement |
| 2. Scope & Methodology | Tools, configurations, scan schedules, **limitations & coverage gaps**, **architecture review scope**, **threat model** (adversary profiles, impact analysis), **data classification** |
| 3. Security Architecture | Auth, RBAC, multi-tenancy, token & session management (purely descriptive), data protection (purely descriptive), API security, deployment |
| 4. Findings Summary | **Assessed risk table** (primary), **tool severity table** (secondary, with Checkov caveat), OWASP Top 10 2025 mapping |
| 5. Detailed Findings | Grouped by **assessed risk** (not tool severity). Each finding shows both assessed risk and tool severity. |
| 6. Risk Assessment Matrix | Critical → Informational classification with caveat about automated tool limitations |
| 7. Remediation Roadmap | Mitigated, accepted risks, remediation priorities, **recommended additional assessments** (pen testing, SignalR review, privilege escalation testing, token audit) |
| 8. Compliance Mapping | OWASP ASVS (with **Gaps column**), NIST SP 800-53 (with **Gaps column**), continuous monitoring |
| 9. References | Tools, standards, Azure security docs |
| 10. Appendices | Tool versions, ZAP rules, scan details, glossary, **document change log** |

### Content Guidelines for Each Section

**Section 1: Executive Summary**
- Open with a **vendor self-assessment disclaimer** (orange-highlighted): this is an internal assessment, not an independent audit. Recommend supplementing with third-party pen testing.
- Summarize the **three-tool defense-in-depth approach** (SAST, DAST, IaC).
- Present key results **honestly**:
  - SAST: "No findings detected within configured rule packs" (NOT "zero vulnerabilities"). Note Semgrep OSS C# limitations.
  - DAST: Note coverage limited to HTTP REST endpoints. SignalR, file uploads, and multi-step workflows not covered.
  - IaC: Frame as configuration hardening recommendations from CIS benchmarks.
- Summary table must show **assessed risk counts** (Medium/Low/Informational), not raw tool severity counts.
- Reference the **threat model in Section 2.6** when describing risk ratings ("risk ratings are calibrated against the threat model in Section 2.6").
- Close with honest forward-looking statement acknowledging automated tools should be supplemented with manual testing.
- **This section is the most important for a non-technical reader.**

**Section 2: Scope & Methodology**
- 2.1: Assessment scope table (Backend API, Frontend SPA, Infrastructure, Live Environment, Authentication, Real-Time)
- 2.2: Tool descriptions with versions, standards, **and limitations**
- 2.3: Scan configurations (Semgrep rule packs, Checkov framework, ZAP rules.tsv exclusions with rationale)
- 2.4: **Limitations & Coverage Gaps** (expanded): no manual pen testing, no privilege escalation testing, SAST C# depth limited, DAST single-user context, no SignalR testing, no runtime drift detection, Dependabot covers SCA separately
- 2.5: **Architecture Review Scope** — explicit statement that the architecture review was internal (not independent), describes methodology (code inspection, template analysis), states no formal threat modeling workshop was conducted, recommends independent validation
- 2.6: **Threat Model** — adversary profiles table (5 adversaries: external actor, cross-tenant insider, malicious participant, CI/CD compromise, opportunistic attacker) with motivation, capability, and likelihood. Impact analysis covering confidentiality (HSEEP exercise data exposure), integrity (MSEL modification), availability (exercise disruption), and multi-tenant breach. Statement that risk ratings are calibrated to this model.
- 2.7: **Data Classification** — 6-row table of data categories (Exercise Scenarios, Organizational Structure, User Credentials, Exercise Observations, System Configuration, Audit & Telemetry) with classification levels and protection requirements. Note about FOUO exercise content.

**Section 3: Security Architecture Overview**

**IMPORTANT: Section 3 is purely descriptive.** It describes what exists — it does NOT assert gaps, make recommendations, or use orange-highlighted callouts. Gap identification belongs in the threat model (Section 2.6), ASVS/NIST gaps (Section 8), and remediation roadmap (Section 7). This separation ensures Section 3 serves as a factual architecture reference that doesn't do double duty as a gap analysis.

- 3.1: Authentication (JWT Bearer, Microsoft Entra ID integration)
- 3.2: Authorization (three-tier role hierarchy: System → Organization → Exercise)
- 3.3: Multi-tenancy data isolation (OrganizationId scoping, validation interceptor)
- 3.4: **Token & Session Management** — describe JWT lifecycle factually: token issuance, contents, validation, client-side storage, expiry-based revocation, MFA status. Use factual bullet points (e.g., "Token revocation: Token expiry is the primary revocation mechanism. Refresh token rotation and server-side session invalidation are not currently implemented.") — do NOT use orange callout boxes.
- 3.5: **Data Protection** — encryption at rest/in transit, secrets management factual description. End with a cross-reference: "See Section 7.3 for remediation plan." — do NOT use orange callout boxes.
- 3.6: API security (CORS, input validation, parameterized queries)
- 3.7: Deployment security (Azure App Service, managed platform)

**Section 4: Findings Summary**
- 4.1: **Two metrics tables**:
  - **Primary**: "Findings by Assessed Risk" (Critical/High/Medium/Low/Informational/Mitigated counts) — this is the main classification
  - **Secondary**: "Findings by Tool-Reported Severity (Raw)" with explicit note that Checkov reports all IaC findings as `error`
- 4.2: Findings by tool table
- 4.3: OWASP Top 10 2025 mapping table — map each finding to its OWASP category using CWE tags

**Section 5: Detailed Findings**
- **Group by assessed risk** (Medium first, then Low, then Informational, then Mitigated) — NOT by tool severity
- For EACH unique finding: Rule ID, **Assessed Risk**, **Tool Severity**, Tool, CWE, OWASP category, instance count, description, affected locations, risk assessment, remediation steps
- **Mitigated findings must have specific "Remediated. [action taken]" text** — not generic boilerplate. A reviewer must be able to verify what was done.

**Section 6: Risk Assessment Matrix**
- Classify each finding: Critical → Informational using the `RISK_CLASSIFICATION` dictionary
- Include caveat: absence of Critical/High from automated tools does not guarantee absence of all high-risk vulnerabilities
- Recommend manual pen testing to validate

**Section 7: Remediation Status & Roadmap**
- 7.1: Already mitigated (CSRF irrelevant for JWT API, HSTS configured, CSP configured, SQL auditing enabled, Defender tiers enabled, etc.)
- 7.2: Accepted risks — only genuinely low-impact items. Do NOT put deferred remediation here.
- 7.3: Remediation priorities — open with a paragraph stating this is a point-in-time snapshot and that **live tracking is maintained in GitHub Issues** (label: `security-report`). The authoritative status should be verified against the issue tracker. Then the priorities table (Priority, Finding, Target, Owner) — include Key Vault migration, token lifecycle (refresh token rotation, revocation), and infrastructure hardening items.
- 7.4: **Recommended Additional Assessments** — table recommending annual pen testing, SignalR security review, privilege escalation testing, token security audit

**Section 8: Compliance Mapping**
- 8.1: OWASP ASVS mapping — **4-column table** with "Gaps" column showing what's NOT covered per ASVS category. Keep gaps current — don't list items as gaps when they've been mitigated.
- 8.2: NIST SP 800-53 selected controls — **4-column table** with "Gaps" column (same pattern as ASVS). Include IA-2 (MFA gap) and IA-5 (token revocation gap).
- 8.3: Continuous monitoring schedule

**Section 9: References & Further Reading**
- Security Testing Tools: Semgrep, OWASP ZAP, Checkov
- Standards & Frameworks: OWASP Top 10 2025 (https://owasp.org/Top10/2025/), OWASP ASVS, NIST SP 800-53, SARIF 2.1.0, CWE
- Azure Security: App Service Security, Azure SQL Security

**Section 10: Appendices**
- A: Tool versions and configuration
- B: Rule exclusions (full .zap/rules.tsv)
- C: Scan execution details
- D: Glossary — must cover ALL acronyms used in the report (SAST, DAST, IaC, SARIF, OWASP, CWE, HSEEP, MSEL, JWT, HSTS, TDE, CIS, RBAC, CORS, ASVS, FOUO, NIST, MFA, SCA, CSP, TLS, CSRF, CDN)
- E: **Document Change Log** — version history table (Version, Date, Changes) showing what changed across versions. Include description of structural changes (new sections, methodology changes, reclassifications). This enables readers comparing versions to understand what changed and why.

### Writing Guidelines

- **Tone:** Professional, factual, and **honest about limitations**. Build confidence through transparency, not by overstating coverage. Frame the automated pipeline as a strength, but acknowledge what it does NOT cover.
- **Vendor self-assessment framing:** This is NOT an independent audit. State this clearly. Recommend third-party validation for organizations with stringent compliance requirements.
- **Dual severity model:** Always distinguish between tool-reported severity (raw SARIF level) and assessed risk (contextual classification). Assessed risk is the primary classification. Explain that Checkov reports everything as `error` — do not label these as "Critical".
- **Tempered claims:** Never say "zero vulnerabilities" — say "no findings detected within configured rule packs" and note tooling limitations. Do not claim comprehensive coverage where it doesn't exist.
- **Every finding** must reference CWE ID and OWASP category where available.
- **For DAST findings:** note the HTTP method and endpoint path.
- **For SAST findings:** note the file path and line number.
- **For IaC findings:** note the Bicep resource and specific misconfiguration.
- **Distinguish** real exploitable risk from informational/best-practice recommendations.
- **Note findings already mitigated** by the architecture (e.g., CSRF irrelevant for JWT API).
- **Mitigated findings** must have specific "Remediated. [specific action taken]" text in both risk assessment and remediation fields. Generic boilerplate undermines credibility.
- **Section 3 is purely descriptive** — do NOT use orange-formatted callout boxes in Section 3. Known gaps and recommendations belong in Section 2.6 (threat model), Section 7 (remediation), and Section 8 (compliance gaps).
- If a tool found **0 findings**, state it as "no findings detected within scope" — not as a guarantee of zero vulnerabilities.
- If a scan source was **unavailable**, note it was not included in this assessment period.
- The report must be **self-contained** — no repo access needed.
- Note this is a **point-in-time assessment** backed by continuous automated monitoring.
- Do NOT include raw SARIF JSON — only interpreted findings.
- **Terminology:** Use "Microsoft Entra ID" (not "Azure AD" or "Azure AD B2C"). Use "OWASP Top 10 2025" (not 2021).

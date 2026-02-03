# Cross-Domain Analysis: Inject Status Configurability

**Research Date:** February 2, 2026  
**Conclusion:** Configurable dropdowns are a strategic necessity for Cadence to serve markets beyond U.S. civilian emergency management.

## Executive Summary

While FEMA's HSEEP serves as the de facto standard for U.S. civilian emergency management, at least **five distinct framework ecosystems** use incompatible terminology. Even HSEEP-aligned organizations often require sector-specific customizations. This research supports implementing configurable status workflows with pre-built framework templates.

## HSEEP: The U.S. Civilian Baseline

FEMA's PrepToolkit defines the most widely adopted inject status workflow in the United States:

| Status | Definition |
|--------|------------|
| **Draft** | Initial creation state during design/development |
| **Submitted** | Sent for review, awaiting approval |
| **Approved** | Reviewed and accepted, ready for synchronization |
| **Synchronized** | Approved events ready for exercise use |
| **Deferred** | Synchronized event canceled during play |
| **Released** | Event just happened in real-time |
| **Complete** | Event has transpired |
| **Obsolete** | Pending permanent deletion |

HSEEP also standardizes:
- **Event categories:** Inject, Contingency Inject, Expected Action, Other
- **Exercise types:** Seven categories from Seminars to Full-Scale Exercises
- **Delivery methods:** Email, phone, radio, fax, in-person, SIMCELL

**Market coverage:** CMS healthcare requirements, CISA tabletop packages, and DOE/ASPR programs explicitly mandate HSEEP alignment.

## Five Framework Ecosystems with Different Terminology

### 1. Department of Defense (Separate System)

The DoD's Joint Training System (JTS) uses its own inject workflow through the Joint Training Tool (JTT):

- **STARTEX/ENDEX** (universal NATO/DoD exercise phase markers)
- **Joint Event Life Cycle (JELC):** Design → Planning → Preparation → Execution → Evaluation
- **Inject types:** Key, Enabling, Supporting (vs. HSEEP's Inject/Contingency/Expected Action)
- **JMSEL** (Joint Master Scenario Event List) with classified network requirements

Organizations supporting both DoD and civilian clients need dual terminology capability.

### 2. International Frameworks

**United Kingdom:**
- "Main Events List" (not MSEL)
- "Post-Exercise Report" (not AAR)
- Stress test cell structure: Blue Cell (primary players), Red Cell (threats), Green Cell (dependencies), White Cell (environment)
- **Hot Debrief/Cold Debrief** rather than Hot Wash
- Exercise Director/Controller/Facilitator role distinctions

**Australia/New Zealand:**
- "Special Ideas" instead of injects
- "Master Schedule of Events" instead of MSEL
- Exercise classification: DISCEX (discussion), Functional, Field (vs. HSEEP's seven types)
- **No Duff** safety protocol (indicating real emergencies)

**NATO:**
- **STARTEX/ENDEX** (mandatory)
- **DISTAFF** (Directing Staff who inject events)
- **EXCON** (Exercise Control)
- Exercise types: **LIVEX** (live), **CPX** (Command Post Exercise), **Exercise Study**

**EU Civil Protection Mechanism:**
- **EU MODEX** (Module Exercises) for certification
- **ERCC** coordination
- European member state protocols

### 3. Private Sector BC/DR Frameworks

**BCI Good Practice Guidelines** and **DRII Professional Practices** align with ISO 22301/22300:
- "Validation" instead of "evaluation"
- Organized around **Business Continuity Management System (BCMS)** principles
- DRII categorizes by facet: Technical, Procedural, Logistical, Timeline

### 4. Cybersecurity Frameworks

**NIST SP 800-84:**
- **Inject Tracking Forms** with technical artifact fields
- **Control Cell** operations (vs. SIMCELL)
- Technical vs. management-level inject distinctions

**MITRE ATT&CK Mapping:**
- **14 tactics** (Reconnaissance through Impact)
- **203+ techniques**
- Specific **adversary procedures (TTPs)**
- **Rules of Engagement (ROE)** fields
- **Kill Chain phase** tracking
- **Indicators of Compromise (IOCs)**
- **Breakout time** metrics

**Role types:** Red Team, Blue Team, White Team, Purple Team (vs. HSEEP's Controller/Evaluator/Simulator)

### 5. Healthcare Requirements

Dual compliance: **CMS Emergency Preparedness Rule** (HSEEP-aligned) + **Joint Commission** (NFPA 1600):

- **Hazard Vulnerability Analysis (HVA)** categories
- **Hospital Incident Command System (HICS)** roles
- **96-hour sustainability** markers
- **Surge response levels:** Conventional, Contingency, Crisis
- **Medical Response and Surge Exercise (MRSE)** phase tracking

### 6. Financial Sector (FFIEC/FINRA)

References FEMA guidance but adds critical fields:

- **Recovery Time Objectives (RTOs)**
- **Recovery Point Objectives (RPOs)**
- **Maximum Tolerable Downtime (MTDs)**
- Mission-critical system classifications

These metrics are regulatory requirements for financial institutions.

## Dropdown Fields with Significant Variation

| Field | Variations Found |
|-------|-----------------|
| **Inject Status** | HSEEP 8-state workflow vs. simpler 3-4 state models vs. DoD/NATO phases |
| **Event/Inject Category** | Inject/Contingency/Expected Action (HSEEP) vs. Key/Enabling/Supporting (DoD) vs. Technical/Management (cyber) |
| **Exercise Type** | 7 HSEEP types vs. 3 Australian categories vs. NATO LIVEX/CPX/Study |
| **Delivery Method** | HSEEP methods vs. cyber-specific (dashboard alert, SIEM, chat) vs. military (SIPRNET/NIPRNET) |
| **Severity/Priority** | Varies from Critical/High/Medium/Low to sector-specific scales |
| **Phase/Timing** | STARTEX/ENDEX (international) vs. HSEEP Design/Conduct phases |
| **Role Types** | Controller/Evaluator/Simulator (HSEEP) vs. EXCON/DISTAFF (NATO) vs. Red/Blue/White/Purple Team (cyber) |
| **Objective Categories** | Mission areas, capabilities, or ATT&CK tactics depending on domain |

## Implementation Recommendation

### Pre-Built Framework Templates

Offer rapid deployment templates for:
- FEMA/HSEEP Standard (default for U.S. civilian)
- DoD/Joint Training System
- NATO/Allied
- UK Cabinet Office
- Australian AIIMS
- Cybersecurity (NIST/MITRE-aligned)
- Healthcare (CMS/Joint Commission dual-compliance)
- Financial (FFIEC/FINRA)
- ISO 22301/BCI/DRII (private sector BC)

### Custom Configuration Capabilities

Allow organizations to:
- Add/remove/rename status options within workflows
- Create custom inject categories and event types
- Define sector-specific fields (RTOs, ATT&CK mappings, HVA categories)
- Configure role types matching their exercise control structure
- Set delivery method options appropriate to their environment

### Cross-Framework Mapping

Support organizations operating across multiple domains (e.g., defense contractors, international NGOs, multi-sector critical infrastructure) with:
- HSEEP mapping for standard exports
- Translation between framework terminologies
- Multi-framework reporting capability

## Conclusion

HSEEP is not the universal standard—it's the U.S. civilian emergency management standard. Organizations in defense, cybersecurity, international, financial, and private sector BC/DR contexts either use different frameworks entirely or require HSEEP plus sector-specific extensions.

**Minimum viable configurability includes:**
- Inject statuses
- Event categories
- Exercise types
- Delivery methods

Full configurability across all dropdown fields positions Cadence to serve the complete exercise management market rather than just HSEEP-aligned U.S. agencies.

## Sources

- FEMA PrepToolkit: https://preptoolkit.fema.gov
- NIST SP 800-84: https://nvlpubs.nist.gov/nistpubs/legacy/sp/nistspecialpublication800-84.pdf
- MITRE ATT&CK: https://attack.mitre.org
- BCI Good Practice Guidelines: https://www.thebci.org/certification-training/good-practice-guidelines.html
- DRII Professional Practices: https://dri-gcc.org
- FFIEC IT Examination Handbook: https://ithandbook.ffiec.gov

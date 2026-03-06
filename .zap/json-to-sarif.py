#!/usr/bin/env python3
"""
Convert ZAP JSON report to SARIF 2.1.0 format.

Usage:
    python json-to-sarif.py <input.json> <output.sarif>

Maps ZAP risk levels to SARIF severity:
    0 (Informational) -> note
    1 (Low)           -> warning
    2 (Medium)        -> warning
    3 (High)          -> error
"""

import json
import sys
import os

# ZAP risk level -> SARIF level
RISK_TO_LEVEL = {
    "0": "note",
    "1": "warning",
    "2": "warning",
    "3": "error",
}

# ZAP confidence -> SARIF rank (0-100)
CONFIDENCE_TO_RANK = {
    "0": 10.0,   # False Positive
    "1": 30.0,   # Low
    "2": 60.0,   # Medium
    "3": 90.0,   # High
    "4": 99.0,   # Confirmed
}


def convert(input_path: str, output_path: str) -> int:
    """Convert ZAP JSON report to SARIF. Returns finding count."""

    with open(input_path) as f:
        zap_report = json.load(f)

    rules = []
    results = []
    rule_ids_seen = set()

    # ZAP JSON structure: { site: [ { alerts: [...] } ] }
    sites = zap_report.get("site", [])
    if not isinstance(sites, list):
        sites = [sites]

    for site in sites:
        site_name = site.get("@name", "")
        alerts = site.get("alerts", [])

        for alert in alerts:
            plugin_id = alert.get("pluginid", alert.get("alertRef", "unknown"))
            rule_id = f"zap-{plugin_id}"
            risk = str(alert.get("riskcode", "0"))
            confidence = str(alert.get("confidence", "2"))
            name = alert.get("name", alert.get("alert", "Unknown Alert"))
            description = alert.get("desc", "")
            solution = alert.get("solution", "")
            reference = alert.get("reference", "")
            cwe_id = alert.get("cweid", "")

            # Add rule definition (once per rule)
            if rule_id not in rule_ids_seen:
                rule_ids_seen.add(rule_id)
                rule_def = {
                    "id": rule_id,
                    "name": name.replace(" ", ""),
                    "shortDescription": {"text": name},
                    "fullDescription": {"text": _strip_html(description)[:1000] or name},
                    "defaultConfiguration": {
                        "level": RISK_TO_LEVEL.get(risk, "note")
                    },
                    "properties": {
                        "confidence": CONFIDENCE_TO_RANK.get(confidence, 50.0),
                        "zapRiskCode": int(risk),
                    },
                }

                if cwe_id and cwe_id != "-1":
                    rule_def["properties"]["tags"] = [f"CWE-{cwe_id}"]

                if solution:
                    rule_def["help"] = {
                        "text": _strip_html(solution)[:2000],
                        "markdown": _strip_html(solution)[:2000],
                    }

                rules.append(rule_def)

            # Create results for each instance
            instances = alert.get("instances", [])
            if not isinstance(instances, list):
                instances = [instances]

            for instance in instances:
                uri = instance.get("uri", site_name)
                method = instance.get("method", "GET")
                param = instance.get("param", "")
                evidence = instance.get("evidence", "")

                message_parts = [name]
                if param:
                    message_parts.append(f"Parameter: {param}")
                if evidence:
                    message_parts.append(f"Evidence: {evidence[:200]}")

                result = {
                    "ruleId": rule_id,
                    "level": RISK_TO_LEVEL.get(risk, "note"),
                    "message": {"text": " | ".join(message_parts)},
                    "locations": [
                        {
                            "physicalLocation": {
                                "artifactLocation": {"uri": uri},
                            },
                            "properties": {
                                "method": method,
                            },
                        }
                    ],
                }

                if param:
                    result["properties"] = {"parameter": param}

                results.append(result)

    # Build SARIF document
    sarif = {
        "$schema": "https://json.schemastore.org/sarif-2.1.0.json",
        "version": "2.1.0",
        "runs": [
            {
                "tool": {
                    "driver": {
                        "name": "OWASP ZAP",
                        "informationUri": "https://www.zaproxy.org/",
                        "version": zap_report.get("@version", "unknown"),
                        "rules": rules,
                    }
                },
                "results": results,
            }
        ],
    }

    with open(output_path, "w") as f:
        json.dump(sarif, f, indent=2)

    return len(results)


def _strip_html(text: str) -> str:
    """Remove HTML tags from ZAP descriptions."""
    import re

    if not text:
        return ""
    clean = re.sub(r"<[^>]+>", " ", text)
    clean = re.sub(r"\s+", " ", clean).strip()
    return clean


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(f"Usage: {sys.argv[0]} <input.json> <output.sarif>", file=sys.stderr)
        sys.exit(1)

    input_file = sys.argv[1]
    output_file = sys.argv[2]

    if not os.path.exists(input_file):
        print(f"Error: Input file not found: {input_file}", file=sys.stderr)
        sys.exit(1)

    count = convert(input_file, output_file)
    print(f"Converted {count} ZAP findings to SARIF: {output_file}")

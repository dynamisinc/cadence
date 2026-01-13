# Deployment Documentation

This folder contains all documentation related to deploying Cadence to Azure.

## Quick Start

1. **Create Azure Resources** - [AZURE_RESOURCES.md](./AZURE_RESOURCES.md)
2. **Configure GitHub Secrets** - [GITHUB_SECRETS.md](./GITHUB_SECRETS.md)
3. **Run Deployment** - Push to main branch or trigger workflow manually
4. **Verify Deployment** - [DEPLOYMENT_CHECKLIST.md](./DEPLOYMENT_CHECKLIST.md)

## Document Index

| Document | Purpose |
|----------|---------|
| [AZURE_RESOURCES.md](./AZURE_RESOURCES.md) | Step-by-step Azure Portal instructions for creating UAT resources |
| [GITHUB_SECRETS.md](./GITHUB_SECRETS.md) | How to configure GitHub secrets for CI/CD |
| [DEPLOYMENT_CHECKLIST.md](./DEPLOYMENT_CHECKLIST.md) | Pre/post deployment verification steps |
| [ENVIRONMENT_CONFIG.md](./ENVIRONMENT_CONFIG.md) | Local vs Azure configuration patterns |

## Architecture Overview

```
GitHub Repository
       |
       v
+------+-------+
| GitHub       |
| Actions CI   |
+------+-------+
       |
       | (on main branch)
       v
+------+-------+     +------------------+
| Deploy       |---->| Azure App Service|
| Backend      |     | (Web App B1)     |
+--------------+     +--------+---------+
                              |
                              v
                     +--------+---------+
                     | Azure SQL        |
                     | (Basic 5 DTU)    |
                     +------------------+
```

## Estimated Costs

| Environment | Monthly Cost |
|-------------|--------------|
| UAT | ~$20 |
| Production | ~$50-100 (depending on scale) |

## Related Documentation

- [CLAUDE.md](../../CLAUDE.md) - Project overview and AI instructions
- [DEPLOYMENT.md](../DEPLOYMENT.md) - Legacy deployment guide (comprehensive)
- [AZURE_PROVISIONING.md](../AZURE_PROVISIONING.md) - Detailed provisioning guide

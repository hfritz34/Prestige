# GitHub Actions CI/CD Setup

This document explains how to set up the GitHub Actions workflows for the Prestige project.

## Workflows Overview

The project includes three GitHub Actions workflows:

1. **API CI** (`.github/workflows/api-ci.yml`) - Builds and tests the .NET API on every push/PR
2. **Web CI** (`.github/workflows/web-ci.yml`) - Builds the React web app on every push/PR  
3. **Deploy** (`.github/workflows/deploy.yml`) - Deploys both API and Web to Azure on push to main

## Required GitHub Secrets

To enable Azure deployment, you need to set up the following secrets in your GitHub repository:

### 1. AZURE_WEBAPP_PUBLISH_PROFILE_API
- **Purpose**: Deploy profile for the API App Service
- **How to get**: 
  1. Go to Azure Portal → App Services → `prestigeapi`
  2. Click "Get publish profile" to download the `.publishsettings` file
  3. Copy the entire contents of this file
  4. Add as a GitHub secret with name `AZURE_WEBAPP_PUBLISH_PROFILE_API`

### 2. AZURE_WEBAPP_PUBLISH_PROFILE_WEB  
- **Purpose**: Deploy profile for the Web App Service
- **How to get**:
  1. Go to Azure Portal → App Services → `PrestigeWeb`
  2. Click "Get publish profile" to download the `.publishsettings` file
  3. Copy the entire contents of this file
  4. Add as a GitHub secret with name `AZURE_WEBAPP_PUBLISH_PROFILE_WEB`

## Setting Up GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with the exact names above

## App Service Names

The workflows are configured for these Azure App Services:
- **API**: `prestigeapi`
- **Web**: `PrestigeWeb`

If your App Service names are different, update the environment variables in `.github/workflows/deploy.yml`:
```yaml
env:
  AZURE_WEBAPP_NAME_API: 'your-api-app-name'
  AZURE_WEBAPP_NAME_WEB: 'your-web-app-name'
```

## Workflow Triggers

- **CI Workflows**: Trigger on any push/PR to any branch when relevant files change
- **Deploy Workflow**: Only triggers on push to `main` branch
- **Path Filtering**: Workflows only run when relevant files are modified:
  - API CI: `Prestige.Api/**` files
  - Web CI: `Prestige.Web/**` files

## Testing the Setup

1. Create a feature branch: `git checkout -b test-actions`
2. Make a small change to either `Prestige.Api/` or `Prestige.Web/`
3. Push the branch: `git push origin test-actions`
4. Create a Pull Request
5. Verify that the appropriate CI workflow runs
6. Merge to `main` to test the deployment workflow

## Troubleshooting

- **Workflow not running**: Check path filters and branch names
- **Deployment fails**: Verify publish profile secrets are correct
- **Build fails**: Check .NET/Node.js versions match your local setup
- **Permission errors**: Ensure the App Service allows GitHub Actions deployments 
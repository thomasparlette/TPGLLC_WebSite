# TPGLLC Deployment Framework

This folder contains the production deployment workflow and PowerShell scripts used by GitHub Actions on the self-hosted runner.

## Workflow

`.github/workflows/deploy-production.yml`

Triggers on pushes to `master` and on manual dispatch.

## Scripts

- `Backup-Website.ps1` - captures the current IIS site into a timestamped backup folder.
- `Publish-Website.ps1` - places the site into `app_offline.htm`, mirrors the new publish output into the live site folder, then removes `app_offline.htm`.
- `Rollback-Website.ps1` - restores the latest backup.
- `Test-Website.ps1` - performs a local HTTP health check.

## Default locations

- Website: `C:\Websites\TPGLLC\Published`
- Backups: `C:\Deploy\Backups`
- Publish output: `D:\Deploy\Artifacts\publish`

## Notes

The workflow uses `shell: powershell` because the runner currently does not have PowerShell 7 installed.

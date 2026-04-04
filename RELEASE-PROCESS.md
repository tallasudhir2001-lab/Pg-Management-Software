# Release Process

Steps to follow when releasing a new version of PG Management Software.

---

## 1. Decide the Version Number

Follow [Semantic Versioning](https://semver.org/):

| Change Type          | Version Bump | Example         |
|----------------------|--------------|-----------------|
| Bug fixes only       | Patch        | 1.0.0 → 1.0.1  |
| New features         | Minor        | 1.0.0 → 1.1.0  |
| Breaking changes     | Major        | 1.0.0 → 2.0.0  |

---

## 2. Update Version Numbers (3 files)

### .NET Web API
**File:** `PgManagement-WebApi/PgManagement-WebApi/PgManagement-WebApi.csproj`
```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

### Angular UI
**File:** `pg-management-ui/package.json`
```json
"version": "1.1.0",
```

### Flutter App
**File:** `pg_management_app/pubspec.yaml`
```yaml
version: 1.1.0+2
```
> The `+2` is the build number. Increment it every release (Android requires this).

---

## 3. Update CHANGELOG.md

**File:** `CHANGELOG.md`

1. Move everything under `[Unreleased]` into a new version section
2. Add a fresh empty `[Unreleased]` at the top

**Before:**
```markdown
## [Unreleased]
### Added
- Payment proof attachments
### Fixed
- Rent calculation bug for daily tenants

## [1.0.0] - 2026-04-04
```

**After:**
```markdown
## [Unreleased]

## [1.1.0] - 2026-05-01
### Added
- Payment proof attachments
### Fixed
- Rent calculation bug for daily tenants

## [1.0.0] - 2026-04-04
```

---

## 4. Run Migrations (if any)

If there are new EF Core migrations:
```powershell
cd PgManagement-WebApi/PgManagement-WebApi
dotnet ef database update
```

---

## 5. Build & Test All Projects

### Backend
```powershell
cd PgManagement-WebApi
dotnet build
dotnet test  # if tests exist
```

### Angular
```powershell
cd pg-management-ui
npm run build
```

### Flutter
```powershell
cd pg_management_app
flutter build apk --release   # Android
flutter build ios --release    # iOS
```

---

## 6. Commit & Tag

```powershell
git add -A
git commit -m "Release v1.1.0"
git tag v1.1.0
git push origin main --tags
```

---

## 7. Deploy

| Project     | Deployment                                    |
|-------------|-----------------------------------------------|
| Web API     | Publish to server / IIS / Azure App Service   |
| Angular UI  | Deploy `dist/` folder to hosting              |
| Flutter App | Upload APK/AAB to Play Store / IPA to TestFlight |

---

## Quick Checklist

- [ ] Version bumped in `.csproj`
- [ ] Version bumped in `package.json`
- [ ] Version bumped in `pubspec.yaml` (version + build number)
- [ ] `CHANGELOG.md` updated with release date
- [ ] Database migrations applied (if any)
- [ ] Backend builds without errors
- [ ] Angular builds without errors
- [ ] Flutter builds without errors
- [ ] Git commit + tag created
- [ ] Deployed to production

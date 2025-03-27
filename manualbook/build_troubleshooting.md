# Panduan Troubleshooting Build Error

## Daftar Isi
1. [Persiapan Build](#persiapan-build)
2. [Common Build Errors](#common-build-errors)
3. [Database Connection Issues](#database-connection-issues)
4. [Dependencies Check](#dependencies-check)
5. [Clean and Rebuild](#clean-and-rebuild)

## Persiapan Build

### 1. Verifikasi Development Environment
```bash
# Check .NET version
dotnet --version  # Harus 6.0 atau lebih tinggi

# Check PostgreSQL
psql --version    # Harus 14 atau lebih tinggi
```

### 2. Verifikasi Project Structure
```
Park28Maret/
├── src/
│   ├── Services/
│   │   ├── CameraService.cs
│   │   ├── PrinterService.cs
│   │   ├── ConnectionStatusService.cs
│   │   └── MaintenanceService.cs
│   ├── Models/
│   ├── Controllers/
│   └── Program.cs
├── appsettings.json
└── Park28Maret.csproj
```

### 3. Verifikasi Configuration Files
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=parkir2;Username=postgres;Password=postgres"
  },
  "PrinterSettings": {
    "Port": "COM3",
    "BaudRate": 9600
  }
}
```

## Common Build Errors

### 1. Database Migration Errors
```bash
# Reset database migrations
dotnet ef database update 0
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. NuGet Package Errors
```bash
# Restore packages
dotnet restore

# Clear NuGet cache if needed
dotnet nuget locals all --clear
```

### 3. Build Errors
```bash
# Clean solution
dotnet clean

# Rebuild
dotnet build --configuration Release
```

## Database Connection Issues

### 1. Verify PostgreSQL Service
```powershell
# Windows
sc query postgresql-x64-14

# Start service if stopped
net start postgresql-x64-14
```

### 2. Test Database Connection
```sql
-- Using psql
psql -U postgres -d parkir2 -c "SELECT 1"

-- Check database exists
psql -U postgres -c "\l" | findstr parkir2
```

### 3. Reset Database If Needed
```sql
DROP DATABASE IF EXISTS parkir2;
CREATE DATABASE parkir2;
```

## Dependencies Check

### 1. Required NuGet Packages
```xml
<!-- Park28Maret.csproj -->
<ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="6.0.8" />
    <PackageReference Include="System.IO.Ports" Version="6.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="6.0.8" />
</ItemGroup>
```

### 2. System Dependencies
- .NET 6.0 SDK
- PostgreSQL 14
- Visual Studio Build Tools
- Windows SDK (if on Windows)

### 3. Verify Port Access
```powershell
# Check if COM3 is available
[System.IO.Ports.SerialPort]::GetPortNames()

# Check if PostgreSQL port is accessible
Test-NetConnection -ComputerName localhost -Port 5432
```

## Clean and Rebuild

### 1. Full Clean
```bash
# Remove build artifacts
rm -rf bin/ obj/

# Remove packages
rm -rf ~/.nuget/packages/park28maret

# Clean solution
dotnet clean
```

### 2. Rebuild Steps
```bash
# Restore packages
dotnet restore

# Build in debug mode first
dotnet build

# If successful, build release
dotnet build -c Release
```

### 3. Verify Build
```bash
# Run tests if available
dotnet test

# Check build output
dir bin\Release\net6.0
```

## Common Solutions

### 1. Database Issues
- Verify PostgreSQL service is running
- Check connection string in appsettings.json
- Ensure database exists and is accessible
- Reset database permissions if needed

### 2. Printer Issues
- Verify COM3 port is available
- Check printer service configuration
- Update printer drivers if needed
- Test printer connection separately

### 3. Build Issues
- Clear all build artifacts
- Restore NuGet packages
- Update .NET SDK if needed
- Check for file permissions

### 4. Runtime Issues
- Verify all services are running
- Check application logs
- Verify environment variables
- Test each component separately

## Post-Build Verification

### 1. Check Services
```powershell
# Test database connection
dotnet run -- --test-db

# Test printer connection
dotnet run -- --test-printer
```

### 2. Verify Configurations
- Database connection string
- Printer settings
- Application ports
- Log file locations

### 3. Test Basic Operations
- Database CRUD operations
- Printer test page
- Camera capture
- Basic transaction flow 
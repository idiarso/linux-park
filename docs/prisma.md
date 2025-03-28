# Prisma Database Management Documentation

## Overview
This document describes how to use Prisma for managing the database in the ParkIRC application. Prisma is a modern database toolkit that provides type-safe database access and visual database management.

## Setup

### Prerequisites
- Node.js and npm installed
- PostgreSQL database server running
- Database `parkir2` created and configured

### Installation
```bash
# Install Prisma CLI and client
npm install prisma --save-dev
npm install @prisma/client
```

## Database Connection
The database connection is configured in `prisma/schema.prisma`:

```prisma
datasource db {
  provider = "postgresql"
  url      = "postgresql://postgres:postgres@localhost:5432/parkir2"
}
```

## Starting Prisma Studio
Prisma Studio is a visual database management tool. To start it:

```bash
npx prisma studio
```

This will launch Prisma Studio at `http://localhost:5555`

## Common Commands

### Pull Database Schema
To update your Prisma schema from the database:
```bash
npx prisma db pull
```

### Generate Prisma Client
After making changes to the schema:
```bash
npx prisma generate
```

## Database Tables
The following tables are managed through Prisma:

- `SiteSettings`: Application configuration
  - Fields: Id, SiteName, LogoPath, FaviconPath, FooterText, ThemeColor, ShowLogo, LastUpdated, UpdatedBy, PostgresConnectionString

## Security Notes
- The database connection URL in `schema.prisma` contains credentials. In production, use environment variables instead:
```prisma
datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}
```

## Best Practices
1. Always pull the latest schema before making changes
2. Use Prisma Studio for quick data inspection and modification
3. Keep the schema in sync with your C# models
4. Back up your data before making schema changes

## Troubleshooting
If you encounter issues:

1. Connection Problems
   - Verify PostgreSQL is running
   - Check credentials and database name
   - Ensure the database exists

2. Schema Sync Issues
   - Run `npx prisma db pull` to refresh schema
   - Check for any pending migrations

## Useful Links
- [Prisma Documentation](https://www.prisma.io/docs/)
- [Prisma Studio Guide](https://www.prisma.io/docs/concepts/components/prisma-studio)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

#Nintendo Collection
Blazor + Minimal APIs + EF Core + SQLite
Import, browse, and manage a personal Nintendo Collection with CSV import/export, filters, search, sort, paging, and basic edit/delete.

## Features
 - **CSV import** with per-row validation & error report
 - **CSV export** of full collection
 - **Browse UI (Blazor Server)** with platform & CIB filters, search, sort, and paging
 - **CRUD** with edit inline, delete with confirmation
 - **SQLite**
 - **Swagger/OpenAPI** for all endpoints

## Tech Stack
  - **.NET 8** (Minimal APIs, Blazor Server)
  - **EF Core** (SQLite provider)
  - **Swashbuckle** (Swagger)
  - **Simple rate limiting**

## Quick Start (Local)
  Prereqs: .NET 8 SKD, Visual Studio or 'dotnet' CLI.

```bash
# Clone
git clone https://github.com/artdude47/nintendo-collection.git
cd nintendo-collection

#First run creates SQLite db as app.db
dotnet run --project src/Collection.Api

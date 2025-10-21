# Nintendo Collection

Blazor Server App + Minimal APIs + EF Core (SQLite) to import, browse, and manage a personal Nintendo collection.

## Features
 - **CSV import** with per-row validation & error report
 - Fast **table** with search, sort, paging, and per-page selector
 - **Add / Edit / Details** modals 
 - Extra metadata: **publisher, developer, genre, release year, barcode, kind**
 - **Stats:** totals and value summary
 - **Swagger/OpenAPI** for all endpoints

## Tech Stack
  - **.NET 8, Blazor Server**
  - **EF Core + SQLite**
  - **Swashbuckle** (Swagger)
  - **Simple rate limiting**

## Quick Start

```bash
# Clone
git clone https://github.com/artdude47/nintendo-collection.git
cd nintendo-collection

#First run creates SQLite db as app.db
dotnet run --project src/Collection.Api
```

App launches on https://localhost:****.
Swagger at /swagger.

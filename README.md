# Nintendo Inventory System

A **Business Intelligence & Inventory Management** system architected with **.NET 8** and **Blazor Server**. This application demonstrates a modern, **Service-Oriented Architecture (SOA)** designed to handle high-concurrency data visualization and complex inventory tracking.

<img width="1919" height="852" alt="image" src="https://github.com/user-attachments/assets/e978edf6-141c-4aa8-9d10-d0535c54d6cc" />


## Key Architectural Features

### 1. Service-Oriented Architecture (SOA)
Refactored from a monolithic design into discrete, injectable services (`InventoryService`, `CsvImportService`). This decoupling allows the UI to remain lightweight while services handle complex business logic, improving maintainability and testability.

### 2. Thread-Safe Database Access
Implemented the **Factory Pattern** using `IDbContextFactory<AppDbContext>`.
* **Problem:** Blazor Server uses long-lived SignalR circuits. Injecting a standard `DbContext` (scoped to the circuit) causes concurrency exceptions if a user triggers multiple events simultaneously.
* **Solution:** The application generates short-lived, transient `DbContext` instances for each unit of work, ensuring thread safety and preventing `InvalidOperationException` during rapid user interactions.

### 3. Business Intelligence Dashboard
A dynamic, real-time BI layer powered by **ApexCharts**.
* **Aggregations:** Uses efficient LINQ projections to calculate Market Value, Acquisition Cost, and Paper Profit in real-time.
* **Context Switching:** The dashboard features a global state filter that instantly recalculates all charts (Value Distribution, Category Breakdown, Acquisition History) based on the selected console (e.g., switching from "Global" to "N64").

### 4. Robust Data Ingestion Pipeline
A dedicated pipeline for bulk CSV ingestion using `CsvHelper`.
* **Transactional Integrity:** Implements the "Unit of Work" pattern where bulk imports are staged in memory and committed in a single transaction. If any row fails validation, the entire batch is rolled back to prevent data corruption.
* **Dynamic Resolution:** Automatically resolves or creates related entities (e.g., Platforms) during import if they don't already exist.

---

## Tech Stack

* **Framework:** .NET 8, Blazor Server
* **Database:** SQLite, Entity Framework Core 8
* **Architecture:** Service-Oriented, Factory Pattern, Minimal APIs
* **Visualization:** ApexCharts (Blazor Wrapper)
* **Tooling:** Swagger/OpenAPI, CsvHelper

---

## Application Overview

### The Dashboard
*Provides immediate insight into collection value, spending habits, and asset distribution.*

### The Inventory Grid
* **Server-Side Pagination:** Optimized for large datasets; only requested rows are fetched from the DB.
* **Live Search:** Debounced search input to reduce database load.
* **CRUD Operations:** Full management capabilities with modal-based workflows.

---

## Installation & Setup

**Prerequisites:** .NET 8 SDK

1.  **Clone the Repository**
    ```bash
    git clone [https://github.com/artdude47/nintendo-collection.git](https://github.com/artdude47/nintendo-collection.git)
    cd nintendo-collection
    ```

2.  **Run the Application**
    ```bash
    dotnet run --project Collection.Web
    ```

3.  **Automatic Seeding**
    * On the first launch, the `DbInitializer` will detect if the database is missing.
    * It will automatically apply migrations and seed the database with sample Nintendo data (N64, Switch, Wii U items) so the dashboard is never empty.

4.  **Access**
    * **Dashboard:** `https://localhost:7197` (Port may vary)
    * **API Documentation:** `https://localhost:7197/swagger`

---

## Future Improvements
* **Authentication:** Re-integrate Identity for multi-user support (removed for portfolio demonstration purposes).
* **Public API:** Expose read-only endpoints for external consumption.
* **Price Charting API:** Integrate with 3rd party APIs to auto-update `EstimatedValue` based on real-time market data.

---

**Author:** Arthur Couser
*Software Engineer | .NET Enthusiast*

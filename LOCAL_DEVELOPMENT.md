# ERPAICHAT - Local Development Guide (No Docker / LocalDB)

This guide provides instructions for running all microservices and the frontend locally using **SQL Server LocalDB** on Windows without requiring Docker containers.

---

## 🚀 Quick Start (1-Click Launch)

### 1. Update Databases (First Time Setup)
Before launching the microservices for the first time or after pulling model changes, apply EF Core migrations to LocalDB:

```powershell
.\update-databases.ps1
```

This will automatically create/update the following databases in `(localdb)\MSSQLLocalDB`:
- `ERP_InventoryDb`
- `ERP_SupplierDb`
- `ERP_ProcurementDb`

---

### 2. Start All Microservices & Frontend
To launch all 5 backend APIs and the Angular frontend simultaneously in separate PowerShell windows:

```powershell
.\run-all.ps1
```

This starts:
| Service | Technology | Local URL | Swagger / Endpoint |
| :--- | :--- | :--- | :--- |
| **ApiGateway** | YARP Reverse Proxy | `http://localhost:5000` | Routes `/api/...` to APIs |
| **InventoryService** | .NET 10 Web API | `http://localhost:5045` | `http://localhost:5045/swagger` |
| **SupplierService** | .NET 10 Web API | `http://localhost:5046` | `http://localhost:5046/swagger` |
| **ProcurementService** | .NET 10 Web API | `http://localhost:5047` | `http://localhost:5047/swagger` |
| **AIAgentService** | .NET 10 Web API | `http://localhost:5048` | `http://localhost:5048/swagger` |
| **erp-frontend** | Angular App | `http://localhost:4200` | Main Web Application UI |

---

### 3. Stop All Services
To stop all 6 microservices gracefully with one command:

```powershell
.\stop-all.ps1
```

---

## 🛠️ Visual Studio Setup (Multi-Project Startup)

If you prefer to debug using **Visual Studio 2022 / 2026**:

1. Open `ERPAICHAT.slnx` (or `ERPAICHAT.sln`).
2. Right-click the Solution in Solution Explorer -> **Configure Startup Projects...**
3. Select **Multiple startup projects**.
4. Set Action to **Start** for:
   - `ApiGateway`
   - `InventoryService`
   - `SupplierService`
   - `ProcurementService`
   - `AIAgentService`
5. Press `F5` to start debugging all backend services together.

---

## 📋 Database Connection Strings

Each microservice is configured in `appsettings.json` to use local SQL Server LocalDB:

- **InventoryService**: `Server=(localdb)\MSSQLLocalDB;Database=ERP_InventoryDb;Trusted_Connection=True;TrustServerCertificate=True;`
- **SupplierService**: `Server=(localdb)\MSSQLLocalDB;Database=ERP_SupplierDb;Trusted_Connection=True;TrustServerCertificate=True;`
- **ProcurementService**: `Server=(localdb)\MSSQLLocalDB;Database=ERP_ProcurementDb;Trusted_Connection=True;TrustServerCertificate=True;`

---

## 🐰 RabbitMQ (Optional for AI Agent Event Messaging)

`AIAgentService` connects to RabbitMQ on `localhost:5672`. 
If you wish to enable RabbitMQ background messaging, you can start a single lightweight RabbitMQ container when needed:

```powershell
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

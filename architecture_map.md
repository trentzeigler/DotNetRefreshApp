# Architecture Overview

This diagram illustrates how your local development environment is set up to mimic a production-like architecture using Docker.

```mermaid
graph TD
    subgraph Host_Machine ["💻 Your Mac (Host Machine)"]
        style Host_Machine fill:#f9f9f9,stroke:#333,stroke-width:2px
        
        Browser["🌐 Web Browser / Postman"]
        
        subgraph DotNet_App ["🚀 .NET Application (Running locally)"]
            style DotNet_App fill:#e1f5fe,stroke:#0277bd,stroke-width:2px
            Controller["Controllers (API Endpoints)"]
            DbContext["AppDbContext (EF Core)"]
            Config["appsettings.json"]
        end
        
        Terminal["🖥️ Terminal"]
    end

    subgraph Docker_Desktop ["🐳 Docker Desktop"]
        style Docker_Desktop fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
        
        subgraph Container ["📦 SQL Server Container"]
            style Container fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
            SQLEdge["🛢️ Azure SQL Edge (Database Engine)"]
        end
        
        Volume[("💾 Docker Volume (Persisted Data)")]
    end

    %% Flows
    Browser ==>|1. HTTP Request (localhost:5000)| Controller
    Controller -->|2. Calls| DbContext
    DbContext -.->|3. Reads Connection String| Config
    
    DbContext ==>|4. TCP Connection (localhost:1433)| SQLEdge
    
    Terminal -.->|Manage App| DotNet_App
    Terminal -.->|Manage DB| Docker_Desktop
    
    SQLEdge <==>|Read/Write| Volume
```

## How It Works

### 1. The Application Layer (Your Code)
*   **Where it runs:** Directly on your Mac's operating system.
*   **Key Components:**
    *   **Controllers:** Receive requests from the browser (e.g., "Get all users").
    *   **AppDbContext:** The translator. It converts your C# code (`_context.Users.ToList()`) into SQL commands.
    *   **appsettings.json:** Holds the "address" of the database. We set this to `localhost,1433`.

### 2. The Database Layer (Docker)
*   **Where it runs:** Inside a virtualized container managed by Docker Desktop.
*   **Why Docker?** It lets you run a full SQL Server instance without installing it directly on your Mac. It isolates the database software from your system files.
*   **Port Mapping (`1433:1433`):** This is the bridge.
    *   The Container listens on port **1433** inside Docker.
    *   We "map" that to port **1433** on your Mac (localhost).
    *   This allows your .NET app to talk to `localhost:1433` as if the database were installed right there.

### 3. Data Persistence (Volumes)
*   **The Problem:** Containers are ephemeral. If you delete the container, the data inside it is gone.
*   **The Solution:** We defined a **Volume** (`sqlserver_data`) in `docker-compose.yml`.
*   **How it works:** Docker stores the actual database files in a safe place on your Mac's hard drive, outside the container. When you restart the container, it reconnects to this volume, so your data survives.

## The Flow of Data
1.  **Request:** You hit `GET /api/users`.
2.  **Processing:** The `UsersController` asks `AppDbContext` for data.
3.  **Connection:** `AppDbContext` looks at `appsettings.json`, sees `Server=localhost,1433`, and opens a network connection to that port.
4.  **Query:** The request travels through the OS network layer, into Docker, and lands in the SQL Edge container.
5.  **Execution:** SQL Edge executes the query, retrieves data from the Volume, and sends it back.
6.  **Response:** Your app converts the data to JSON and sends it to the browser.

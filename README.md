# shopit
An eCommerce Platform built with ASP.NET and Angular, with multi-tenancy support.
## How to Run the Project

### Run with Dev Container (Recommended)

This repository includes a pre-configured [Dev Container](https://containers.dev/) that includes all necessary tools and dependencies.

**Requirements:**
- [Visual Studio Code](https://code.visualstudio.com/)
- [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
- [Docker](https://www.docker.com/get-started)

**Steps:**

1. Open the repository in VS Code
2. When prompted, click "Reopen in Container" (or use the Command Palette: `Dev Containers: Reopen in Container`)
3. VS Code will build and start the dev container with all dependencies pre-installed
4. Follow the instructions in [Running Both Backend and Frontend](#running-both-backend-and-frontend) below

The dev container includes:
- .NET 10 SDK with .NET Aspire
- Node.js with Angular CLI and pnpm
- Git and Docker CLI
- Useful VS Code extensions (C#, Angular, Docker, GitLens, etc.)
- Pre-configured port forwarding (4200 for frontend, 15000 for Aspire dashboard)

### Run Locally

You can also run the project locally.

**Prerequisites**

- **.NET SDK** (v10.0 or later) with the **.NET Aspire workload** - Required for running the backend
- **Angular** (v21.1.4 or later) and a compatible **Node.js** and **npm** or **pnpm** - Required for running the frontend
- **Docker** - For containerized services orchestrated by .NET Aspire

#### Running the Backend

The backend uses [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) for service orchestration.

1. Navigate to the backend directory:
   ```bash
   cd src/backend
   ```

2. Run the AppHost project:
   ```bash
   dotnet run --project ShopIt.AppHost
   ```

The backend will start and orchestrate all necessary services. The dashboard will typically be available at `http://localhost:15000` (check console output for the exact URL).

#### Running the Frontend

The frontend is an Angular application.

1. Navigate to the frontend directory:
   ```bash
   cd src/frontend
   ```

2. Install dependencies:
   ```bash
   pnpm install
   # or
   npm install
   ```

3. Start the development server:
   ```bash
   pnpm start:portal
   # or
   npm start:portal
   ```

The frontend will be available at `http://localhost:4200/`.

#### Running Both Backend and Frontend

For full-stack development:

1. Open two terminals

2. **Terminal 1 - Start the backend:**
   ```bash
   cd src/backend
   dotnet run --project ShopIt.AppHost
   ```

3. **Terminal 2 - Start the frontend:**
   ```bash
   cd src/frontend
   pnpm install # or use npm
   pnpm start # or use npm
   ```

Access the application at `http://localhost:4200/`.

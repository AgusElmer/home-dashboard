# Home Dashboard

A personal home dashboard application with an ASP.NET Core backend and a React frontend, designed to centralize and display various home-related information.

## Features

*   **Backend:** ASP.NET Core API
*   **Frontend:** React with Vite
*   **Database:** SQLite
*   **Containerization:** Docker

## Getting Started

### Prerequisites

*   .NET SDK (version 8.0 or later)
*   Node.js (version 18 or later)
*   Docker (for containerized deployment)

### Installation

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/your-username/home-dashboard.git
    cd home-dashboard
    ```

2.  **Backend Setup:**

    ```bash
    cd backend
    dotnet restore
    dotnet build
    dotnet ef database update
    cd ..
    ```

3.  **Frontend Setup:**

    ```bash
    cd frontend
    npm install
    cd ..
    ```

### Running the Application

#### Development Mode

1.  **Start the Backend API:**

    ```bash
    cd backend
    dotnet run
    ```

2.  **Start the Frontend Development Server:**

    ```bash
    cd frontend
    npm run dev
    ```

    The frontend will typically be available at `http://localhost:5173`.

#### Docker Compose

To run the application using Docker Compose:

```bash
docker compose up --build
```

This will build and start both the backend and frontend services in Docker containers.

## Project Structure

*   `backend/`: Contains the ASP.NET Core API project.
*   `frontend/`: Contains the React frontend project.
*   `docker-compose.yml`: Defines the Docker services for the application.
*   `Dockerfile`: Dockerfile for building the backend and frontend images.

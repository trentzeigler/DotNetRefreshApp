# AI Email Assistant

A modern, AI-powered email assistant built with .NET 7, OpenAI, and SendGrid. This application allows users to draft and send emails using natural language conversation.

## Features

- **AI Chat Interface**: Conversational interface powered by OpenAI (GPT-4).
- **Email Drafting & Sending**: The AI can draft emails and send them via SendGrid upon user approval.
- **Secure Authentication**: JWT-based authentication with secure password hashing (BCrypt).
- **Mobile Responsive**: Fully responsive UI that works on desktop and mobile devices.
- **Real-time Streaming**: Chat responses are streamed in real-time with typing indicators.
- **Tool Usage Status**: Visual indicators when the AI is using tools (drafting/sending emails).
- **Markdown Support**: Rich text formatting in chat messages.

## Tech Stack

- **Backend**: ASP.NET Core 7.0 (C#)
- **Database**: Azure SQL Database (Entity Framework Core)
- **Frontend**: Vanilla HTML/CSS/JS (No framework overhead)
- **AI**: OpenAI API (Chat Completions & Function Calling)
- **Email**: SendGrid API

## Prerequisites

- .NET 7.0 SDK
- SQL Server (LocalDB or Azure SQL)
- OpenAI API Key
- SendGrid API Key

## Setup

1.  **Clone the repository**
    ```bash
    git clone <repository-url>
    cd DotNetRefreshApp
    ```

2.  **Configure Environment Variables**
    Copy `.env.example` to `.env` and fill in your API keys and database connection string.
    ```bash
    cp .env.example .env
    ```
    
    Required variables:
    - `OPENAI_API_KEY`: Your OpenAI API key
    - `OPENAI_MODEL`: Model to use (e.g., `gpt-4` or `gpt-3.5-turbo`)
    - `SENDGRID_API_KEY`: Your SendGrid API key
    - `JWT_SECRET`: A long, random string for signing tokens
    - `JWT_EXPIRATION_DAYS`: Token validity in days (e.g., 7)
    - `AZURE_SQL_CONNECTION_STRING`: Connection string for your SQL database

3.  **Database Migration**
    Apply the database migrations to create the schema.
    ```bash
    # Ensure .env variables are loaded/exported
    set -a; source .env; set +a
    dotnet ef database update
    ```

4.  **Run the Application**
    ```bash
    ./run-local.sh
    ```
    Or manually:
    ```bash
    dotnet run
    ```

5.  **Access the App**
    Open your browser and navigate to `http://localhost:5000`.

## Project Structure

- `Controllers/`: API controllers (Auth, Conversation)
- `Models/`: Data models (User, Conversation, Email)
- `Services/`: Business logic (Email, AI, JWT, Password Hashing)
- `Views/`: Frontend static files (HTML, CSS, JS)
- `Migrations/`: EF Core database migrations

## License

MIT

# 📚 CampusBook Exchange

A full-stack web application that allows university students to share, exchange, and discover second-hand academic books within their campus community.

---

## 🚀 Features

- 📖 **Book Listings** — Post books you want to share or give away, with details like faculty, condition, and contact info
- 🔍 **AI-Powered Search** — Semantic book recommendations powered by Groq AI (Llama model), matching queries to faculty categories automatically
- 🔄 **Book Requests** — Send and manage borrow/exchange requests between students
- 🔔 **Notification System** — Real-time notifications for incoming and outgoing requests
- 👤 **User Profiles** — Manage your listings, profile photo, and account details
- 🔐 **Session-Based Authentication** — Secure login and registration with BCrypt password hashing and JWT support

---

## 🛠️ Tech Stack

### Backend
- **ASP.NET Core (.NET 10)** — RESTful API
- **Entity Framework Core** — ORM with SQL Server
- **SQL Server** (LocalDB / SQL Express) — Database
- **Groq AI API** — AI-powered semantic book category matching
- **BCrypt.Net** — Password hashing
- **JWT Bearer Authentication**
- **AutoMapper** — Object mapping
- **FluentValidation** — Input validation
- **Serilog** — Logging
- **Swagger / OpenAPI** — API documentation

### Frontend
- **Vanilla HTML / CSS / JavaScript** — No framework
- **Fetch API** — REST communication with backend

---

## 📁 Project Structure

```
CampusBookProject/
├── Controllers/
│   ├── AuthController.cs       # Register, login, logout
│   ├── BooksController.cs      # Book CRUD operations
│   ├── RequestsController.cs   # Borrow/exchange requests
│   ├── ProfilesController.cs   # User profile management
│   ├── NotificationsController.cs
│   └── AiController.cs         # Groq AI integration
├── Models/
│   ├── Users.cs
│   ├── Book.cs
│   ├── Request.cs
│   ├── Profile.cs
│   └── Notification.cs
├── Data/
│   └── AppDbContext.cs
├── Migrations/
├── wwwroot/
│   ├── index.html              # Main page (book listings)
│   ├── login.html              # Login / Register
│   ├── profile.html            # User profile
│   ├── css/
│   └── js/
│       ├── auth.js             # Auth state & API base URL
│       ├── script.js           # Main page logic
│       ├── profile.js          # Profile page logic
│       ├── login.js            # Login/register logic
│       └── notifications.js    # Notification handling
└── Program.cs
```

---

## ⚙️ Setup & Installation

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express
- A [Groq API Key](https://console.groq.com/)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/aksoyysmn/CampusBook-Exchange.git
   cd CampusBook-Exchange
   ```

2. **Configure the database and API key**

   Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=CampusBookProject_DB;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "Groq": {
       "ApiKey": "your_groq_api_key_here"
     }
   }
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. Open your browser and navigate to `https://localhost:{port}`

---

## 🔑 API Endpoints (Summary)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login |
| GET | `/api/books` | List all books |
| POST | `/api/books` | Add a new book |
| POST | `/api/ai/analyze-query` | AI semantic search |
| GET | `/api/requests` | View requests |
| POST | `/api/requests` | Send a book request |
| GET | `/api/notifications` | Get notifications |

Full API documentation available at `/swagger` when running locally.

---

## 🎓 About

This project was developed as a campus book-sharing platform to help university students easily find and exchange academic books with peers from the same or different faculties.

---

## 📄 License

This project is for educational purposes.

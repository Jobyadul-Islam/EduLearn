# EduLearn

EduLearn is a full-featured e-learning platform built with ASP.NET Core MVC (.NET 10), where Instructors create and manage courses, Students enroll and learn, and Admins oversee the whole platform.

## Tech Stack

- **Backend:** ASP.NET Core MVC + Razor Pages (.NET 10), Entity Framework Core (Code-First), SQL Server
- **Auth:** ASP.NET Core Identity, with email OTP verification for students and an admin-PIN application flow for instructors
- **Frontend:** Razor views, Bootstrap 5, vanilla JS — all client libraries (Bootstrap, jQuery, jQuery Validation, Chart.js) are vendored locally in `wwwroot/lib`, no CDN dependency
- **PDF generation:** QuestPDF (certificates, payment receipts, admin revenue/analytics reports)
- **Email:** MailKit (SMTP)
- **Payments:** bKash Tokenized Checkout (sandbox)
- **AI assistant:** Google Gemini API
- **Testing:** xUnit, Moq, EF Core InMemory provider (`EduLearn.Tests`)

## Features

**Student**
- Register with email OTP verification, browse/search/filter courses, enroll (free or paid via bKash)
- View lessons (video/file attachments), track progress, submit assignments
- Take timed quizzes with a configurable pass mark — quiz results never block course completion or certificate eligibility
- Leave a rating/review once a course is fully completed
- Download an auto-generated PDF certificate on course completion
- In-app notifications and an AI chat assistant

**Instructor**
- Apply via an admin-issued PIN, with CV upload; admin sets the login password at approval
- Build courses: modules, lessons, assignments, quizzes (with pass mark and time limit)
- View reviews and quiz results across their own courses
- Get notified when a course is approved/rejected or a student enrolls

**Admin**
- Approve/reject course submissions and instructor applications
- Manage users (search, filter, activate/deactivate)
- Revenue and course analytics dashboards, each exportable as a PDF report
- Generate instructor application PINs

## Project Structure

```
Controllers/            MVC controllers (Course, Instructor, Apply, Bkash, Chat, Notification, ...)
Areas/Admin/             Admin dashboard, controllers and views
Areas/Identity/          ASP.NET Core Identity pages (register, login, OTP, password reset)
Models/                  EF Core entities
Models/ViewModels/       View-specific models
Data/                    ApplicationDbContext (EF Core)
Services/                Business logic and integrations (PDF generation, email, bKash, Gemini chat,
                         quiz grading, notifications, course progress)
Views/                   Razor views
Migrations/              EF Core migrations
wwwroot/                 Static assets, vendored libraries, uploaded files
EduLearn.Tests/          xUnit unit + integration tests
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (SQL Server Express works fine) — the default connection string targets `localhost\SQLEXPRESS`

### Setup

1. Clone the repository and open it in your IDE of choice.
2. Create `appsettings.Local.json` in the project root (already gitignored) to hold your real secrets — it's merged on top of `appsettings.json` at startup. See **Configuration** below for what to put in it.
3. Apply the database migrations:
   ```
   dotnet ef database update
   ```
4. Run the app:
   ```
   dotnet run
   ```
   The admin account configured under `AdminSeed` is created automatically on first run if it doesn't already exist.

## Configuration

`appsettings.json` ships with safe empty/placeholder values for every secret; override them in your own `appsettings.Local.json`:

| Section | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `AdminSeed` | Email/password for the auto-seeded Admin account |
| `Email` | SMTP credentials, used for OTPs, password resets, enrollment/approval emails |
| `Bkash` | bKash Tokenized Checkout sandbox credentials — the app functions without them, but paid checkout will fail |
| `Gemini` | Google Gemini API key for the AI chat assistant — without it, the chat widget shows a graceful fallback message instead of erroring |

## Running Tests

```
cd EduLearn.Tests
dotnet test
```

The suite covers quiz grading logic, course-completion/certificate eligibility, PDF report generation, and integration tests that exercise real controller flows (enrollment, quizzes, admin approvals, reviews, notifications) against an in-memory database.

## Notes

- File uploads (thumbnails, lesson attachments, assignment submissions, instructor CVs, certificates) are stored under `wwwroot/uploads/`.
- The app has not been deployed to a production host — it currently runs against a local SQL Server instance only.

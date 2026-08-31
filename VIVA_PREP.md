# VIVA_PREP.md — Learn Your Own Project

This document exists to get you from "I didn't write this" to "I can defend every part of it" in a couple of days. Read it in order the first time — each section builds on the last. Then use Sections 6 and 9 for last-minute review the night before.

---

## 1. Elevator Pitch

*(Say this out loud a few times until it feels natural — 20 seconds, no notes.)*

"EduLearn is a full e-learning platform, like a scaled-down Udemy, built with ASP.NET Core MVC and SQL Server. Students browse and enroll in courses — free or paid through a real mobile payment gateway called bKash — work through lessons, submit assignments, take timed quizzes, and earn a certificate. Instructors apply to teach, get approved by an admin, and build their own courses. Admins run the whole platform: approving courses and instructors, and pulling revenue and analytics reports. It's a single, complete system — one codebase handles the whole teach-learn-pay-certify loop."

---

## 2. Explain It Like I'm New To This

You don't need to be an expert in every one of these — you need to be able to say what it is and why it's here, in one or two sentences each. That's what an examiner is actually listening for.

### C# and .NET
**What it is:** C# is the programming language; .NET is the runtime/platform that executes it. This project targets **.NET 10**, the current version.
**Why here:** It's a mainstream, strongly-typed, object-oriented language well suited to building structured, maintainable web applications — and it's what ASP.NET Core (below) is written in.

### ASP.NET Core MVC
**What it is:** A web framework from Microsoft. "MVC" stands for **Model-View-Controller** — a way of organizing code into three roles: **Models** (your data), **Views** (the HTML the user sees), and **Controllers** (the code that receives a request, decides what to do, talks to the database, and picks which View to show).
**Why here:** The whole app is built this way. Every page you see corresponds to a Controller action method that runs on the server and returns a rendered HTML page (a "View"). This is **server-rendered** — there's no separate frontend app talking to a JSON API; the browser gets ready-made HTML.
**In this project:** `Controllers/CourseController.cs`, `Controllers/InstructorController.cs`, `Areas/Admin/Controllers/AdminController.cs`, etc. are the Controllers. `Models/Course.cs`, `Models/Enrollment.cs`, etc. are the Models. `Views/Course/Details.cshtml` etc. are the Views.

### Razor / `.cshtml`
**What it is:** Razor is the templating syntax that lets you mix C# and HTML in one file (a `.cshtml` file). `@` introduces C# code inside HTML.
**Why here:** It's the standard view engine for ASP.NET Core MVC — lets you loop over a list of courses and render a `<div>` for each one, for example, without a separate templating language.

### Entity Framework Core (EF Core) — the ORM
**What it is:** An **ORM (Object-Relational Mapper)**. Instead of writing raw SQL, you write C# classes (like `Course`, `Enrollment`) and EF Core translates your C# code (LINQ queries) into SQL behind the scenes, and turns SQL query results back into C# objects.
**Why here:** It's the standard EF Core Code-First approach — you design your database by writing C# classes first, then EF Core generates the actual database tables from them (via "migrations," explained below). It saves you from hand-writing SQL for almost everything.
**In this project:** `Data/ApplicationDbContext.cs` is the single point of contact with the database — every controller injects it and queries through it, e.g. `_context.Courses.Where(c => c.Status == CourseStatus.Approved)`.

### SQL Server
**What it is:** Microsoft's relational database engine. This project uses **SQL Server Express**, the free edition, running locally.
**Why here:** It pairs naturally with EF Core and ASP.NET Core (both Microsoft technologies), and is free for a project of this scale.

### Migrations
**What it is:** A migration is a small, timestamped C# file that describes one change to the database schema (e.g., "add a `PassMarkPercentage` column to the `Quizzes` table"). Running `dotnet ef database update` applies any migrations that haven't been applied yet.
**Why here:** It means the database schema has a full history, in version control, alongside the code — you can see exactly when and why each table/column was added. There are **14 migrations** in this project, from `InitialCreate` to `AddQuizPassMarkAndTimeLimit`.

### ASP.NET Core Identity
**What it is:** A pre-built library from Microsoft that handles user accounts: storing (hashed) passwords, logging in/out, roles, password reset. You don't write your own password-hashing code — Identity does it.
**Why here:** It's the standard choice for auth in an ASP.NET Core app, and this project extends it with a custom `ApplicationUser` class (adding fields like `FullName`, `IsApproved`, `Bio`) rather than reinventing user accounts from scratch.

### Bootstrap 5, jQuery, jQuery Validation, Chart.js
**What they are:** Frontend JavaScript/CSS libraries. Bootstrap gives ready-made responsive layout and components (buttons, cards, modals). jQuery is a JS helper library. jQuery Validation hooks up client-side form validation. Chart.js draws the graphs on the Admin dashboard.
**Why here, and one detail worth knowing:** These are all **vendored** — copied into `wwwroot/lib/` and served from this app itself, not loaded from a CDN (a public internet address). That means the site works with zero internet dependency, which matters for a local demo/defense where you don't want to depend on Wi-Fi working.

### QuestPDF
**What it is:** A C# library for generating PDF files by describing the layout in code (a "fluent" API — you chain method calls like `.FontSize(20).Bold()`).
**Why here:** Used to generate completion certificates, payment receipts, and two admin PDF reports — all without needing any external tool.

### MailKit
**What it is:** A C# library for sending email over SMTP.
**Why here:** Used for every email the app sends — OTP codes, password reset links, enrollment confirmations, instructor approval credentials, assignment deadline reminders.

### bKash
**What it is:** A real, widely-used mobile payment service in Bangladesh (similar in role to a payment gateway like Stripe, but specific to that market). This project integrates with bKash's **sandbox** (test) environment via their "Tokenized Checkout" API.
**Why here:** It's a genuine, working payment integration, not a fake "simulate payment" button — real HTTP calls go to bKash's test servers and come back with real responses.

### Google Gemini API
**What it is:** Google's AI model, used here purely to power a chat widget that has a short conversation with a student and recommends one course.
**Why here:** Adds a modern "AI assistant" feature without needing to run any AI model yourself — it's just an HTTP call to Google's API.

### xUnit, Moq, EF Core InMemory
**What they are:** xUnit is a testing framework for C# (it's what actually runs your test methods and reports pass/fail). Moq lets you create a fake/"mock" version of a dependency (so you can test code without needing the real email server, for example). EF Core InMemory is a version of the database that lives only in memory — fast, and doesn't need a real SQL Server running.
**Why here:** The `EduLearn.Tests` project uses all three so tests run in seconds, with no external setup, and don't touch your real database.

---

## 3. How the Project Actually Works (Step by Step)

### Flow A: A student signs up and logs in

1. **Student fills out the registration form** at `/Identity/Account/Register`. This is a **Razor Page** (not an MVC controller — a slightly different but related pattern where the `.cshtml` file has its own code-behind class). The code-behind is `Areas/Identity/Pages/Account/Register.cshtml.cs`, class `RegisterModel`.
2. When they submit, `RegisterModel.OnPostAsync()` runs. It checks the form is valid, checks no account already exists with that email, then generates a **random 6-digit code** (`new Random().Next(0, 1000000).ToString("D6")`).
3. Here's the important part: **no account is created yet.** The name/email/password/code are bundled into a `PendingRegistration` object and stored in the **session** (server-side temporary storage tied to the browser, not the database) — `HttpContext.Session.SetString(...)`.
4. An email is sent with the code, via `IEmailService.SendEmailAsync(...)`.
5. The student is redirected to `/Identity/Account/VerifyOtp`. They type in the 6-digit code.
6. `VerifyOtpModel.OnPostAsync()` (in `VerifyOtp.cshtml.cs`) pulls the pending registration back out of session, checks the code matches and hasn't expired (10-minute window), and **only now** actually creates the account: `_userManager.CreateAsync(user, pending.Password)`.
7. The new user is added to the `"Student"` role, signed in immediately (`_signInManager.SignInAsync(...)`), and sent to the home page.
8. **Login** later is simpler: `Login.cshtml.cs` checks the account isn't deactivated, then calls `_signInManager.PasswordSignInAsync(...)` — Identity itself checks the password hash and sets the login cookie.

**Why this matters to explain well:** if you're asked "how does registration work," the key insight to state is: *nothing touches the database until the code is verified.* An abandoned signup leaves zero trace.

### Flow B: A student browses, enrolls, and pays for a course

1. `GET /Course` → `CourseController.Index(search, categoryId, sort, page)`. This builds an EF Core query starting from `_context.Courses`, filters to only `Status == CourseStatus.Approved` courses, optionally filters by search text and category, sorts (Newest/Popular/Rating), and paginates (4 per page).
2. Clicking a course → `GET /Course/Details/{id}` → `CourseController.Details`. Loads the course with its modules/lessons, works out which lessons are free-preview (`GetFreePreviewLessonIds` — always the first 2 lessons by database ID), and whether this visitor already has full access.
3. Clicking **Enroll** → `POST /Course/Enroll` → `CourseController.Enroll`. Creates an `Enrollment` row. If the course price is 0, the enrollment starts as **`Active`** immediately (full access). If it's a paid course, it starts as **`Pending`** — enrolled, but not yet paid, so only the free-preview lessons are unlocked.
4. For a paid course, the student goes to **Checkout** (`CourseController.Checkout`), then clicks the bKash button, which POSTs to `BkashController.Pay`. This is a multi-step handshake with bKash's real sandbox API:
   - **Grant Token** — authenticate this app to bKash, get a temporary `idToken`.
   - **Create Agreement** (mode `0000`) — starts a one-time consent step; the student is redirected to bKash's own page to authorize.
   - Student comes back to `AgreementCallback` → **Execute Agreement** confirms it.
   - **Create Payment** (mode `0011`) — now that the agreement exists, actually request the charge; student redirected to bKash again to approve the specific amount.
   - Student comes back to `PaymentCallback` → **Execute Payment** confirms the charge went through.
5. Whatever happens, a `Payment` row is recorded (`Success` or `Failed` — even failures are logged, so there's a full audit trail). On success, the `Enrollment.Status` flips to `Active` and `PaymentDate` is set.

### Flow C: A student learns, gets certified

1. `GET /Course/ViewLesson/{id}` → `CourseController.ViewLesson`. Checks the student is enrolled at all (otherwise `Forbid()`), and whether they have full access or this specific lesson is free-preview. Shows the lesson content plus any assignments and quizzes attached to it.
2. **Mark Complete** → `POST /Course/MarkComplete` → creates or updates a `LessonProgress` row (`IsCompleted = true`).
3. Once **every** lesson in the course has a completed `LessonProgress` row, two things unlock:
   - **Certificate** (`CourseController.Certificate`) — generates a PDF via `CertificateService.Generate(...)` with the student's name, course title, and completion date.
   - **Review** (`CourseController.WriteReview`) — lets them leave a 1–5 star rating + comment, but only once per course.
4. **Quizzes are separate and don't block either of the above.** A student can take a quiz (`CourseController.TakeQuiz` / `SubmitQuiz`), get graded automatically by `Services/QuizGrader.cs`, fail it completely, and still get their certificate as long as the *lessons* are done. This was a deliberate design decision (explained more in Section 7).

### Flow D: An instructor applies and builds a course

1. Admin generates a one-time 6-digit PIN (`AdminController.GeneratePin`).
2. Instructor goes to `/Apply`, enters the PIN (`ApplyController.VerifyPin`), and fills out an application form — including a **required CV upload**.
3. `ApplyController.Form` (POST) creates the `ApplicationUser` immediately, but with `IsApproved = false` and a **password the applicant never sees** (a random string just to satisfy Identity's requirement that `CreateAsync` needs a password). The account literally cannot be logged into yet.
4. Every Admin gets a notification: "New instructor application from X."
5. Admin reviews the application (`AdminController.ViewApplication`, including the CV link) and approves it (`AdminController.Approve`) by **choosing a real login password themselves**, which gets emailed to the instructor.
6. Now the instructor can log in and build: `CreateCourse` → `CreateModule` → `CreateLesson` → `CreateAssignment` / `CreateQuiz`. Every new/edited course starts (or returns to) `Status = Pending`.
7. Admin approves or rejects the course (`AdminController.ApproveCourse` / `RejectCourse`). Approving makes it publicly visible; rejecting can include a reason. Either way, the instructor gets a notification.

### Flow E: Taking a quiz (worth tracing carefully — it's the most recently built feature)

1. `GET /Course/TakeQuiz?quizId=X` loads the quiz with its questions and options, and renders a form with a checkbox per option, plus a JavaScript countdown timer set to the quiz's configured time limit.
2. When the student submits (or the timer hits zero and **auto-submits** the form via JS), `POST /Course/SubmitQuiz` runs.
3. Grading is delegated to `QuizGrader.Grade(quiz, selectedOptionIds)` — explained fully, line by line, in Section 7.
4. The result (`Score`, `TotalQuestions`, `Passed`) is saved as a `QuizResult` row — if the student already attempted this quiz before, the existing row is **updated**, not duplicated (so retaking a quiz always reflects your latest attempt).
5. Redirect to `QuizResult`, which shows Passed/Failed against the instructor's configured pass mark, with a clear note that this doesn't affect course completion.

---

## 4. Feature-by-Feature Breakdown

| Feature | Where | How it works | Why built this way |
|---|---|---|---|
| **Course search/filter/sort/pagination** | `CourseController.Index` | One LINQ query, built up conditionally (`if (!string.IsNullOrWhiteSpace(search)) query = query.Where(...)`), then a `switch` on the sort parameter, then `.Skip()/.Take()` for paging | Simple and readable — no need for a search engine at this scale; EF Core translates it all into one SQL query |
| **Free preview** | `CourseController.GetFreePreviewLessonIds` | Always the first 2 lessons (ordered by database Id) of a course | Lets a non-paying visitor sample a paid course before buying, without needing a separate "is this lesson free" flag on every lesson |
| **Payment (bKash)** | `Controllers/BkashController.cs`, `Services/BkashPaymentService.cs` | 5-call HTTP handshake with bKash's sandbox (see Flow B) | Real integration, not a fake button — demonstrates working with a genuine third-party API with its own quirks (see Section 8 for the bugs found) |
| **Assignments** | `SubmitAssignment` (Course), `CreateAssignment` (Instructor) | File upload saved to `wwwroot/uploads/submissions/`; resubmitting **overwrites** the previous submission (same row, not a new one) | Keeps one clean record per student per assignment — matches how a real classroom only cares about your latest submission |
| **Quizzes** | `Quiz`/`QuizQuestion`/`QuizOption`/`QuizResult` models, `QuizGrader.cs`, `CourseController.TakeQuiz/SubmitQuiz` | Auto-graded, supports multiple correct answers per question, timed with client-side auto-submit, has a configurable pass mark | Auto-grading means no manual marking workload for the instructor; deliberately excluded from certificate logic (see Section 7) |
| **Certificates** | `CourseController.Certificate`, `Services/CertificateService.cs` | Generates a landscape A4 PDF on the fly, only when every lesson is complete | On-demand generation means no need to store/manage certificate files — it's recomputed fresh every time it's requested |
| **Reviews** | `Review` model, `WriteReview` action | One review per student per course, gated on 100% completion | Prevents drive-by reviews from people who never actually took the course |
| **Notifications** | `Notification` model, `Services/NotificationService.cs`, bell icon in `_Layout.cshtml` | A live database query on every page load for a logged-in user (`Notifications.Where(n => n.UserId == currentUserId)...Take(8)`) | Deliberately simple — no real-time push, no caching — appropriate for a small app where a page-load query is cheap |
| **AI chat assistant** | `Controllers/ChatController.cs`, `Services/GeminiChatService.cs` | Builds a system prompt from the **live course catalog** and the student's enrollments each time, so the AI can't invent fake courses; conversation kept in session, capped at 40 turns | Grounding the AI's knowledge in real data (rather than trusting it to "know" the courses) avoids it hallucinating course names or prices |
| **Admin reports** | `AdminController.Revenue/Analytics` + PDF export actions, `Services/ReportPdfService.cs` | Same underlying data method (`GetRevenueData`/`GetAnalyticsData`) feeds both the on-screen Chart.js view and the PDF export, so they can never show different numbers | One source of truth for the numbers, rendered two ways |
| **Accessibility** | Across many views; see git commit `0c11462` | Added `alt` text, `<label for="...">` pairing on every form control, ARIA labels on icon buttons, fixed a real keyboard-trap bug in the star-rating widget | Shows attention to a commonly-overlooked but important quality attribute — good material for a viva question |

---

## 5. Key Concepts & Terminology I Must Know

- **MVC (Model-View-Controller):** the architectural pattern this whole app follows. Model = data (`Models/`), View = HTML template (`Views/*.cshtml`), Controller = the code that handles a request and decides what to do.
- **ORM (Object-Relational Mapper):** software that converts between C# objects and database rows/tables automatically. EF Core is the ORM here.
- **Code-First:** you write C# model classes first; the database schema is generated *from* them (via migrations), rather than designing the database first and generating classes from it.
- **Migration:** a versioned, incremental description of one database schema change. Applied with `dotnet ef database update`.
- **DbContext:** the EF Core class (`ApplicationDbContext`) representing "a connection to the database plus a set of tables you can query." Every controller gets one injected.
- **Dependency Injection (DI):** instead of a class creating its own dependencies (like a database connection) directly, they're "injected" into its constructor by the framework. Look at any controller's constructor, e.g. `CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ...)` — ASP.NET Core supplies all of these automatically, based on what's registered in `Program.cs`.
- **Foreign Key (FK):** a column that points to the primary key of another table, creating a relationship (e.g., `Enrollment.CourseId` points to `Course.Id`).
- **Cascade delete vs Restrict:** what happens to related rows when you delete a row. `Cascade` means related rows get deleted too (used here for course content: delete a Course, its Modules/Lessons/etc. go with it). `Restrict` means the delete is *blocked* if related rows exist (used here for anything pointing back to a user — you can't accidentally wipe someone's progress by deleting an unrelated row).
- **LINQ:** "Language Integrated Query" — writing database queries as C# code (`_context.Courses.Where(c => c.Price == 0)`) instead of raw SQL strings. EF Core translates it to SQL.
- **Claims-based authentication:** ASP.NET Core Identity doesn't just know "you're logged in" — it attaches a set of "claims" to you (like your user ID, your role). `[Authorize(Roles = "Admin")]` on a controller checks one of those claims.
- **Session:** small pieces of server-side data tied to a specific browser via a cookie, but *not* stored permanently in the database. Used here for the pending OTP registration, the chat history, and a couple of in-flight payment/PIN states.
- **TempData vs ViewBag vs ViewData:** three ways to pass data from a controller to a view (or across a redirect). `TempData` survives exactly one redirect (used for "your review was blocked" style messages). `ViewBag`/`ViewData` only last for the current request/view render.
- **Upsert:** "update or insert" — check if a row already exists for this combination (e.g. this student + this quiz); update it if so, insert a new one if not. Used for `QuizResult`, `AssignmentSubmission`, `LessonProgress`.
- **Anonymous type:** a C# object created with `new { Foo = x, Bar = y }` with no named class. Used a lot in this project's reporting queries because EF Core can translate anonymous-type projections to SQL, but cannot translate tuple literals.
- **Background Service:** a piece of code that runs continuously in the background of the web app, independent of any incoming request. `DeadlineReminderBackgroundService` is one — it wakes up every 6 hours and checks for assignment deadlines coming up.
- **HashSet & Set equality:** a HashSet is a collection with no duplicates, and `SetEquals` checks if two sets contain exactly the same items regardless of order. This is the core trick behind quiz grading — see Section 7.
- **Mocking (in testing):** creating a fake stand-in for a dependency (like "a fake email service that pretends to send an email but doesn't really") so you can test your code in isolation. The `Moq` library does this here.
- **Unit test vs Integration test:** a unit test checks one small piece of logic in total isolation (e.g. `QuizGraderTests` — no database involved at all). An integration test checks that several pieces work correctly *together* — e.g. `EnrollmentAndCertificateFlowTests` actually runs the real `CourseController` code against a real (in-memory) database.
- **In-memory database:** a version of the database that exists only in RAM for the duration of a test, then disappears — fast, and doesn't touch your real SQL Server.

---

## 6. Likely Viva Questions & Model Answers

**Q1: What is this project, in one sentence?**
> "An e-learning platform built in ASP.NET Core MVC where students enroll in and complete courses — with real payment, quizzes, and certificates — instructors build courses, and admins moderate the platform."

**Q2: Why did you choose ASP.NET Core MVC instead of, say, a React frontend with a separate API?**
> "Because the whole application — pages, forms, business logic — is naturally suited to server-rendered pages, and MVC keeps the codebase in one language, one project, and one clear request-response flow. A separate API and frontend would add real complexity — two codebases, CORS, a build pipeline for the frontend — without a corresponding benefit for an app of this size and this deadline."

**Q3: Why SQL Server and not something like MongoDB?**
> "The data is naturally relational — courses have modules, modules have lessons, lessons have assignments and quizzes, students enroll in courses and progress through lessons. Those are exactly the kind of one-to-many and many-to-many relationships a relational database is built for, and SQL Server pairs directly with EF Core."

**Q4: Walk me through what happens when a user registers.**
> *(Use Flow A from Section 3 — the key point to hit is that nothing is saved to the database until the OTP is verified.)*

**Q5: Why store the pending registration in session instead of the database?**
> "So an abandoned signup — someone who never enters the code — leaves zero trace. If I saved a half-verified user row to the database first, I'd need extra cleanup logic for all the unverified accounts that pile up. Session data just expires naturally."

**Q6: How does authentication actually work under the hood?**
> "ASP.NET Core Identity handles it — it hashes and stores passwords, and on a successful login it issues an encrypted cookie that identifies the user on every future request. I didn't write any password hashing or session cookie logic myself; Identity provides `UserManager` and `SignInManager` for that."

**Q7: How do you tell a Student from an Instructor from an Admin in code?**
> "Roles. Identity has a built-in role system — I seed three roles at startup (`Admin`, `Instructor`, `Student`) in `Program.cs`, and every controller or action that should be restricted has an `[Authorize(Roles = "...")]` attribute. For example, `InstructorController` is entirely locked to the Instructor role at the class level."

**Q8: How does an instructor account get created — can anyone just sign up as an instructor?**
> "No — public registration only ever creates Student accounts. Becoming an instructor requires an Admin to first generate a one-time PIN, which the applicant enters before they can even see the application form. And even after applying, the account can't log in until an Admin explicitly approves it and sets the real password."

**Q9: Why does the Admin set the instructor's password instead of the instructor choosing their own?**
> "It was a deliberate design decision so the instructor can't set up working login credentials before they're actually approved — the account exists in the database the moment they apply, but it's completely unusable until an Admin reviews the CV and application and consciously decides to grant access."

**Q10: Explain your database schema at a high level.**
> "At the center is Course, which has a Category, an Instructor, and a tree of content underneath it — Modules containing Lessons, which have Assignments and Quizzes. Students interact with all of that through separate join-style tables: Enrollment links a student to a course, LessonProgress tracks completion per lesson, AssignmentSubmission and QuizResult record their work, and Review holds their rating. There's also Payment for transaction records and Notification for the in-app notification system."

**Q11: How do you enforce that deleting something doesn't break the database or wipe unrelated data?**
> "Through EF Core's delete-behavior configuration in `ApplicationDbContext.OnModelCreating`. I use two rules consistently: course content cascades — delete a Course and its Modules, Lessons, Assignments, and Quizzes all go with it, since they only exist as part of that course. But anything that points back to a user — Enrollment, LessonProgress, QuizResult, Review — uses Restrict, never Cascade. That means deleting a Course can't accidentally destroy a student's grade history through some indirect path, and it also avoids a SQL Server error you get from having multiple cascade paths to the same table."

**Q12: What's the difference between how you handle course-owned data versus user-owned data?**
> "Course-owned content — modules, lessons, assignments, quizzes — cascades on delete, because that data has no meaning outside its parent course. User-linked data — enrollments, progress, results, reviews, payments — uses Restrict, because that data represents a real person's history and shouldn't ever be silently destroyed as a side effect of deleting something else."

**Q13: How does the quiz grading algorithm work?**
> "Each question can have one or more correct answers. I grade a question as correct only if the set of options the student selected exactly matches the set of options marked correct — using a HashSet and `SetEquals`. That means for a multi-answer question, you have to select every correct option and no incorrect ones — partial credit isn't given. It's implemented in one small, dependency-free class, `Services/QuizGrader.cs`, specifically so it could be unit tested without needing a database."

**Q14: Why doesn't a failed quiz block the certificate?**
> "That was an explicit design decision — the certificate and the course-completion logic are both based purely on whether every *lesson* has been marked complete, tracked separately in `LessonProgress`. A quiz result is stored, and the student sees Pass/Fail, but it's never checked anywhere in the certificate or review-eligibility logic. I even wrote a shared helper, `CourseProgressCalculator.IsComplete`, specifically so certificate eligibility and review eligibility use the exact same completion check and can never disagree with each other — and neither of them ever looks at QuizResult at all."

**Q15: How does the payment integration work?**
> "It's a real integration with bKash's sandbox environment, not a fake button. It's a multi-step handshake: get an auth token, create a payment agreement (the student authorizes on bKash's own page), execute that agreement, create the actual payment, and execute that. Every attempt — success or failure — gets recorded as a Payment row, so there's a full audit trail even for declined payments."

**Q16: What was the hardest bug you had to fix?**
> "Probably the bKash 'Invalid intent' error. bKash's API silently rejected my payment request and gave almost no useful error detail. I isolated it by testing the raw API directly with curl instead of going through the whole app each time, and found two separate problems: the `intent` field had to be lowercase `'sale'` — I had it capitalized — and a `payerReference` field was required on the actual payment call, not just the earlier agreement call, and I'd only been sending it on the agreement step."

**Q17: How do you handle a case where an external service (email, payment, AI) isn't configured or is down?**
> "Every external integration has an `IsConfigured` check and degrades gracefully rather than crashing the request. If SMTP credentials aren't set, `SmtpEmailService` just logs a warning and returns false — enrollment still succeeds even if the confirmation email can't send. If the Gemini API key is missing, the chat widget returns a friendly 'not set up yet' message instead of erroring. Same idea for bKash."

**Q18: What testing did you do?**
> "I built a separate test project, `EduLearn.Tests`, using xUnit. There are two kinds of tests: unit tests for pure logic — like the quiz grading algorithm and the course-completion check — that run with no database at all, and integration tests that exercise real controller code against an in-memory database, covering full flows like enroll-complete-certificate and admin course approval. There are 31 tests total, all passing."

**Q19: Why did you write both unit tests and integration tests instead of just one kind?**
> "They catch different things. A unit test on `QuizGrader` proves the grading math itself is correct in every edge case — partial selection, exact boundary, empty quiz — fast and isolated. An integration test proves those pieces actually work correctly wired together through a real controller and a real database — for example, that submitting a quiz twice updates the same row instead of creating a duplicate. I needed both kinds of confidence."

**Q20: What design pattern does this project follow? Did you use a Repository pattern?**
> "It's MVC, and no, there's no separate Repository layer — controllers talk to `ApplicationDbContext` directly. That's a deliberate simplicity choice for a project of this size; a repository layer adds an abstraction that mainly pays off when you might swap out your database technology, which was never a requirement here."

**Q21: Isn't putting business logic directly in controllers bad practice?**
> "For a project this size, keeping most logic in controllers keeps the code easy to follow end-to-end. That said, I did pull out logic that specifically needed to be reusable or independently testable into a Services layer — quiz grading, course-completion calculation, PDF generation, email, payment, and notifications are all separate service classes, not buried inside a controller."

**Q22: How would this scale to thousands of concurrent users?**
> "Honestly, not without changes. The notification bell runs a live database query on every single page load rather than caching anything, file uploads go straight to local disk rather than cloud storage, and there's no caching layer anywhere. For a bigger deployment I'd add response/output caching, move uploads to blob storage, and probably introduce a proper background job queue instead of the simple 6-hour-loop background service. For the scale this was built for — a single institution — it's fine as-is."

**Q23: Where does file upload go, and is that production-ready?**
> "Uploaded files — thumbnails, lesson attachments, assignment submissions, instructor CVs — are saved to `wwwroot/uploads/` on local disk. That's fine for a single-server local setup, but it wouldn't survive a typical cloud redeploy or work if you scaled to multiple server instances, since each instance would have its own separate disk. In a real deployment I'd move that to cloud blob storage."

**Q24: What was descoped or left incomplete, and why?**
> "Actual production deployment — there's no hosting, no CI/CD pipeline, no production database. That was a conscious decision to focus effort on making the application itself fully correct and tested rather than spending the remaining time on infrastructure work. Also, instructors can create a quiz but there's currently no Edit Quiz feature — only Create."

**Q25: How do you know the app actually works — did you just assume it?**
> "No — I verified everything live, not just by reading code. I ran the app and drove it through real HTTP requests as a student, an instructor, and an admin, checking the database directly afterward to confirm the right rows were created. That process actually caught a real bug — the notification system existed in the UI but only one of its four intended triggers was ever wired up to fire — which I then fixed and re-verified the same way."

**Q26: What's an example of a bug you found through that live verification, not just from writing tests?**
> "The notification gap in Q25 is the best example — all the code paths and the bell/dropdown UI looked complete on paper, and it would have looked fine in a code review. It was only by actually enrolling in a course as a real test student and then checking whether the instructor's Notifications table got a new row that I discovered nothing had actually fired."

**Q27: Why use anonymous types in some of your LINQ queries instead of proper model classes?**
> "Mainly in the analytics and reporting queries — EF Core can translate a projection into an anonymous type (`new { c.Id, c.Title, EnrollmentCount = c.Enrollments.Count }`) into real SQL, including sorting and limiting on the server. It can't do the same with a C# tuple literal directly inside the query — that throws at compile time. So the pattern is: project to an anonymous type, pull the (small) result into memory with `.ToList()`, then convert to whatever shape I actually need afterward."

**Q28: How does the countdown timer for quizzes actually work — is it enforced by the server?**
> "It's a client-side JavaScript timer that auto-submits the form when it reaches zero. Honestly, that's a real limitation — a technically sophisticated user could disable JavaScript or tamper with it to bypass the time limit, since the server doesn't independently track or enforce elapsed time. For this project's scope, that was an acceptable trade-off; a fully hardened version would need the server to record a start timestamp and validate elapsed time on submission."

**Q29: Why is the notification system a live query instead of using something like SignalR for real-time push?**
> "Simplicity, appropriate to scale. SignalR would let notifications appear instantly without a page refresh, but it adds a persistent connection and real infrastructure complexity. Here, a notification just needs to be visible the next time you load a page, which a normal database query handles fine — the same pattern the Admin dashboard already used for its pending-approvals badge."

**Q30: If you had one more week, what would you build next?**
> "I'd prioritize an Edit Quiz feature — right now instructors can only create, not modify. After that, I'd move file uploads to cloud storage and actually deploy the application somewhere real, since right now it's only ever run locally."

---

## 7. "What If They Ask Me to Explain This Code" — Line by Line

### `Services/QuizGrader.cs` — the grading algorithm

```csharp
public static QuizGradeResult Grade(Quiz quiz, IEnumerable<int>? selectedOptionIds)
{
    var selectedSet = new HashSet<int>(selectedOptionIds ?? Enumerable.Empty<int>());
```
This takes whatever list of option IDs the student ticked (could be `null` if they submitted nothing) and puts them in a `HashSet` — a collection where order doesn't matter and there are no duplicates. `?? Enumerable.Empty<int>()` means "if it's null, just use an empty list instead," so we never crash on a blank submission.

```csharp
    int score = 0;
    foreach (var question in quiz.Questions)
    {
        var correctSet = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
        var selectedForQuestion = question.Options.Select(o => o.Id).Where(selectedSet.Contains).ToHashSet();

        if (correctSet.SetEquals(selectedForQuestion))
        {
            score++;
        }
    }
```
For every question in the quiz: build the set of IDs that are actually correct (`correctSet`), and separately, out of *this question's* options, work out which ones the student picked (`selectedForQuestion` — filtering the question's own options down to the ones present in the student's overall selected set). Then `SetEquals` checks these two sets contain exactly the same members. If they do, one point. If the student missed a correct option, or picked an extra wrong one, the sets won't match and no point is given — **there's no partial credit.**

```csharp
    int totalQuestions = quiz.Questions.Count;
    bool passed = totalQuestions > 0 && (100.0 * score / totalQuestions) >= quiz.PassMarkPercentage;
```
Turn the raw score into a percentage and compare it to the instructor's configured pass mark. The `totalQuestions > 0 &&` guard means a quiz with zero questions can never accidentally count as "passed" from a division producing something weird.

```csharp
    return new QuizGradeResult { Score = score, TotalQuestions = totalQuestions, Passed = passed };
}
```
Return a small plain object with the three numbers the caller needs. Notice this whole method takes plain data in and returns plain data out — it never touches the database, a controller, or anything else. That's exactly why it was pulled out into its own class: it's trivial to unit test in total isolation.

### `Services/CourseProgressCalculator.cs` — the shared completion check

```csharp
public static bool IsComplete(int totalLessons, int completedLessons)
{
    return totalLessons > 0 && completedLessons >= totalLessons;
}
```
That's the entire method. It's deliberately this small. `totalLessons > 0` means a course with no lessons at all can never be "complete" (there'd be nothing to prove you learned). This one function is called from two different places — `CourseController.Certificate` and `CourseController.HasCompletedCourse` (used by the review feature) — guaranteeing both features agree on what "finished the course" means, forever, without needing to keep two copies of the same logic in sync by hand.

### `Data/ApplicationDbContext.cs` — the delete-behavior rules (excerpt)

```csharp
builder.Entity<Module>()
    .HasOne(m => m.Course)
    .WithMany(c => c.Modules)
    .HasForeignKey(m => m.CourseId)
    .OnDelete(DeleteBehavior.Cascade);
```
Read this as: "A Module has one Course; a Course has many Modules; the link is the `CourseId` column; if the Course is deleted, delete this Module too." This is repeated down the whole content tree (Module → Lesson → Assignment/Quiz → Question → Option), so deleting a course cleans up everything underneath it automatically.

```csharp
builder.Entity<Enrollment>()
    .HasOne<ApplicationUser>()
    .WithMany()
    .HasForeignKey(e => e.StudentId)
    .OnDelete(DeleteBehavior.Restrict);
```
Contrast this one: "An Enrollment points to an ApplicationUser via `StudentId`; if someone tries to delete that user, **block it** (Restrict) instead of cascading." `HasOne<ApplicationUser>()` (with no property in the angle-bracket-less version) and `.WithMany()` with nothing inside means "there's a relationship, but I'm not bothering to expose a full C# navigation collection back on `ApplicationUser` for it" — we just need the constraint enforced, not a `user.Enrollments` list to loop over in code.

### `Areas/Identity/Pages/Account/VerifyOtp.cshtml.cs` — `OnPostAsync` (the account-creation moment)

```csharp
if (DateTime.Now > pending.ExpiresAt)
{
    HttpContext.Session.Remove(RegisterModel.SessionKey);
    ModelState.AddModelError(string.Empty, "This code has expired. Please register again.");
    return Page();
}
```
If more than 10 minutes have passed since the code was generated, clear the pending session data and show an error — forcing them to start over rather than letting a stale code linger forever.

```csharp
if (Input.Code != pending.Otp)
{
    ModelState.AddModelError(string.Empty, "Incorrect code. Please try again.");
    return Page();
}
```
Simple string comparison of what they typed against what was emailed. Note: this does *not* clear the session on a wrong guess — they get to try again without restarting registration.

```csharp
var user = new ApplicationUser { UserName = pending.Email, Email = pending.Email, FullName = pending.FullName, EmailConfirmed = true };
var result = await _userManager.CreateAsync(user, pending.Password);
```
Only now — after both checks pass — is an actual `ApplicationUser` object built and handed to Identity's `CreateAsync`, which hashes the password and inserts the row. `EmailConfirmed = true` is set directly because *we* already proved they own the email address via the OTP — there's no need for Identity's own separate confirmation-link system on top of that.

```csharp
await _userManager.AddToRoleAsync(user, "Student");
HttpContext.Session.Remove(RegisterModel.SessionKey);
await _signInManager.SignInAsync(user, isPersistent: false);
return RedirectToPage("/Index", new { area = "" });
```
Put them in the Student role, clean up the now-unneeded session data, sign them in immediately (no separate "please log in" step), and send them to the home page already authenticated.

### `Controllers/CourseController.cs` — `SubmitQuiz` (the upsert pattern in action)

```csharp
var existingResult = _context.QuizResults.FirstOrDefault(r => r.QuizId == quizId && r.StudentId == userId);

if (existingResult != null)
{
    existingResult.Score = grade.Score;
    existingResult.TotalQuestions = grade.TotalQuestions;
    existingResult.Passed = grade.Passed;
    existingResult.AttemptDate = DateTime.Now;
}
else
{
    _context.QuizResults.Add(new QuizResult { QuizId = quizId, StudentId = userId, Score = grade.Score, TotalQuestions = grade.TotalQuestions, Passed = grade.Passed, AttemptDate = DateTime.Now });
}

_context.SaveChanges();
```
Look for a `QuizResult` row already tied to this exact (quiz, student) pair. If one's found, mutate its fields in place — because `existingResult` is an object EF Core is already tracking, just changing its properties is enough; calling `SaveChanges()` later writes an `UPDATE`. If none is found, build and `Add` a brand-new row, which becomes an `INSERT` on save. Either way, exactly one row per student per quiz always exists — this is the "upsert" pattern, used identically for `AssignmentSubmission` and `LessonProgress` elsewhere in this same file.

### `Program.cs` — application startup (the pieces that matter most)

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```
Registers Identity, using our custom `ApplicationUser` class instead of the default bare-bones one, adds role support, and tells it to store everything in our `ApplicationDbContext`. `RequireConfirmedAccount = false` because — as covered above — email verification is handled by our own OTP system, not Identity's built-in one.

```csharp
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, GeminiChatService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IBkashPaymentService, BkashPaymentService>();
builder.Services.AddHostedService<DeadlineReminderBackgroundService>();
```
This is **Dependency Injection registration** — telling ASP.NET Core "whenever some class asks for an `IEmailService` in its constructor, give it a `SmtpEmailService` instance." `AddScoped` means one instance per web request. `AddHostedService` is different — it registers something that starts running in the background the moment the app starts, independent of any request.

```csharp
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Instructor", "Student" };
    foreach (var role in roles) { if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role)); }
    // ...then seeds the Admin account the same way, if it doesn't already exist
}
```
Runs once, every time the app starts: make sure the three roles exist in the database, and make sure there's at least one working Admin account (from configuration, or a hardcoded fallback). This means a completely fresh database becomes usable the moment you run the app — no manual setup script needed.

---

## 8. Weak Points / Things I Should Be Ready to Defend

Be upfront about these if asked — an examiner respects "yes, I know that's a limitation, here's why/what I'd do differently" far more than getting caught pretending everything is perfect.

1. **No repository/service abstraction between controllers and EF Core.** Controllers call `_context` directly everywhere. *If pushed:* "That's a deliberate simplicity trade-off for this project's size — a repository layer mainly earns its cost when you expect to swap data-access technology, which was never a real requirement here. I did still extract logic that specifically needed to be reusable or testable — grading, progress calculation, PDF/email/payment — into a proper Services layer."

2. **The quiz timer is enforced client-side only.** A student could disable JavaScript to bypass it. *If pushed:* "Correct, and I'd flag that as a real gap in a security-hardened version — the server doesn't independently track elapsed time. It would need a server-recorded start timestamp checked against submission time to be tamper-proof."

3. **No production deployment.** The app has only ever run against a local SQL Server instance. *If pushed:* "That was a conscious scope decision — I prioritized getting the application itself fully correct and tested over spending remaining time on hosting/infrastructure, which wasn't the focus of this assessment."

4. **File uploads go to local disk (`wwwroot/uploads/`).** Won't survive a typical cloud redeploy or scale across multiple servers. *If pushed:* "For a real deployment I'd move that to blob storage — Azure Blob or similar — this works because it's a single local instance."

5. **The notification system uses a live, uncached database query on every page load.** *If pushed:* "It's intentionally simple — appropriate for the scale of this app. At real scale I'd cache it or move to a push-based system like SignalR, but a query this small on every page load isn't a real cost here."

6. **No rate limiting on OTP requests or the AI chat beyond a per-session turn cap.** Someone could hammer the "resend code" or chat endpoint. *If pushed:* "That's true — there's no IP-based throttling. The chat has a hard 40-turn session cap as a basic cost control, but a production version would need proper rate limiting on both."

7. **Heavy use of `ViewBag`/`TempData` instead of strongly-typed ViewModels everywhere.** Some views cast `ViewBag` values with `(List<...>)ViewBag.X`, which isn't compile-time safe. *If pushed:* "That's a fair critique — it trades some type safety for speed of development. A more rigorous version would use a dedicated ViewModel class per view instead."

8. **Instructors can create but not edit a quiz.** *If pushed:* "Correct gap — noticed but not built due to time. The fix would follow the exact same Create/Edit pattern already used for courses."

9. **`NuGetAudit` is disabled in the main project**, suppressing dependency vulnerability warnings; the test project (without that suppression) shows several inherited advisories from `MailKit`/`MimeKit`. *If pushed:* "I'd want to review and re-enable that before any real deployment — right now it's suppressed and those warnings aren't addressed."

10. **Admin generates the instructor's real password and it's emailed in plain text in the email body.** *If pushed:* "That's a legitimate security concern for a production system — email isn't a fully secure channel. For this project's scope it mirrors a common small-scale onboarding pattern, but a hardened version would send a one-time setup link instead of the password itself."

---

## 9. Glossary

**Admin** — one of three user roles; manages the whole platform (approvals, users, reports).

**Anonymous type** — a C# object created with `new { ... }` with no named class; used here because EF Core can translate it into SQL where it can't translate a tuple.

**ASP.NET Core** — Microsoft's web framework for building server-rendered or API-based web applications in .NET.

**ASP.NET Core Identity** — the built-in library handling user accounts, password hashing, login/logout, and roles.

**Authorization** — checking whether a logged-in user is *allowed* to do something (vs. Authentication, which checks *who* they are). Enforced here via `[Authorize(Roles = "...")]`.

**Background Service** — code that runs continuously alongside the web app, independent of incoming requests (`DeadlineReminderBackgroundService`).

**bKash** — a Bangladeshi mobile payment gateway integrated here via its sandbox (test) API.

**Cascade delete** — deleting a row automatically deletes related rows too. Used for course content.

**Certificate** — a PDF generated on course completion, via `CertificateService`.

**Chart.js** — a JavaScript charting library, vendored locally, used on the Admin dashboard and Revenue report.

**Claims** — pieces of identity information (user ID, role, etc.) attached to a logged-in user by ASP.NET Core Identity.

**Controller** — the C# class that receives a web request and decides what to do (the "C" in MVC).

**DbContext** — the EF Core class representing a database connection plus queryable tables (`ApplicationDbContext`).

**Dependency Injection (DI)** — supplying a class's dependencies via its constructor rather than it creating them itself; configured in `Program.cs`.

**Enrollment** — the record linking a student to a course, with a status of Pending or Active.

**Entity Framework (EF) Core** — the ORM used to talk to SQL Server via C# code instead of raw SQL.

**Foreign Key (FK)** — a database column that references another table's primary key, forming a relationship.

**Gemini** — Google's AI API, used to power the in-app course-recommendation chat.

**HashSet / SetEquals** — a no-duplicates collection type and its method for checking two sets contain exactly the same elements; the core of the quiz-grading logic.

**Identity** — see ASP.NET Core Identity.

**In-memory database** — a temporary, RAM-only version of the database used during automated tests.

**Instructor** — one of three user roles; applies (with admin approval) to build and teach courses.

**Integration test** — a test that exercises multiple real pieces of the app working together (e.g. a controller + a database), as opposed to one isolated function.

**jQuery / jQuery Validation** — JavaScript helper libraries; the latter hooks up client-side form validation.

**LINQ** — writing database (or in-memory collection) queries as C# code instead of SQL strings.

**Lesson Progress** — the record tracking whether a specific student has completed a specific lesson.

**MailKit** — the C# library used to actually send emails over SMTP.

**Migration** — a versioned, incremental description of a database schema change, applied with `dotnet ef database update`.

**Mocking** — creating a fake stand-in for a dependency during testing (via the `Moq` library here).

**Model** — a C# class representing a piece of data (the "M" in MVC); also refers to EF Core entity classes.

**MVC (Model-View-Controller)** — the architectural pattern the whole app follows.

**Notification** — an in-app message shown via the bell icon; created by `NotificationService.NotifyAsync`.

**ORM (Object-Relational Mapper)** — software translating between C# objects and database rows; EF Core here.

**PIN** — the one-time 6-digit code an Admin generates that gates the instructor application form.

**QuestPDF** — the C# library used to generate all PDFs (certificates, receipts, reports) in code.

**Quiz pass mark / time limit** — instructor-configured settings per quiz (`PassMarkPercentage`, `TimeLimitMinutes`) determining pass/fail and the countdown duration.

**Razor** — the templating syntax (`.cshtml` files) mixing C# and HTML for views.

**Restrict (delete behavior)** — blocks a delete if related rows exist, rather than cascading; used for anything pointing back to a user.

**Role** — a category of user (`Admin`, `Instructor`, `Student`) used to control access via `[Authorize(Roles = "...")]`.

**Seed / seeding** — creating initial required data (roles, the admin account) automatically on first startup.

**Service layer** — the `Services/` folder; classes handling business logic or external integrations independent of any one controller.

**Session** — small server-side data tied to a browser via a cookie, not permanently stored in the database; used for pending OTP registration, chat history, and a couple of in-flight states.

**SignInManager / UserManager** — ASP.NET Core Identity classes handling login/logout and user account operations respectively.

**SQL Server** — the relational database engine used to store all persistent data.

**Student** — one of three user roles; browses, enrolls, learns, and gets certified.

**TempData** — data passed from a controller to a view that survives exactly one redirect; used for post-action messages (errors/success banners → toasts).

**Unit test** — a test of one small, isolated piece of logic with no external dependencies (database, network, etc.).

**Upsert** — "update or insert": update an existing row if one matches, otherwise create a new one. Used for `QuizResult`, `AssignmentSubmission`, `LessonProgress`.

**ViewBag / ViewData** — ways of passing data from a controller to a view for the current request only (don't survive a redirect).

**ViewModel** — a C# class built specifically to shape data for one view, not necessarily matching a database table 1:1 (e.g. `InstructorApplicationViewModel`, `QuizCreateViewModel`).

**xUnit** — the testing framework used to write and run this project's automated tests.

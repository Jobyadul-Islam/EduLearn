# EduLearn — Project Overview

> **Purpose of this document:** a complete, self-contained handoff brief. A reader (human or AI) with zero prior context on this project should be able to read this file and understand what EduLearn is, how it's built, why key decisions were made, what currently works, and what to do next — without needing to dig through commit history or guess at intent.
>
> This document was produced by reading the entire source tree, every controller/model/service, the full EF Core migration history, `git log --all`, and the project's own `README.md`, as of the state described in "Current State" below. Where something in the code was genuinely ambiguous, it's flagged explicitly as **unclear/needs clarification** rather than guessed.

---

## 1. Project Goal & Purpose

**EduLearn** is a full-featured e-learning platform — the kind of product Udemy or Coursera represent, scoped down to a single-institution/single-vendor deployment. It was built as a university practicum project, developed against a 60-day logbook plan (referenced in commit messages as "Page 16," "Page 21," "Day 41," etc.), but the resulting codebase is a genuinely complete, working application rather than a toy.

**Who it's for:** three user roles —
- **Students** browse and enroll in courses (free or paid), consume lesson content, submit assignments, take quizzes, earn certificates, and leave reviews.
- **Instructors** apply to teach (subject to Admin approval), build courses (modules → lessons → assignments/quizzes), and track their students' engagement and results.
- **Admins** run the platform: approve/reject courses and instructor applications, manage all user accounts, and pull revenue/analytics reports.

**Problem it solves:** provides an end-to-end teach/learn/pay/certify loop without needing a third-party LMS — course creation and approval, real payment collection (bKash, a major Bangladeshi mobile payment gateway), progress tracking, assessment (assignments and auto-graded timed quizzes), and completion certificates are all handled natively.

**Scope:** this is a single-tenant web application (one deployment = one "school"), not a multi-tenant SaaS product. There is no concept of multiple organizations sharing one instance.

---

## 2. Tech Stack

| Layer | Choice | Why (inferred from usage) |
|---|---|---|
| Backend framework | **ASP.NET Core MVC, .NET 10** | Classic server-rendered MVC with Razor views throughout — no SPA framework, no separate API layer. Chosen for a practicum context where a single cohesive full-stack C# codebase is easier to build and reason about solo than a split frontend/backend. |
| Auth | **ASP.NET Core Identity** (`AddDefaultIdentity<ApplicationUser>` + `AddRoles<IdentityRole>`) | Standard, well-documented, integrates directly with EF Core. `RequireConfirmedAccount = false` because email verification is instead handled by a **custom OTP flow** (see §5) rather than Identity's built-in confirmation-link flow. |
| Database | **SQL Server** (via `Microsoft.EntityFrameworkCore.SqlServer`), Code-First EF Core migrations | Default connection string targets `localhost\SQLEXPRESS` — this is built and run against a local SQL Server Express instance, not a cloud-hosted DB. |
| ORM | **Entity Framework Core 10** | Code-First with an explicit `OnModelCreating` FK/cascade configuration (see §6) rather than relying on EF's conventions — the delete-behavior choices are deliberate and documented inline. |
| PDF generation | **QuestPDF** (Community license, set in `Program.cs`) | Used for certificates, payment receipts, and two admin reports. Fluent C# API — no external tools (wkhtmltopdf, etc.) needed. |
| Email | **MailKit** via a custom `SmtpEmailService` | Sends OTPs, password resets, enrollment confirmations, instructor-approval credentials, and deadline reminders. Gracefully no-ops (logs a warning, returns `false`) if SMTP credentials aren't configured — nothing in the app hard-fails without email. |
| Payments | **bKash Tokenized Checkout** (sandbox) | Bangladesh-specific mobile payment gateway. Integrated directly via raw `HttpClient` calls to bKash's REST API (`BkashPaymentService`) — no official .NET SDK exists for bKash, so this is a bespoke integration. |
| AI assistant | **Google Gemini API** (`gemini-3.6-flash` by default, configurable) | Powers an in-app chat widget that recommends courses to students based on a short conversation. Called via raw `HttpClient`, not a Google SDK package. |
| Frontend | **Razor views + Bootstrap 5 + vanilla JS + jQuery** | No client-side framework (React/Vue/etc.). All third-party JS/CSS (Bootstrap, jQuery, jQuery Validation + Unobtrusive, Chart.js) is **vendored locally** under `wwwroot/lib/` rather than pulled from a CDN — a deliberate choice so the app has zero internet dependency, useful for an offline practicum defense. |
| Charts | **Chart.js** (vendored, `wwwroot/lib/chartjs/chart.umd.min.js`) | Used on the Admin dashboard (registration trend) and Revenue report. |
| Testing | **xUnit + Moq + EF Core InMemory provider** (`EduLearn.Tests` project) | Added late in the project's life (see §7) to cover both pure business logic and full controller-level integration flows without needing a real database or web server. |
| Session/state | `AddDistributedMemoryCache` + `AddSession` | Used for short-lived, non-persistent state: the pending-registration OTP payload, the verified-PIN marker during instructor application, the chat conversation history, and the in-flight bKash `courseId` during the payment redirect round-trip. |

---

## 3. Architecture Overview

### High-level shape

This is a **traditional ASP.NET Core MVC monolith** — no microservices, no separate API/frontend split, no message queue. One process serves everything: HTML pages, form POST handlers, and the two JSON endpoints (`/Chat/SendMessage`, `/Notification/*`) that back small pieces of AJAX/form-based interactivity.

```
Browser
  │  (HTML forms, a little vanilla JS/fetch for chat + toasts)
  ▼
ASP.NET Core MVC pipeline (Program.cs)
  │  routing → [Authorize] filters → Controller action
  ▼
Controllers (business logic lives HERE, not in a separate layer)
  │  directly query/mutate via injected ApplicationDbContext
  │  delegate to Services/ for anything reusable or external (PDF, email, payment, AI, notifications)
  ▼
EF Core (ApplicationDbContext) ──▶ SQL Server
```

### Folder structure

```
Controllers/              Top-level MVC controllers (Course, Instructor, Apply, Bkash, Chat,
                           Notification, Category, Home)
Areas/Admin/               Admin dashboard — its own Controllers/ and Views/, routed via the
                           ASP.NET Core "Areas" feature ({area:exists}/{controller}/{action})
Areas/Identity/Pages/      ASP.NET Core Identity UI as Razor Pages (Register, Login, VerifyOtp,
                           ForgotPassword, ResetPassword, Logout) — NOT MVC controllers, these
                           are page-model (.cshtml.cs) code-behind classes
Models/                    EF Core entities (one class per table, roughly)
Models/ViewModels/         View-specific DTOs that don't map 1:1 to a table (e.g.
                           InstructorApplicationViewModel, QuizCreateViewModel)
Data/ApplicationDbContext.cs   The single EF Core DbContext; all FK/cascade rules live in
                           its OnModelCreating override
Services/                  Business logic that isn't tied to a single controller/request:
                           PDF generation (stateless static classes), email, bKash, Gemini
                           chat, notifications, quiz grading, course-progress calculation,
                           background deadline reminders
Views/                     Razor views, one folder per controller, plus Views/Shared/ for
                           _Layout.cshtml, the toast partial, validation scripts partial
Migrations/                EF Core Code-First migrations, chronological (14 total as of
                           this writing)
wwwroot/                   Static assets; wwwroot/lib/ holds vendored Bootstrap/jQuery/
                           Chart.js; wwwroot/uploads/ holds all user-uploaded files
                           (thumbnails, lesson attachments, assignment submissions, CVs)
EduLearn.Tests/             xUnit test project — Services/ (pure unit tests) and
                           Integration/ (controller-level tests against EF Core InMemory)
```

### Request/data flow example

A student submitting a quiz (`POST /Course/SubmitQuiz`):
1. Browser POSTs `quizId` + a repeated `selectedOptionIds` form field (one per checked checkbox) to `CourseController.SubmitQuiz`.
2. The action loads the `Quiz` (with its `Questions`→`Options` graph) via `ApplicationDbContext`.
3. Grading itself is delegated to `Services/QuizGrader.Grade(quiz, selectedOptionIds)` — a pure, stateless static method (see §5) that returns a `QuizGradeResult { Score, TotalQuestions, Passed }`.
4. The controller upserts a `QuizResult` row (update in place if the student already attempted this quiz, insert otherwise) and calls `_context.SaveChanges()`.
5. Redirects to `GET /Course/QuizResult?quizId=X`, which re-reads the freshly-saved result and renders it.

No API layer, no DTOs crossing an HTTP boundary internally — the controller talks straight to EF Core and to the Services layer in-process.

### Design patterns in use

- **MVC**, straightforwardly — Razor views bound to controller actions, `ViewBag`/`ViewData`/`TempData` used liberally for view-only data rather than always building dedicated ViewModels (a pragmatic, not strict, MVC style).
- **No repository/unit-of-work abstraction** — controllers inject `ApplicationDbContext` directly and query it inline. This is a deliberate simplicity choice appropriate to the project's scale, not an oversight; there is no interface between controllers and EF Core.
- **Thin service layer for cross-cutting/external concerns** — `IEmailService`, `IBkashPaymentService`, `IChatService`, `INotificationService`, `IDeadlineReminderService` are all interface + implementation pairs registered in DI (`Program.cs`), each wrapping one integration or one piece of reusable logic. Two services (`QuizGrader`, `CourseProgressCalculator`, `CertificateService`, `InvoiceService`, `ReportPdfService`) are **stateless static classes** rather than DI services, because they have no dependencies and were specifically extracted to be trivially unit-testable (see §7, the testing-focused commits).
- **Background service** — `DeadlineReminderBackgroundService : BackgroundService` runs a self-contained loop (immediate check on startup, then every 6 hours) that resolves a scoped `IDeadlineReminderService` per iteration via `IServiceScopeFactory`, since a singleton hosted service can't directly hold a scoped `DbContext`.
- **Upsert pattern** used consistently for "one row per (user, thing)" data: `AssignmentSubmission`, `QuizResult`, and `LessonProgress` all follow the same shape — look up an existing row for this user+target, update it if found, insert if not. This means resubmitting an assignment or retaking a quiz never creates duplicate rows; the latest attempt simply overwrites the previous one.

---

## 4. Features Implemented

### Public / unauthenticated
- **Home page** (`Controllers/HomeController.cs`) — platform stats (total courses/categories/students/instructors), top categories by course count, 6 featured courses with ratings.
- **Course browsing** (`CourseController.Index`) — search by title, filter by category, sort by Newest/Most Popular/Highest Rated, paginated (4 per page via `CoursesPerPage`).
- **Course details** (`CourseController.Details`) — full description, module/lesson list (locked/unlocked per free-preview rules), reviews, average rating, enroll/checkout CTA.
- **Registration with email OTP** (`Areas/Identity/Pages/Account/Register.cshtml.cs`, `VerifyOtp.cshtml.cs`) — see §5 for how this differs from Identity's default flow.
- **Login / Logout / Forgot Password / Reset Password** — standard Identity pages, customized (deactivated-account check on login; see §5 for a historical reset-token bug that was fixed).
- **Instructor application** (`Controllers/ApplyController.cs`, `Views/Apply/*`) — PIN entry → application form (with required CV upload) → confirmation. Covered in depth in §5.

### Student
- **Enroll** (free instantly-active, paid starts `Pending` until payment) — `CourseController.Enroll`.
- **Checkout & payment** — `CourseController.Checkout` (view) + `Controllers/BkashController.cs` (the full bKash Tokenized Checkout round-trip: grant token → create agreement → execute agreement → create payment → execute payment). Every attempt (success or failure) is recorded as a `Payment` row.
- **My Enrollments dashboard** (`CourseController.MyEnrollments`) — per-course progress %, next-lesson quick link, upcoming assignment deadlines (next 5), certificate/review CTAs once eligible.
- **Order History + PDF Receipts** (`CourseController.OrderHistory`, `CourseController.Receipt` → `Services/InvoiceService.cs`).
- **Lesson viewing** (`CourseController.ViewLesson`) — gated by enrollment + (full access OR one of the first 2 lessons, which are always free-preview). Shows video/file attachment, assignments, and quizzes for that lesson.
- **Mark lesson complete** (`CourseController.MarkComplete`) — drives progress %, certificate eligibility, and review eligibility.
- **Assignment submission** (`CourseController.SubmitAssignment`) — file upload to `wwwroot/uploads/submissions/`; resubmitting overwrites the previous submission.
- **Quiz taking** (`CourseController.TakeQuiz` / `SubmitQuiz` / `QuizResult`, `Views/Course/TakeQuiz.cshtml`) — timed (instructor-configured minutes), auto-submits via client-side JS countdown when time runs out, auto-graded (supports multi-correct-answer questions — see §5), pass/fail computed against an instructor-configured pass mark. **Quiz results never affect course completion or certificate eligibility** — this was an explicit product requirement (see §7, the pass-mark commit and the notification-wiring conversation that led to this doc).
- **Certificate download** (`CourseController.Certificate` → `Services/CertificateService.cs`) — unlocked once every lesson in the course is marked complete; a landscape A4 PDF with student name, course title, and completion date.
- **Reviews** (`CourseController.WriteReview` GET/POST) — one review per student per course, only after 100% lesson completion; star rating (1–5) + optional comment.
- **AI course-recommendation chat** (`Controllers/ChatController.cs`, floating widget in `Views/Shared/_Layout.cshtml`) — session-scoped conversation (capped at 40 turns), system prompt built per-request from the live course catalog and the student's existing enrollments so it never invents courses or prices.
- **In-app notifications** — bell icon with unread badge, visible to every authenticated role (see §5 for exactly which events trigger one).

### Instructor
- **Apply with CV upload**, admin-set password at approval (§5).
- **Dashboard** (`InstructorController.Index`) — lists only their own courses; shows an approval-pending state if `IsApproved == false`.
- **Course authoring**: `CreateCourse`/`EditCourse`/`DeleteCourse`, `CreateModule`, `CreateLesson` (with file upload), `CreateAssignment`, `CreateQuiz` (with pass mark % and time limit, dynamic add-question JS on the create form). **Note:** there is no Edit action for Module/Lesson/Assignment/Quiz once created — only Create and (for Course) Delete. Editing a course kicks it back to `Pending` for re-approval.
- **Course code generation** — `InstructorController.GenerateCourseCode()` produces a human-readable `XX-1234` code with a DB-uniqueness retry loop (10 attempts, then a GUID-fragment fallback).
- **Reviews dashboard** (`InstructorController.Reviews`) — per-course average rating (courses under 3.5★ visually flagged), filterable by course, full review list.
- **Quiz results dashboard** (`InstructorController.QuizResults`) — every student attempt across their courses, with Pass/Fail, filterable by course. Confirmed (via integration test and live testing) to correctly isolate one instructor's data from another's.
- **Notifications** — new enrollment, course approved, course rejected (with reason if given).

### Admin
- **Dashboard** (`AdminController.Index`) — total users/courses/enrollments, pending-approval count, student/instructor counts, a 6-month registration trend (zero-filled for months with no signups).
- **Course moderation** — `PendingCourses`, `ApproveCourse`, `RejectCourse` (optional reason).
- **All Courses / All Enrollments** — full unfiltered listings (added specifically so every Admin dashboard stat card is clickable through to detail).
- **User management** (`Users`) — search by email, filter by role/status, activate/deactivate (self-deactivation blocked), approve/reject instructor applications (approval requires the admin to set a login password, which is emailed to the instructor), view a full application (`ViewApplication`, including a CV download link).
- **Instructor PINs** (`InstructorPins`, `GeneratePin`) — 6-digit codes, collision-checked against unused codes, tied to the generating admin.
- **Revenue report** (`Revenue` + `ExportRevenuePdf`) — total revenue + 6-month trend, on-screen with a Chart.js graph and as a downloadable PDF.
- **Course analytics** (`Analytics` + `ExportAnalyticsPdf`) — Most Popular (by enrollment count) and Top Rated (by average review rating, courses with zero reviews excluded) — top 10 each, on-screen and as PDF.

### Cross-cutting
- **Accessibility** — a dedicated audit pass (git commit `0c11462`, "Day 52") added `alt` text, `<label for>` associations across every form, ARIA labels on icon-only buttons, `aria-hidden` on decorative icons, and fixed a real keyboard-trap bug in the star-rating widget (see §5).
- **Client-side validation, loading spinners, toast notifications** (`c4c6cb7`, "Day 51") — jQuery Validate Unobtrusive activated app-wide, a generic form-submit spinner, and a TempData→toast pattern replacing static Bootstrap alerts.
- **Responsive layout fixes** (`968c15f`, "Day 49") — table overflow, notification dropdown width, navbar email truncation on small screens.

---

## 5. Approaches & Methods Used

### Authentication & Authorization
- ASP.NET Core Identity with three roles seeded at startup (`Program.cs`): `Admin`, `Instructor`, `Student`.
- **Custom email-OTP registration flow**, not Identity's built-in confirmation link: `Register.cshtml.cs` generates a 6-digit code, stores a `PendingRegistration` (name/email/password/otp/expiry) serialized into session (`HttpContext.Session`, NOT the database), and emails the code. `VerifyOtp.cshtml.cs` validates it, THEN actually creates the Identity user and signs them in. This means an unverified registration leaves no trace in the database at all — if the OTP is never entered, nothing was ever persisted.
- **Instructor onboarding is a separate, gated pipeline**, not self-service registration: Admin generates a one-time PIN (`AdminController.GeneratePin`) → applicant enters it (`ApplyController.VerifyPin`, stored in session) → fills out a form including a required CV upload → an `ApplicationUser` is created immediately but with `IsApproved = false` and a **random password the applicant never sees** (`ApplyController.GenerateRandomPassword()` — purely to satisfy `CreateAsync`'s non-null requirement). The account is unusable until an Admin approves it and **chooses the real password themselves**, which is then emailed to the instructor (`AdminController.Approve`).
- **Deactivation check happens at login**, not via Identity's lockout system: `Login.cshtml.cs` explicitly checks `existingUser.IsActive` before attempting sign-in and shows a custom error if false.
- Role-gating is straightforward `[Authorize(Roles = "...")]` at the controller or area level (`AdminController` is `[Area("Admin")][Authorize(Roles = "Admin")]`; `InstructorController` is class-level `[Authorize(Roles = "Instructor")]`; `CourseController` mixes `[Authorize]`, `[Authorize(Roles = "Student")]`, and unauthenticated actions per-method).
- **Historical bug, fixed**: `ForgotPassword.cshtml.cs` used to manually URL-encode the reset token before passing it to `Url.Page(...)`, which *also* URL-encodes its route values — causing a double-encoded, invalid token. Fixed by removing the manual encode (git commit `c9595eb`).

### Validation strategy
- Primarily server-side: `ModelState.IsValid` checks with `[Required]`/`[EmailAddress]`/`[Range]` DataAnnotations on ViewModels (e.g. `InstructorApplicationViewModel`).
- Client-side validation was added later (`c4c6cb7`) by simply referencing the pre-existing-but-unused `_ValidationScriptsPartial.cshtml` from `_Layout.cshtml` — no new wiring was needed because ASP.NET Core's `asp-for` tag helpers already emit `data-val-*` attributes from the DataAnnotations; jQuery Validate Unobtrusive just needed to be loaded to pick them up.

### Error handling strategy
- No global try/catch or custom exception middleware beyond the framework default (`app.UseExceptionHandler("/Home/Error")` in non-Development environments, plus `UseHsts`).
- Expected failure paths (payment declined, review blocked because course incomplete, invalid PIN, etc.) are handled with explicit guard clauses and **TempData-driven user-facing messages** (`TempData["PaymentError"]`, `TempData["ReviewError"]`, etc.), rendered as toast notifications — not exceptions.
- Not-found cases are consistent `if (x == null) return NotFound();` guards throughout every controller.
- External integrations (bKash, Gemini, SMTP) each degrade gracefully rather than throwing: `IsConfigured` properties gate each service, and callers check it (or the service itself returns a friendly fallback string/`false`) rather than letting a missing API key crash a request.

### Quiz grading algorithm (`Services/QuizGrader.cs`)
A question is scored correct only if the set of options the student selected **exactly equals** the set of options marked `IsCorrect` — implemented via `HashSet<int>.SetEquals`. This means:
- A multi-correct-answer question requires selecting *all* correct options and *no* incorrect ones to get credit — partial selection earns nothing (verified by unit test `Grade_PartialMultiSelectAnswer_CountsQuestionAsWrong`).
- `Passed` is computed as `(100.0 * score / totalQuestions) >= quiz.PassMarkPercentage`, with the boundary being inclusive (scoring exactly the pass mark counts as a pass).
- A zero-question quiz can never be marked passed (guarded explicitly, not just falling out of the math).
- This logic was **deliberately extracted from `CourseController.SubmitQuiz` into a separate stateless static class** specifically so it could be unit-tested without needing a database or web request context — see §7.

### Course completion calculation (`Services/CourseProgressCalculator.cs`)
A single `IsComplete(totalLessons, completedLessons)` pure function is shared by both certificate eligibility (`CourseController.Certificate`) and review eligibility (`CourseController.HasCompletedCourse`), so the two can never silently drift apart. A course with zero lessons can never be "complete." **Quiz results are deliberately NOT a factor anywhere in this calculation** — a student can fail (or never take) every quiz in a course and still earn the certificate, as long as every *lesson* is marked complete. This was an explicit, stated product requirement, not an oversight.

### EF Core delete-behavior policy
Consistently applied across `ApplicationDbContext.OnModelCreating` (with an inline comment explaining it): **any foreign key from a record back to `ApplicationUser` uses `DeleteBehavior.Restrict`**, never `Cascade` — this avoids SQL Server's "multiple cascade paths" error and, more importantly, guarantees that deleting a user account can never silently wipe unrelated progress/grade data through some other cascade path. By contrast, the **course-content hierarchy cascades fully**: deleting a `Course` cascades to `Module` → `Lesson` → `Assignment`/`Quiz` → `QuizQuestion` → `QuizOption`, and deleting a `Quiz` cascades to its `QuizResult` rows.

### EF Core anonymous-type / tuple limitation (recurring, solved pattern)
EF Core cannot translate a tuple literal into a SQL expression tree. The established workaround, used identically in `AdminController.GetAnalyticsData()` and `InstructorController.Reviews`/`QuizResults`: project to an **anonymous type** first (which EF *can* translate to SQL), materialize with `.ToList()`, then convert to named tuples or keep as anonymous types for the view in a second, in-memory `.Select()`. The code comment in `AdminController.cs` states this explicitly: *"EF Core can't put a tuple literal inside a SQL expression tree, so the ranking/limiting happens in SQL via an anonymous type, then the small materialized result is converted to named tuples."*

### Testing an anonymous-type-returning action from a separate test assembly
C# anonymous types are compiler-generated `internal` classes. `InstructorController.QuizResults` returns `IEnumerable<dynamic>` backed by an anonymous type; reading its properties via `dynamic` from `EduLearn.Tests` (a different assembly) throws `RuntimeBinderException` at runtime unless visibility is explicitly granted. Fixed with `[assembly: InternalsVisibleTo("EduLearn.Tests")]` in `Properties/AssemblyInfo.cs`.

### Mocking `UserManager<ApplicationUser>` for controller-level tests
`UserManager<TUser>` has a 9-parameter constructor and no interface, but its members (`GetUserId`, `GetUserAsync`, `FindByIdAsync`, etc.) are `virtual`. `EduLearn.Tests/Integration/TestHelpers.CreateMockUserManager` builds a `Mock<UserManager<ApplicationUser>>` by passing a mocked `IUserStore<ApplicationUser>` and nulls (`!`-suppressed) for the rest, then sets up only the specific virtual members each test needs.

### bKash integration specifics (real, previously-broken, now fixed)
- The `intent` field in the Create Payment request **must be lowercase `"sale"`** — bKash's sandbox rejected `"Sale"` with a generic "Invalid intent" error that gave no hint about casing.
- `payerReference` is required on **both** the Create Agreement (mode `0000`) call and the Create Payment (mode `0011`) call — it was originally only being sent on the former.
- Both bugs were found by isolating variables via direct curl calls against the real bKash sandbox API rather than only testing through the full app flow (git commit `9a08353`).
- The full flow is a 5-call round-trip: Grant Token → Create Agreement → (browser redirect to bKash, then back) → Execute Agreement → Create Payment → (browser redirect to bKash, then back) → Execute Payment. `BkashController` orchestrates this across two callback actions (`AgreementCallback`, `PaymentCallback`), carrying the in-flight `courseId` in session between redirects.

### Notification system
A deliberately simple, non-cached, non-real-time design: `Views/Shared/_Layout.cshtml` runs a live LINQ query against `ApplicationDbContext` inline (`Notifications.Where(n => n.UserId == currentUserId).OrderByDescending(...).Take(8)`) on every page load for an authenticated user — the same pattern already used for the Admin pending-course badge. Mark-read/mark-all-read are plain POST-and-redirect actions (`Controllers/NotificationController.cs`), not AJAX. `Services/NotificationService.NotifyAsync(userId, message, link)` is the single write path, called from exactly four places: new instructor application (`ApplyController`, notifies all Admins), course approved/rejected (`AdminController`, notifies the instructor), instructor account approved (`AdminController`, notifies that instructor), and new enrollment (`CourseController.Enroll`, notifies the course's instructor). **Deliberately not wired:** notifying a *rejected* instructor applicant — their account is set `IsActive = false` on rejection, and `Login.cshtml.cs` blocks deactivated accounts at login, so they could never log in to see such a notification; wiring it would be unreachable dead code.

### Keyboard-accessibility bug (found and fixed, Day 52)
The star-rating widget (`Views/Course/WriteReview.cshtml`) is 5 reversed `<input type="radio">` + `<label>` pairs styled with a `~` sibling selector for hover/checked states. The radios were hidden with `display: none`, which **removes an element from the tab order and the accessibility tree entirely** — a keyboard-only user could never reach or operate the rating control. Fixed by switching to `position: absolute; opacity: 0` (invisible but still focusable), and a `:focus-visible` outline was added on the label so keyboard focus is visibly indicated.

---

## 6. Database Schema

SQL Server, EF Core Code-First. `ApplicationUser` extends Identity's `IdentityUser` (adds `FullName`, `ProfilePicture`, `IsApproved`, `IsActive`, `CreatedAt`, plus instructor-application-only fields: `Qualification`, `Institution`, `Skills`, `YearsOfExperience`, `Bio`, `ResumePath`).

| Entity | Key fields | Relationships / delete behavior |
|---|---|---|
| **Category** | Name, Description | 1→many Courses (`Restrict` — can't delete a category with courses) |
| **Course** | Title, Description, CourseCode (e.g. `CS-4821`), Price, ThumbnailPath, Status (`Pending`/`Approved`/`Rejected`), RejectionReason | →Category (Restrict), →Instructor/`ApplicationUser` (Restrict); 1→many Modules/Enrollments/Reviews (Cascade) |
| **Module** | Title | →Course (Cascade); 1→many Lessons (Cascade) |
| **Lesson** | Title, Content, VideoUrl?, FilePath? | →Module (Cascade); 1→many Assignments, Quizzes (Cascade) |
| **Assignment** | Title, Description, DueDate | →Lesson (Cascade) |
| **AssignmentSubmission** | FilePath, SubmittedDate | →Assignment (Cascade), →Student (Restrict). One row per (Assignment, Student) — upserted |
| **AssignmentReminder** | SentAt | →Assignment (Cascade), →Student (Restrict). Marker row preventing duplicate reminder emails |
| **Quiz** | Title, **PassMarkPercentage** (int, default 60), **TimeLimitMinutes** (int, default 10) | →Lesson (Cascade); 1→many Questions (Cascade) |
| **QuizQuestion** | QuestionText | →Quiz (Cascade); 1→many Options (Cascade) |
| **QuizOption** | OptionText, IsCorrect (bool) | →QuizQuestion (Cascade) |
| **QuizResult** | Score, TotalQuestions, **Passed** (bool), AttemptDate | →Quiz (Cascade), →Student (Restrict). One row per (Quiz, Student) — upserted on retake |
| **Enrollment** | Status (`Pending`/`Active` enum, `EnrollmentStatus`), EnrollDate, PaymentDate? | →Course (Cascade), →Student (Restrict) |
| **LessonProgress** | IsCompleted, CompletedAt? | →Lesson (Cascade), →Student (Restrict). One row per (Lesson, Student) |
| **Payment** | Amount, TransactionId, Status (`Success`/`Failed` enum, `PaymentStatus`), CreatedAt | →Course (Cascade), →Student (Restrict). One row per payment *attempt* (failures are recorded too) |
| **Review** | Rating (1–5), Comment?, CreatedAt | →Course (Cascade), →Student (Restrict). One row per (Course, Student) |
| **Notification** | Message, Link?, IsRead, CreatedAt | →User/`ApplicationUser` (Restrict) |
| **InstructorApplicationPin** | Code (6-digit), IsUsed, CreatedAt, UsedAt? | →GeneratedByAdmin/`ApplicationUser` (Restrict) |

**Migration history** (14 migrations, chronological): `InitialCreate` → `AddCourseCodeAndInstructorNav` → `AddQuizResultAndUserApprovalFields` → `AddCourseApprovalWorkflow` → `AddEnrollmentPayment` → `AddInstructorApplicationSystem` → `AddLessonProgressCompletedAt` → `AddPayments` → `AddEnrollmentStatus` → `AddAssignmentReminders` → `AddReviews` → `AddUserCreatedAt` → `AddApplicationUserResumePath` → `AddQuizPassMarkAndTimeLimit`.

---

## 7. Chronological Development Summary

Reconstructed from `git log --all` (56 commits, 2026-08-04 through 2026-08-26). The project followed a 60-day logbook plan; commit messages through mid-project reference specific "Page N" numbers from that plan, later switching to "Day N."

**Phase 1 — Foundation (Aug 4):** Initial project scaffold, EF Core models, `ApplicationDbContext`, Identity setup, and role seeding. Two commits.

**Phase 2 — Core CRUD (Aug 9):** Role-based authorization; Admin dashboard + Category CRUD; dashboard statistics; Instructor dashboard + Course CRUD with thumbnail upload; nested Module/Lesson creation with file upload; Assignment creation; Quiz creation with dynamic questions/options. This established the entire content-authoring backbone in one day of logbook pages (16–20).

**Phase 3 — Student consumption (Aug 10–11):** Public course browsing, an enrolled-courses page, lesson viewing restricted to enrolled students, assignment submission with file upload, lesson progress tracking, and duplicate-submission prevention (Pages 21–25).

**Phase 4 — Stabilization & redesign (Aug 16):** A cleanup commit ("Close remaining gaps to genuinely complete logbook pages through Page 25") followed by a large one: course approval workflow, a full UI/UX redesign, and a lesson paywall — i.e., the point where courses stopped being instantly live and started requiring Admin approval, and where free-preview-vs-paid access control was introduced. A small nav-link fix followed two days later.

**Phase 5 — Instructor pipeline, AI, real email (Aug 21):** The PIN-based instructor application system, the Gemini-powered course-recommendation chatbot, and real SMTP email delivery all landed together. Two same-day follow-ups: hiding the Apply/Contact section from Admin and Instructor accounts, and fixing a self-deactivation UI bug plus restricting "Send Login Email" to pending instructors.

**Phase 6 — Payments, certificates, engagement (Aug 24):** A dense day — PDF certificate generation on 100% completion; upcoming-deadlines and next-lesson widgets on the student dashboard; persisted `Payment` records with a simulated (pre-bKash) checkout flow; replacing a boolean `Enrollment.IsPaid` with the proper `Pending`/`Active` state machine; order history with downloadable PDF receipts; enrollment-confirmation email; a background deadline-reminder service; category filtering combined with search; Newest/Most-Popular sort; pagination; and making email OTP verification mandatory for student registration.

**Phase 7 — Real payments & admin polish (Aug 25, early):** The simulated checkout was replaced with a real bKash Tokenized Checkout sandbox integration. Same day: clickable Admin dashboard stat cards (adding the All Courses / All Enrollments pages), admin-set instructor passwords at approval time, switching displayed currency from `$` to `TK` everywhere, removing sandbox-disclosure copy from the checkout page, and — critically — fixing the real "Invalid intent" bKash bug (lowercase `intent`, missing `payerReference`).

**Phase 8 — Reviews & analytics ("Day 41" arc, Aug 25):** Starting from an explicit user instruction ("let's start working from day 41"), a self-contained arc built: the `Review` model and course relationship; restricting review submission to students who completed the course; average-rating + full review-list display on course pages; an instructor reviews dashboard with per-course filtering and low-rating highlights; a monthly-registration-trend chart plus student/instructor counts on the Admin dashboard; a revenue report with Chart.js; Most-Popular/Top-Rated course analytics; PDF export for both reports; and a responsive-layout bug-fix pass (table overflow, notification dropdown width, navbar truncation) — "Day 49."

**Phase 9 — Bug fixes, instructor overhaul, accessibility (Aug 25–26):** A double-encoded password-reset-token bug was found and fixed (alongside repairing an `appsettings.json` file that had been externally corrupted mid-session), immediately followed by removing the applicant-set password from instructor applications and adding required CV upload. Then three sequential polish days: centralizing star-rating colors into CSS variables ("Day 50"); activating client-side validation, loading spinners, and toast notifications app-wide ("Day 51"); and a full accessibility audit — alt text, label associations, ARIA labels, and the keyboard-accessible star-rating fix ("Day 52").

**Phase 10 — Quiz-taking, testing infrastructure, notifications (Aug 26, this session's later work):** The quiz *creation* feature had existed since Phase 2, but there was no way for a student to actually take one — this phase closed that gap: student-facing quiz-taking with auto-grading and an instructor quiz-results view; then, per explicit request, a per-quiz pass mark, a live countdown timer with auto-submit-at-zero, and pass/fail tracking that's deliberately isolated from course-completion/certificate logic. This was followed by setting up the `EduLearn.Tests` xUnit project from scratch: first unit tests (quiz grading, course-completion logic, PDF generation), then integration tests exercising real controller code against an EF Core InMemory database. A subsequent "check everything" verification pass (live-driving the whole app as Student/Instructor/Admin via HTTP, not just reading code) surfaced one real, previously-unnoticed gap — the notification system existed but only one of its four intended triggers actually fired — which was then fixed and covered by both new tests and live re-verification. The phase closed with a `README.md` (setup/run instructions) and this document.

---

## 8. Current State

### Fully working (verified, not assumed)
Every feature listed in §4 has been exercised end-to-end at least once this development cycle — either via the automated test suite or via live HTTP-driven testing against the running app with real (throwaway) accounts, with database state checked directly via SQL afterward. This includes the full student journey (register → enroll → lesson → quiz with timer/pass-mark → certificate → review), the full instructor journey (apply with CV → admin approval → course/module/lesson/assignment/quiz creation), the full admin journey (approvals, PDF report exports, user management, PIN generation), and the notification system's four trigger points.

The automated test suite (`EduLearn.Tests`) has **31 passing tests** — unit tests for `QuizGrader`, `CourseProgressCalculator`, and PDF generation (including the empty-data edge cases), plus integration tests for the enrollment→certificate flow, quiz submission/retake/instructor-visibility, admin course approval/rejection, review-submission gating, and notification delivery.

### Known limitations / deliberate gaps
- **No production deployment exists.** The app has only ever run against a local SQL Server Express instance (`localhost\SQLEXPRESS`). There is no CI/CD pipeline, no hosting configuration, no production `appsettings`, and no domain. This was explicitly descoped by the project owner in favor of calling the application itself "done."
- **No formal manual/visual QA pass** was completed. Automated tests verify logic and database state, not rendered appearance, responsive behavior under real viewport sizes, or actual screen-reader output — the accessibility work (§4/§5) was implemented and spot-checked via rendered HTML inspection, not a live screen reader session.
- **Quizzes cannot be edited after creation** — `InstructorController` has `CreateQuiz` but no `EditQuiz`. To change a quiz's questions, pass mark, or time limit, an instructor would currently have to delete the whole course (there's no delete-quiz-only path either) and rebuild it.
- **No export (PDF/CSV) for the Users, All Courses, or All Enrollments admin lists** — only Revenue and Analytics have a PDF export button; the rest are browse-only.
- **A rejected instructor applicant is never notified** — deliberate, not a bug (see §5).
- **No `.sln` solution file** — the repository has two independently-buildable `.csproj` files (`EduLearn.csproj`, `EduLearn.Tests/EduLearn.Tests.csproj`) with a project reference between them, but nothing ties them into a Visual Studio solution. `dotnet build`/`dotnet test` work fine without one.
- **`<NuGetAudit>false</NuGetAudit>`** is set in `EduLearn.csproj`, suppressing NuGet vulnerability-advisory warnings for the main app at build time. The test project (which doesn't have this suppression) surfaces several `NU190x` advisories inherited transitively through `MailKit`/`MimeKit` and test tooling — none currently addressed. **Unclear/needs clarification:** whether this was a deliberate suppression or incidental; worth revisiting before any real deployment.
- **The Gemini model name** (`gemini-3.6-flash`, configurable via `Gemini:Model`) is taken as given from the config default — **unclear/needs clarification:** this document cannot verify against Google's live model catalog whether this is a currently-valid model identifier.
- Payment and email integrations degrade gracefully but are **sandbox/unconfigured by default** — `appsettings.json` ships empty placeholders for `Bkash`, `Email`, and `Gemini`; real functionality requires a local, gitignored `appsettings.Local.json` with real credentials (see §9).

### No known open bugs
As of this writing there are no known reproducible bugs in the implemented feature set — the bugs discovered during development (bKash intent/payerReference casing, the double-encoded reset token, the notification-wiring gap, the keyboard-trap in the star widget, an empty-data PDF rendering bug in the analytics report) were all found and fixed within this same development history, each with a regression test or live re-verification.

---

## 9. How to Run / Deploy

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express is sufficient) — reachable at `localhost\SQLEXPRESS` with the default connection string, or override it (see below)

### Setup
1. Clone the repo.
2. Create `appsettings.Local.json` at the repository root (already gitignored — merged on top of `appsettings.json` at startup via `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, ...)` in `Program.cs`). Populate whichever of these you need:
   ```json
   {
     "ConnectionStrings": { "DefaultConnection": "..." },
     "AdminSeed": { "Email": "you@example.com", "Password": "..." },
     "Email": { "SmtpHost": "smtp.gmail.com", "SmtpPort": 587, "Username": "...", "Password": "...", "FromName": "EduLearn" },
     "Bkash": { "BaseUrl": "...", "Username": "...", "Password": "...", "AppKey": "...", "AppSecret": "..." },
     "Gemini": { "ApiKey": "..." }
   }
   ```
   The app runs without `Bkash`/`Email`/`Gemini` populated — those features just degrade gracefully (see §5/§8).
3. Apply migrations: `dotnet ef database update`
4. Run: `dotnet run` — the seeded Admin account (from `AdminSeed`, or the hardcoded fallback `admin@edulearn.com` / `ChangeMe123!` if unset) is created automatically on first run if it doesn't already exist (`Program.cs`, lines ~80–97).

### Running tests
```
cd EduLearn.Tests
dotnet test
```
No database or running server required — everything uses `Microsoft.EntityFrameworkCore.InMemory` and mocked dependencies.

### Deployment
**Not currently set up.** There is no Dockerfile, no CI/CD workflow file, no cloud hosting configuration, and no production connection string. To deploy this, a future session would need to: choose a host (Azure App Service, a VPS, etc.), provision a real SQL Server instance, set real environment-variable-backed configuration for all the secrets currently only in `appsettings.Local.json`, and decide on a static-file/uploads storage strategy (currently `wwwroot/uploads/` is local disk, which won't survive typical PaaS redeploys or scale across multiple instances).

---

## 10. Next Steps / Roadmap

Nothing in the code itself contains `// TODO` markers or commented-out incomplete code paths — the gaps below are inferred from §8's "known limitations" plus the explicit scope boundary the project owner set (deployment was consciously descoped, not left half-done).

1. **Deployment** (§8, §9) — the single biggest remaining gap. Needs a hosting decision before anything else here can proceed.
2. **Edit Quiz** — instructors can create but not modify a quiz; adding this would follow the exact same pattern already established for `EditCourse`.
3. **Formal manual/visual QA pass** — a structured walkthrough of every flow in a real browser (layout, responsive breakpoints, actual screen-reader output), since automated tests only cover logic/data, not rendering.
4. **Export for Users/Courses/Enrollments admin lists** — Revenue and Analytics already have a `ReportPdfService`-based PDF export; the same pattern could be extended to the other three admin listing pages if needed.
5. **Address the suppressed NuGet audit** (`NuGetAudit=false`) and the test project's inherited `MailKit`/`MimeKit` advisories before any production deployment.
6. **Uploads storage strategy for production** — `wwwroot/uploads/` on local disk works for a single-instance local dev setup but will not survive a typical cloud redeploy; would need to move to blob storage (Azure Blob, S3, etc.) if/when deployed.

No other partially-implemented features were found — every controller action reachable from a menu/link has a corresponding, working view and was exercised during development.

# EduLearn — Page-by-Page File Guide (Defense Prep)

This file exists so you can open any page in the running website, find it in this list, and know **exactly which file to open** to change what you see (frontend) or how it behaves (backend) — then reload the page and watch it change.

## How to read every entry

Every page in this app is built from **two files working together**:

- 🎨 **Frontend / View file** (ends in `.cshtml`) — this is what you edit to change **text, layout, colors, what buttons appear**. It's HTML with a bit of C# mixed in (anything starting with `@`).
- ⚙️ **Backend / Controller file** (ends in `.cs`) — this is what you edit to change **what data is fetched, what rules are enforced, what happens when a button is clicked**. No HTML here, just C# logic.

**The pattern:** a URL like `/Course/Details/5` means: open `Controllers/CourseController.cs`, find the method `Details`, that's the backend for that page. It hands data to a view file at `Views/Course/Details.cshtml`, that's the frontend.

**Admin pages are different:** they live in a separate "Area." A URL like `/Admin/Admin/Users` means: open `Areas/Admin/Controllers/AdminController.cs`, method `Users`, frontend at `Areas/Admin/Views/Admin/Users.cshtml`.

**Every single page** also uses one shared file for the navbar, footer, notification bell, and chat bubble:
- 🎨 `Views/Shared/_Layout.cshtml` — edit this and it changes on *every page at once*.
- 🎨 `wwwroot/css/site.css` — all the colors/spacing/fonts, site-wide.
- 🎨 `wwwroot/js/site.js` — small interactive bits (loading spinners, the chat widget, the profile picture cropper).

Try this to prove it to yourself: open `Views/Shared/_Layout.cshtml`, change the word "EduLearn" in the navbar brand to anything else, save, refresh any page — it changes everywhere.

---

## 1. Public Pages (no login needed)

### Home page — `/`
- 🎨 `Views/Home/Index.cshtml`
- ⚙️ `Controllers/HomeController.cs` → `Index()`
- What it does: pulls total course/student/instructor counts and the "Top Categories" list from the database, shows featured courses.

### Course catalog (browse all courses) — `/Course`
- 🎨 `Views/Course/Index.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `Index()`
- What it does: search, category filter, sort (newest/popular/rating), pagination — all computed here before being handed to the view.

### Single course page — `/Course/Details/{id}`
- 🎨 `Views/Course/Details.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `Details()`
- What it does: decides whether you (the viewer) get full access, free-preview-only, or a locked view, based on login/role/enrollment/payment status.

### Apply as Instructor — `/Apply`
A 5-step flow, each step is its own page:
| Step | URL | Frontend | Backend method |
|---|---|---|---|
| 1. Choose Google/Email | `/Apply` | `Views/Apply/Index.cshtml` | `ApplyController.Index()` |
| 2. "Request received" | `/Apply/RequestReceived` | `Views/Apply/RequestReceived.cshtml` | `ApplyController.RequestReceived()` |
| 3. Enter the emailed code | `/Apply/VerifyAccessCode` | `Views/Apply/VerifyAccessCode.cshtml` | `ApplyController.VerifyAccessCode()` |
| 4. The actual application form | `/Apply/Form` | `Views/Apply/Form.cshtml` | `ApplyController.Form()` |
| 5. "Submitted" | `/Apply/Confirmation` | `Views/Apply/Confirmation.cshtml` | `ApplyController.Confirmation()` |

All backend logic: `Controllers/ApplyController.cs`.

### Login / Register / Password reset — under `/Identity/Account/...`
These are a special ASP.NET feature ("Razor Pages") — each page's frontend AND backend live **together in one pair of files**, not split into separate Controller/View folders.
| Page | URL | Files (both live here) |
|---|---|---|
| Login | `/Identity/Account/Login` | `Areas/Identity/Pages/Account/Login.cshtml` + `Login.cshtml.cs` |
| Register | `/Identity/Account/Register` | `Areas/Identity/Pages/Account/Register.cshtml` + `Register.cshtml.cs` |
| Verify email code | `/Identity/Account/VerifyOtp` | `Areas/Identity/Pages/Account/VerifyOtp.cshtml` + `.cshtml.cs` |
| Forgot password | `/Identity/Account/ForgotPassword` | `Areas/Identity/Pages/Account/ForgotPassword.cshtml` + `.cshtml.cs` |
| Reset password | `/Identity/Account/ResetPassword` | `Areas/Identity/Pages/Account/ResetPassword.cshtml` + `.cshtml.cs` |
| Logout | (a button, POSTs to) `/Identity/Account/Logout` | `Logout.cshtml.cs` |

The `.cshtml` file is the frontend, the `.cshtml.cs` file (same name, extra `.cs`) is the backend for that exact same page.

### Privacy page — `/Home/Privacy`
- 🎨 `Views/Home/Privacy.cshtml` · ⚙️ `Controllers/HomeController.cs` → `Privacy()`

---

## 2. Student Pages (logged in, Student role)

### My Enrollments (student's course list) — `/Course/MyEnrollments`
- 🎨 `Views/Course/MyEnrollments.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `MyEnrollments()`
- What it does: computes a progress % per course (lessons completed ÷ total lessons) and the "next lesson to continue" link.

### Cart — `/Cart`
- 🎨 `Views/Cart/Index.cshtml`
- ⚙️ `Controllers/CartController.cs` → `Index()` (+ `RemoveFromCart`, `ApplyCoupon`, `RemoveCoupon`)
- What it does: your "cart" is just your unpaid course enrollments — this page lists them, applies a coupon code, shows the discounted total.

### Checkout (pay for a course) — `/Course/Checkout?courseId={id}`
- 🎨 `Views/Course/Checkout.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `Checkout()`
- The actual payment gateway round-trip (bKash) is backend-only, no page of its own: `Controllers/BkashController.cs` → `Pay()`, `AgreementCallback()`, `PaymentCallback()`.

### Order History — `/Course/OrderHistory`
- 🎨 `Views/Course/OrderHistory.cshtml` · ⚙️ `CourseController.cs` → `OrderHistory()`

### Receipt (PDF download, no page) — `/Course/Receipt/{paymentId}`
- ⚙️ `CourseController.cs` → `Receipt()` — generates a PDF directly via `Services/InvoiceService.cs`, there's no `.cshtml` for this one.

### Viewing a lesson — `/Course/ViewLesson/{id}`
- 🎨 `Views/Course/ViewLesson.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `ViewLesson()` (+ `MarkComplete()` for the "Mark as Complete" button)
- What it does: shows lesson content/video/file, and any assignments/quizzes attached to that lesson, each with its own due date and lock-out logic.

### Take a Quiz — `/Course/TakeQuiz/{quizId}`
- 🎨 `Views/Course/TakeQuiz.cshtml`
- ⚙️ `Controllers/CourseController.cs` → `TakeQuiz()` (shows it) and `SubmitQuiz()` (grades it — uses `Services/QuizGrader.cs`)

### Quiz Result — `/Course/QuizResult/{quizId}`
- 🎨 `Views/Course/QuizResult.cshtml` · ⚙️ `CourseController.cs` → `QuizResult()`

### Submit an Assignment — `/Course/SubmitAssignment/{assignmentId}`
- 🎨 `Views/Course/SubmitAssignment.cshtml` · ⚙️ `CourseController.cs` → `SubmitAssignment()`

### Write / Edit a Review — `/Course/WriteReview?courseId={id}` and `/Course/EditReview?courseId={id}`
- 🎨 `Views/Course/WriteReview.cshtml`, `Views/Course/EditReview.cshtml`
- ⚙️ `CourseController.cs` → `WriteReview()`, `EditReview()`

### Certificate (PDF download, no page) — `/Course/Certificate?courseId={id}`
- ⚙️ `CourseController.cs` → `Certificate()` — generates a PDF via `Services/CertificateService.cs`, only unlocks once every lesson is completed.

### Profile (view / edit) — `/Profile` and `/Profile/Edit`
- 🎨 `Views/Profile/Index.cshtml`, `Views/Profile/Edit.cshtml`
- ⚙️ `Controllers/ProfileController.cs` → `Index()`, `Edit()`
- Shared by every role — an Admin can also view someone else's profile via `/Profile?userId=...`.

---

## 3. Instructor Pages (logged in, Instructor role)

### My Courses (dashboard) — `/Instructor`
- 🎨 `Views/Instructor/Index.cshtml` · ⚙️ `Controllers/InstructorController.cs` → `Index()`

### Create / Edit / Delete a Course
| Page | URL | Frontend | Backend method |
|---|---|---|---|
| Create | `/Instructor/CreateCourse` | `Views/Instructor/CreateCourse.cshtml` | `CreateCourse()` |
| Edit | `/Instructor/EditCourse/{id}` | `Views/Instructor/EditCourse.cshtml` | `EditCourse()` |
| Delete | `/Instructor/DeleteCourse/{id}` | `Views/Instructor/DeleteCourse.cshtml` | `DeleteCourse()` |

All in `Controllers/InstructorController.cs`. Note: instructors can't set a course's price here — only an Admin can (see Admin section).

### Manage Content (modules/lessons/quizzes/assignments inside a course) — `/Instructor/CourseDetails/{id}`
- 🎨 `Views/Instructor/CourseDetails.cshtml` · ⚙️ `InstructorController.cs` → `CourseDetails()`
- From here you add content, each with its own create page:

| Add a... | URL | Frontend | Backend method |
|---|---|---|---|
| Module | `/Instructor/CreateModule?courseId={id}` | `Views/Instructor/CreateModule.cshtml` | `CreateModule()` |
| Lesson | `/Instructor/CreateLesson?moduleId={id}` | `Views/Instructor/CreateLesson.cshtml` | `CreateLesson()` |
| Assignment | `/Instructor/CreateAssignment?lessonId={id}` | `Views/Instructor/CreateAssignment.cshtml` | `CreateAssignment()` |
| Quiz | `/Instructor/CreateQuiz?lessonId={id}` | `Views/Instructor/CreateQuiz.cshtml` | `CreateQuiz()` |

### Reviews (from students) — `/Instructor/Reviews`
- 🎨 `Views/Instructor/Reviews.cshtml` · ⚙️ `InstructorController.cs` → `Reviews()`, `ReplyToReview()`

### Quiz Results (across your courses) — `/Instructor/QuizResults`
- 🎨 `Views/Instructor/QuizResults.cshtml` · ⚙️ `InstructorController.cs` → `QuizResults()`

### Students (roster for one course) — `/Instructor/Students?courseId={id}`
- 🎨 `Views/Instructor/Students.cshtml`
- ⚙️ `InstructorController.cs` → `Students()`, which calls `Services/CourseRosterService.cs` to build the list (name, email, enrolled date, % progress, last activity).

### Report (weekly/monthly activity for one course) — `/Instructor/CourseReport?courseId={id}&period=weekly`
- 🎨 `Views/Instructor/CourseReport.cshtml`
- ⚙️ `InstructorController.cs` → `CourseReport()` (shows it), `ExportCourseReportPdf()` (downloads it as PDF)
- The number-crunching (bucketing by week/month) lives in `Services/CourseReportService.cs`.

---

## 4. Admin Pages (logged in, Admin role) — all under `/Admin/Admin/...`

### Dashboard — `/Admin/Admin/Index`
- 🎨 `Areas/Admin/Views/Admin/Index.cshtml` · ⚙️ `Areas/Admin/Controllers/AdminController.cs` → `Index()`
- Shows total users/courses/enrollments, the registrations-per-month chart, and quick links to everything below.

### Manage Categories — `/Category` (its own controller, not under Admin/Admin)
- 🎨 `Views/Category/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`
- ⚙️ `Controllers/CategoryController.cs` (all 4 actions: `Index`, `Create`, `Edit`, `Delete`)

### Manage Users — `/Admin/Admin/Users`
- 🎨 `Areas/Admin/Views/Admin/Users.cshtml`
- ⚙️ `AdminController.cs` → `Users()` (the list), `Approve()`, `Reject()`, `ToggleActive()` (the action buttons)

### View an instructor's application — `/Admin/Admin/ViewApplication/{id}`
- 🎨 `Areas/Admin/Views/Admin/ViewApplication.cshtml`
- ⚙️ `AdminController.cs` → `ViewApplication()`, `DownloadResume()`

### Rejected Applications (archive) — `/Admin/Admin/RejectedApplications`
- 🎨 `Areas/Admin/Views/Admin/RejectedApplications.cshtml` · ⚙️ `AdminController.cs` → `RejectedApplications()`

### Access Requests (the "Apply as Instructor" front door) — `/Admin/Admin/AccessRequests`
- 🎨 `Areas/Admin/Views/Admin/AccessRequests.cshtml`
- ⚙️ `AdminController.cs` → `AccessRequests()`, `ApproveAccessRequest()`, `DenyAccessRequest()`

### Pending Courses (approve/reject new courses) — `/Admin/Admin/PendingCourses`
- 🎨 `Areas/Admin/Views/Admin/PendingCourses.cshtml`
- ⚙️ `AdminController.cs` → `PendingCourses()`, `ApproveCourse()` (this is also where the price gets set), `RejectCourse()`

### Manage Courses (every course, any status) — `/Admin/Admin/AllCourses`
- 🎨 `Areas/Admin/Views/Admin/AllCourses.cshtml`
- ⚙️ `AdminController.cs` → `AllCourses()`, `EditCoursePrice()`, `DeleteCourse()`

### Course Overview (drill into one course) — `/Admin/Admin/CourseOverview/{id}`
- 🎨 `Areas/Admin/Views/Admin/CourseOverview.cshtml`
- ⚙️ `AdminController.cs` → `CourseOverview()` — shows the instructor's details, a live activity timeline, and the student roster (reuses `Services/CourseRosterService.cs`)

### Course Report (weekly/monthly, any course) — `/Admin/Admin/CourseReport?courseId={id}`
- 🎨 `Areas/Admin/Views/Admin/CourseReport.cshtml`
- ⚙️ `AdminController.cs` → `CourseReport()`, `ExportCourseReportPdf()` (same `Services/CourseReportService.cs` as the instructor version)

### All Enrollments — `/Admin/Admin/AllEnrollments`
- 🎨 `Areas/Admin/Views/Admin/AllEnrollments.cshtml` · ⚙️ `AdminController.cs` → `AllEnrollments()`

### Revenue Report — `/Admin/Admin/Revenue`
- 🎨 `Areas/Admin/Views/Admin/Revenue.cshtml`
- ⚙️ `AdminController.cs` → `Revenue()`, `ExportRevenuePdf()`

### Course Analytics (most popular / top rated) — `/Admin/Admin/Analytics`
- 🎨 `Areas/Admin/Views/Admin/Analytics.cshtml`
- ⚙️ `AdminController.cs` → `Analytics()`, `ExportAnalyticsPdf()`

### Payments (every transaction + refund button) — `/Admin/Admin/Payments`
- 🎨 `Areas/Admin/Views/Admin/Payments.cshtml`
- ⚙️ `AdminController.cs` → `Payments()`, `RefundPayment()`

### Coupons — `/Admin/Admin/Coupons` and `/Admin/Admin/CreateCoupon`
- 🎨 `Areas/Admin/Views/Admin/Coupons.cshtml`, `CreateCoupon.cshtml`
- ⚙️ `AdminController.cs` → `Coupons()`, `CreateCoupon()`, `ToggleCouponActive()`, `DeleteCoupon()`
- The discount-checking rules live in `Services/CouponService.cs`.

---

## 5. Backend-only files (no page of their own — but they power several pages above)

Nobody navigates to these directly, but editing them changes behavior across multiple pages:

| File | What it controls |
|---|---|
| `Data/ApplicationDbContext.cs` | The master list of every database table in the whole app |
| `Services/SmtpEmailService.cs` | Every email the site sends (approvals, rejections, OTP codes, invites) |
| `Services/NotificationService.cs` | Every entry that shows up in the notification bell (top-right, every page) |
| `Services/ReportPdfService.cs` | The Revenue / Analytics / Course Activity PDF documents |
| `Services/CertificateService.cs` | The certificate PDF |
| `Services/InvoiceService.cs` | The receipt PDF |
| `Services/QuizGrader.cs` | How a submitted quiz gets scored |
| `Services/CourseProgressCalculator.cs` | The "% complete" math used everywhere progress shows up |
| `Services/CourseRosterService.cs` | Builds the student list + activity timeline (Instructor's Students page, Admin's Course Overview) |
| `Services/CourseReportService.cs` | Buckets activity into weekly/monthly numbers for the Report pages |
| `Services/CouponService.cs` | Whether a coupon code is valid right now |
| `Services/FileUploadService.cs` | Every file upload on the site (profile pictures, resumes, thumbnails, assignment submissions) |
| `Services/GeminiChatService.cs` | The AI chat bubble's replies |
| `Services/BkashPaymentService.cs` + `Controllers/BkashController.cs` | The bKash payment gateway round-trip |
| `Models/*.cs` (one file per thing, e.g. `Course.cs`, `Quiz.cs`, `Enrollment.cs`) | The actual shape of each database table — add a property here (+ a migration) to add a new column |

---

## Quick tips for the live defense

- **To change what a page *looks* like** (wording, colors, which fields show): edit the `.cshtml` file, save, refresh the browser. No restart needed for most view-only changes if the site is already running — but if it doesn't update, stop the app (`Ctrl+C` in the terminal) and run `dotnet run` again.
- **To change what a page *does*** (a rule, a calculation, what gets saved): edit the `.cs` controller file. This **always** needs a restart (`dotnet run` again) to take effect, since C# code has to be recompiled.
- **If you break something**: the terminal running `dotnet run` will show a red error with a file name and line number — that tells you exactly where to look.
- **Every role's badge/permission check** looks like `[Authorize(Roles = "Admin")]` (or `Instructor`, or `Student`) right above a controller class or method — that one line is what decides who's allowed to see that page at all.

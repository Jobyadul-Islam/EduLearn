# -*- coding: utf-8 -*-
"""Chapter 8 — Quality Assurance and Testing."""
from docx_engine import *


def tc(doc, tcid, priority, module, title, precond, steps, cap):
    add_table(doc, [
        ["Item", "Description"],
        ["Test Case ID", tcid], ["Test Priority", priority], ["Module Name", module],
        ["Title of the Test", title], ["Precondition", precond],
    ], size=10, col_widths=[1.48, 4.43])
    rows = [["Step", "Step Name", "Test Data", "Expected Result", "Actual Result", "Status"]] + steps
    add_table(doc, rows, caption=cap, size=8.3, col_widths=[0.39, 1.06, 1.06, 1.55, 1.35, 0.48])


def build(doc):
    add_heading1(doc, "Chapter 8. Quality Assurance and Testing")
    add_para(doc, "Quality assurance and testing are essential activities in the development of EduLearn. They help ensure the application behaves according to its requirements, maintains data consistency, protects restricted resources, and provides reliable functionality across all three roles — particularly since the system handles real payment flows and enforced academic deadlines, where a silent defect is not merely inconvenient but could mean a student is wrongly charged, wrongly denied access they paid for, or able to submit work after a deadline that was supposed to be firm.")
    add_para(doc, "EduLearn's testing combined two complementary approaches used throughout development: an automated xUnit test suite (33 test cases) and live, evidence-based manual testing — exercising the actual running application over real HTTP requests and verifying the result directly against the live SQL Server database, rather than trusting that a change “should” work from reading the code alone. This second discipline is what actually caught the routing defect described in Chapter 5 (RMMM 06); no amount of code review surfaced it, only following the real generated link did.")

    add_heading2(doc, "8.1 System Quality Management")
    add_para(doc, "For EduLearn, quality management focuses on ensuring the system satisfies its functional requirements, maintains data integrity, provides appropriate authorization, and behaves correctly at the boundaries that matter most: deadlines, pricing, refunds, and account deletion.")
    for b in ["Functional correctness of course, enrollment, and assessment workflows.", "Secure, rate-limited instructor onboarding.",
              "Correct role- and ownership-based authorization.", "Accurate quiz auto-grading and due-date enforcement for both quizzes and assignments.",
              "Consistent enrollment/payment state, including on refund.", "Correct certificate-eligibility computation.",
              "Reliable notification generation and read/unread state.", "Accurate roster and activity-report aggregation.",
              "Correct PDF generation (certificates, receipts, revenue/analytics/course-activity reports).", "Responsive, role-appropriate interfaces."]:
        add_bullet(doc, b)
    add_heading3(doc, "8.1.1 Software Quality Management Process")
    add_para(doc, "Quality Planning: defined expected behavior for authentication, instructor onboarding, course approval and pricing, enrollment/payment, assessment, certification, and reporting before and during each module's development.")
    add_para(doc, "Quality Assurance: followed the MVC-with-a-Service-Layer architecture described in Chapter 7, so that business rules live in one owned, testable place rather than being duplicated across controllers.")
    add_para(doc, "Quality Control: identifying defects through both the automated suite and live testing, and correcting them before a module was considered complete. Overall process: Quality Planning → Module Development → Quality Assurance → Automated + Live Testing → Defect Identification → Correction & Refinement → Retesting → Final Validation.")

    add_heading2(doc, "8.2 System Testing")
    add_para(doc, "System testing verifies the complete, integrated EduLearn platform against its functional and non-functional requirements, checking not just individual pages but full workflows.")
    add_heading3(doc, "8.2.1 Reasons for Performing System Testing")
    for b in ["To verify that EduLearn's functional requirements were correctly implemented.",
              "To ensure users can access only the functions permitted by their role.",
              "To verify authentication, registration (with OTP), and logout work correctly.",
              "To ensure course records are created, approved, priced, edited, and deleted correctly.",
              "To verify the instructor access-request flow's rate limiting and expiry actually block a brute-force or stale attempt.",
              "To ensure enrollment and payment state remain consistent through checkout and refund.",
              "To verify quiz grading is arithmetically correct and that both quizzes and assignments are rejected after their due date.",
              "To ensure certificate eligibility reflects real recorded lesson completion.",
              "To verify roster/report numbers match a manual database count.",
              "To identify and correct defects before considering the system complete."]:
        add_bullet(doc, b)
    add_heading3(doc, "8.2.2 System Testing Methodology")
    add_para(doc, "A combination of black-box testing (via the application's real interfaces and HTTP endpoints) and white-box-informed automated testing (the xUnit suite, exercising controller/service code paths against a seeded in-memory database) was used, comprising Functional Testing, Non-Functional Testing, Automated Regression Testing, and Incremental Testing performed as modules were built.")
    add_para(doc, "The xUnit suite (33 tests) is run after any change to catch unintended breakage — this is not hypothetical: adding the Quiz.DueDate field broke three previously-passing tests whose seeded quiz objects had no due date and therefore defaulted to the year 1 (permanently “expired”); the suite caught this immediately, and the fix was verified by re-running the full suite to 33/33 passing again.")

    add_heading2(doc, "8.3 System Testing Design")
    add_para(doc, "The following test cases validate EduLearn's major functional workflows. Status values reflect the actual outcome of running these cases against the live application and its database during this practicum, and against the automated suite where noted.")

    tc(doc, "TC-01", "High", "Authentication", "Verifying self-registration with OTP and login", "A valid, unused email address must be available", [
        ["1", "Submit registration form", "Valid name, email, password", "A 6-digit OTP is emailed and the registration is held in session", "OTP emailed; no row created until verified", "Pass"],
        ["2", "Enter correct OTP within 10 min", "Correct code", "Student account created and signed in", "Account created, signed in", "Pass"],
        ["3", "Enter incorrect OTP", "Wrong code", "Error shown; no account created", "Error shown, no account created", "Pass"],
        ["4", "Log in with the new account", "Registered credentials", "Login succeeds, lands on catalog", "Login succeeded", "Pass"],
        ["5", "Log out", "Authenticated session", "Session ends", "Session ended", "Pass"],
    ], "Table 8.1 User Authentication and Registration.")

    tc(doc, "TC-02", "High", "Authorization", "Verifying role-based and ownership-based access control", "Accounts for all three roles, and two distinct Instructors, must exist", [
        ["1", "Student requests Admin-only URL", "/Admin/Admin/Users", "Access denied", "Access denied", "Pass"],
        ["2", "Instructor A requests Instructor B's course URL", "B's courseId", "404 (ownership check fails)", "404 returned", "Pass"],
        ["3", "Unauthenticated visitor requests Student-only URL", "/Course/MyEnrollments", "Redirected to Login with ReturnUrl", "Redirected correctly", "Pass"],
        ["4", "Each role loads its own landing page", "Role-specific credentials", "Correct dashboard renders", "Verified for all three roles", "Pass"],
    ], "Table 8.2 Role-Based Authorization.")

    tc(doc, "TC-03", "High", "Instructor Onboarding", "Verifying the full access-request → OTP → application pipeline", "Google OAuth configured; an Admin account available", [
        ["1", "Submit access request via Google", "Real Google account", "Pending request created with verified email", "Verified via DB query", "Pass"],
        ["2", "Submit access request via email", "Valid email", "Request created, Method=Email", "Request created", "Pass"],
        ["3", "Admin approves the request", "Pending request", "Code + link emailed together", "Received (after routing fix)", "Pass"],
        ["4", "Enter an incorrect code", "Wrong value", "Error, attempts remaining shown", "“5 attempts left” etc.", "Pass"],
        ["5", "Exceed 5 incorrect attempts", "6th wrong attempt", "Further attempts blocked", "Blocked with message", "Pass"],
        ["6", "Enter correct code", "Correct code", "Redirected to application form", "Redirected correctly", "Pass"],
        ["7", "Submit application with résumé", "Valid applicant details", "Account created, request consumed", "Verified via DB query", "Pass"],
        ["8", "Admin approves full application", "Pending applicant", "Password-setup email sent", "Verified live", "Pass"],
    ], "Table 8.3 Instructor Access Request and OTP Onboarding.")

    tc(doc, "TC-04", "High", "Course Management", "Verifying course creation, approval, and re-review on edit", "Approved Instructor account", [
        ["1", "Create a new course", "Title, description, category", "Status=Pending, Price=0", "Created Pending, price forced 0", "Pass"],
        ["2", "Admin approves, sets price", "Amount e.g. 500", "Status=Approved, Price=500", "Verified via DB query", "Pass"],
        ["3", "Instructor edits approved course", "Updated description", "Status reverts to Pending, Price untouched", "Verified", "Pass"],
        ["4", "Admin rejects with a reason", "Reason text", "Status=Rejected, reason emailed", "Verified", "Pass"],
    ], "Table 8.4 Course Creation and Admin Approval.")

    tc(doc, "TC-05", "High", "Course Pricing", "Verifying Instructors cannot set price, and Admin can edit/delete any course", "Approved course exists", [
        ["1", "Instructor's Create/Edit form", "—", "No price field rendered", "Confirmed removed", "Pass"],
        ["2", "Craft raw POST with Price as Instructor", "Price=9999", "Ignored server-side", "Confirmed ignored", "Pass"],
        ["3", "Admin edits price of approved course", "New amount", "Updates independent of status", "Verified", "Pass"],
        ["4", "Admin deletes a course with enrollments", "Confirm modal", "Course/content/enrollments/payments/reviews removed", "Verified via DB query", "Pass"],
    ], "Table 8.5 Course Pricing and Course Management.")

    tc(doc, "TC-06", "High", "Enrollment", "Verifying free and paid enrollment creation", "Approved free and paid courses exist", [
        ["1", "Student enrolls in free course", "Free course", "Status=Active immediately", "Verified", "Pass"],
        ["2", "Student enrolls in paid course", "Paid course", "Status=Pending, appears in Cart", "Verified", "Pass"],
        ["3", "Access paid content before payment", "Pending enrollment", "Only free-preview lessons accessible", "Verified", "Pass"],
    ], "Table 8.6 Enrollment (Free and Paid).")

    tc(doc, "TC-07", "High", "Payment", "Verifying coupon application and bKash checkout", "Pending paid enrollment in Cart; active coupon exists", [
        ["1", "Apply valid coupon", "Coupon code", "Discounted total computed", "Verified", "Pass"],
        ["2", "Apply expired/exhausted coupon", "Invalid code", "Rejected, no discount", "Verified", "Pass"],
        ["3", "Complete bKash checkout", "Sandbox credentials", "Payment=Success, Enrollment=Active", "Verified via DB query", "Pass"],
        ["4", "Admin refunds the payment", "Successful payment", "Payment=Refunded, Enrollment=Pending", "Verified", "Pass"],
    ], "Table 8.7 Cart, Coupons, and bKash Checkout.")

    tc(doc, "TC-08", "High", "Progress Tracking", "Verifying lesson completion and progress percentage", "Active enrollment in multi-lesson course", [
        ["1", "Mark a lesson complete", "Lesson ID", "LessonProgress row created/updated", "Verified", "Pass"],
        ["2", "View My Enrollments", "Partially complete course", "Progress % matches manual count", "Verified", "Pass"],
        ["3", "Complete every lesson", "All lessons", "Progress=100%, certificate available", "Verified", "Pass"],
    ], "Table 8.8 Lesson Progress and Mark-as-Complete.")

    tc(doc, "TC-09", "High", "Assignments", "Verifying submission before and rejection after the due date", "An assignment with a known due date exists", [
        ["1", "Submit before due date", "Valid file", "Submission accepted", "Accepted", "Pass"],
        ["2", "Resubmit before due date", "Replacement file", "Updated in place, not duplicated", "Verified single row", "Pass"],
        ["3", "Open submission page after due date (GET)", "Expired assignment", "Redirected with “deadline passed”", "Verified", "Pass"],
        ["4", "Raw POST after due date", "Crafted request", "Rejected server-side", "Verified independently", "Pass"],
    ], "Table 8.9 Assignment Submission and Due-Date Lockout.")

    tc(doc, "TC-10", "High", "Quizzes", "Verifying auto-grading, retake behavior, and due-date lockout", "A quiz with known correct answers and a due date exists", [
        ["1", "Submit all-correct answers", "Correct option IDs", "Score=total; Passed=true", "Confirmed via automated test", "Pass"],
        ["2", "Retake with a worse score", "Incorrect option IDs", "Existing row updates in place", "Confirmed via automated test", "Pass"],
        ["3", "Instructor views results (own course only)", "Two Instructors, one quiz", "Owning Instructor sees result; other sees none", "Confirmed via automated test", "Pass"],
        ["4", "Attempt after due date", "Expired quiz", "Both GET and POST redirect with message", "Verified live and via automated test", "Pass"],
    ], "Table 8.10 Quiz Attempt, Auto-Grading, and Due-Date Lockout.")

    tc(doc, "TC-11", "Medium", "Certification", "Verifying certificate eligibility and PDF generation", "Partial and full-completion enrollments", [
        ["1", "Request certificate, incomplete progress", "Partial course", "Refused with a clear message", "Verified", "Pass"],
        ["2", "Request certificate, 100% complete", "Fully completed course", "Valid PDF downloads", "Verified", "Pass"],
    ], "Table 8.11 Certificate Issuance.")

    tc(doc, "TC-12", "Medium", "Reviews", "Verifying review submission is gated on completion and is editable", "One completed and one incomplete enrollment", [
        ["1", "Review an incomplete course", "Rating + comment", "Rejected", "Verified", "Pass"],
        ["2", "Review a completed course", "Rating + comment", "Created; second attempt blocked", "Verified", "Pass"],
        ["3", "Edit an existing review", "Updated rating/comment", "Updates in place", "Verified", "Pass"],
    ], "Table 8.12 Review Submission and Editing.")

    tc(doc, "TC-13", "Medium", "Notifications", "Verifying notification generation and read/unread styling", "A relevant event must occur", [
        ["1", "Trigger course approval", "Pending course", "Instructor receives a Notification", "Verified", "Pass"],
        ["2", "Trigger new enrollment", "New paid enrollment", "Instructor notified", "Verified", "Pass"],
        ["3", "View bell with mixed items", "Read/unread mix", "Unread bold/tinted; read plain", "Verified visually", "Pass"],
        ["4", "Click notification / Mark all read", "Unread item(s)", "IsRead flips true", "Verified", "Pass"],
    ], "Table 8.13 Notification System.")

    tc(doc, "TC-14", "High", "Reporting", "Verifying roster accuracy and weekly/monthly report numbers", "A course with real enrollment/completion/submission activity", [
        ["1", "Open Students for a course", "Real seeded activity", "Correct roster fields", "Progress/last-activity matched exactly", "Pass"],
        ["2", "Open monthly Report", "Same course", "Correct month shows count=1", "Matched manual DB count", "Pass"],
        ["3", "Export report as PDF", "Monthly period", "Valid PDF downloads", "Real application/pdf response", "Pass"],
    ], "Table 8.14 Instructor Student Roster and Reports.")

    tc(doc, "TC-15", "High", "Admin Reporting", "Verifying the Admin course drill-down for a course the Admin does not teach", "A course owned by a different Instructor", [
        ["1", "Open Course Overview as Admin", "Course owned by another Instructor", "Shows that Instructor's own details", "Verified live", "Pass"],
        ["2", "Review activity timeline", "Real events", "Newest-first, correct description/timestamp", "Verified ordering", "Pass"],
        ["3", "Review roster on same page", "Same course", "Matches Instructor-side roster exactly", "Verified", "Pass"],
    ], "Table 8.15 Admin Course Overview.")

    tc(doc, "TC-16", "High", "User Management", "Verifying search/filter, activation, and dashboard-count consistency", "Multiple users across roles/statuses", [
        ["1", "Search/filter Manage Users", "Role/status filters", "Correct filtered subset", "Verified", "Pass"],
        ["2", "Deactivate then reactivate a user", "Active user", "Status toggles", "Verified", "Pass"],
        ["3", "Compare Dashboard totals to list count", "Same moment", "Counts must agree exactly", "Real discrepancy found & fixed", "Pass"],
    ], "Table 8.16 Admin User Management.")

    tc(doc, "TC-17", "High", "Payments", "Verifying the Admin payments ledger and refund action", "At least one successful payment exists", [
        ["1", "Open Payments", "—", "All payments listed with details", "Verified", "Pass"],
        ["2", "Refund a successful payment", "Successful payment row", "Status→Refunded, enrollment→Pending", "Verified via DB query", "Pass"],
        ["3", "Attempt to refund an already-refunded payment", "Refunded payment", "Rejected as not eligible", "Verified", "Pass"],
    ], "Table 8.17 Payments and Refunds.")

    tc(doc, "TC-18", "Medium", "Content Preview", "Verifying Admin/Instructor can preview any course without enrolling", "A paid course neither viewer has purchased", [
        ["1", "Admin opens a locked paid lesson", "Paid course", "Content visible, no enrollment row created", "Verified", "Pass"],
        ["2", "Check for student-only controls", "Same lesson", "None render for Admin/Instructor", "Verified", "Pass"],
        ["3", "Instructor previews own vs. other's course", "Both cases", "Both work identically", "Verified", "Pass"],
    ], "Table 8.18 Admin/Instructor Full-Content Preview Access.")

    tc(doc, "TC-19", "Low", "AI Assistant", "Verifying the chat widget is available only when logged in", "Logged-in and logged-out sessions", [
        ["1", "View any page logged out", "—", "Chat bubble not present", "Verified", "Pass"],
        ["2", "View any page logged in, send message", "“Hello!”", "Relevant reply returned", "Transient upstream error investigated and confirmed", "Pass"],
    ], "Table 8.19 Chatbot Assistant.")

    tc(doc, "TC-20", "Medium", "PDF Generation", "Verifying certificate, receipt, and report PDFs are generated correctly", "Eligible data exists for each document type", [
        ["1", "Generate a certificate", "Fully completed course", "Valid PDF, correct name/date", "Verified (automated + live)", "Pass"],
        ["2", "Generate a receipt", "Successful payment", "Valid PDF, correct transaction details", "Verified", "Pass"],
        ["3", "Export Revenue Report", "Real payment data", "Valid PDF matching on-screen data", "Verified", "Pass"],
        ["4", "Export a Course Activity Report, zero data", "New course, no activity", "PDF still generates cleanly", "Verified via dedicated test", "Pass"],
    ], "Table 8.20 PDF Report and Document Generation.")

    add_heading2(doc, "8.4 Non-Functional Testing")
    add_heading3(doc, "8.4.1 Security Testing")
    for b in ["Password hashing and verification (ASP.NET Core Identity default).", "Role- and ownership-based authorization, tested by direct URL access.",
              "Session and authentication cookie handling, including confirming the Google OAuth external cookie never becomes a persistent login.",
              "Rate limiting and expiry on the instructor one-time code (5 attempts, 24-hour window).", "Input and file-upload validation.",
              "Confirmed that a crafted request cannot set Instructor-controlled fields the UI no longer exposes."]:
        add_bullet(doc, b)
    add_heading3(doc, "8.4.2 Usability Testing")
    for b in ["Clear, role-specific dashboards and navigation.", "Responsive Bootstrap-based interface, verified at narrow widths.",
              "Specific, actionable validation and error messages.", "Gmail-style unread notifications, verified visually distinct from read ones."]:
        add_bullet(doc, b)
    add_heading3(doc, "8.4.3 Data Integrity Testing")
    add_para(doc, "Important relationships tested include Users↔Roles, Courses↔Enrollments, Courses↔Content, Quizzes↔QuizResults, Assignments↔AssignmentSubmissions, Coupons↔CouponRedemptions, and Payments↔Enrollments. Delete-behavior correctness was specifically tested by deleting a course with real enrollments and confirming the full cascade, and separately by confirming that deleting a user account never removes unrelated course content.")
    add_heading3(doc, "8.4.4 File Upload Testing")
    add_para(doc, "Tested upload paths: instructor résumés, profile pictures, course thumbnails, and assignment submissions — each validated by FileUploadService against allowed extensions and size limits.")
    add_heading3(doc, "8.4.5 Background Service Testing")
    add_para(doc, "The DeadlineReminderBackgroundService was tested to confirm it correctly identifies assignments due soon and emails the affected students without blocking normal request handling.")

    add_heading2(doc, "8.5 Integration Testing")
    for h, txt in [
        ("Access Request → OTP → Application → Full Approval", "each stage's output is required input for the next, tested as one continuous chain."),
        ("Course Approval → Pricing → Enrollment", "a course's price, set only at Admin approval, is what checkout reads — tested end-to-end."),
        ("Enrollment → Payment → Access", "paid access is granted only on a confirmed successful payment, and revoked on refund — tested in both directions."),
        ("Lesson Completion → Progress → Certificate", "certificate eligibility is computed from real LessonProgress rows, tested by confirming refusal at 99% and grant at 100%."),
        ("Quiz/Assignment Due Date → Lockout → Roster/Report", "a lockout event is reflected consistently in both the interactive page and the aggregated roster/report numbers."),
        ("Roster/Report Service → Instructor / Admin Controller", "the identical service output was confirmed to render correctly from both callers."),
    ]:
        p = doc.add_paragraph()
        r = p.add_run(h + ": "); set_run_font(r, 12, bold=True)
        r2 = p.add_run(txt); set_run_font(r2, 12)
        p.paragraph_format.space_after = Pt(8)

    add_heading2(doc, "8.6 System Testing Summary")
    add_para(doc, "System testing covered authentication, authorization, instructor onboarding, course management and pricing, enrollment and payment, assessment and deadline enforcement, certification, reviews, notifications, reporting, and Admin/Instructor content preview. Both the 33-case automated xUnit suite and extensive live, database-verified manual testing were used throughout development, not only at the end.")
    add_para(doc, "This combination proved valuable in practice: the automated suite caught a real regression the moment the Quiz.DueDate field was added, while live testing caught a defect the automated suite structurally could not have — a malformed URL in a real sent email, only detectable by actually following the link.")
    add_para(doc, "Overall, the quality assurance and testing activities provided a structured, evidence-based approach for validating EduLearn's reliability, security, functionality, and data consistency, with every claim in this chapter backed by an actual verification performed during development rather than an assumption about how the code “should” behave.")

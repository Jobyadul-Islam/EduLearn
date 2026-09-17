# -*- coding: utf-8 -*-
"""Chapters 6-7."""
from docx_engine import *


def build(doc):
    # ============================================================ CHAPTER 6
    add_heading1(doc, "Chapter 6. Project Planning")
    add_para(doc, "Project planning is an essential part of software development because it helps determine the functional scope, estimated effort, required resources, and development schedule of a project. For EduLearn, project planning was carried out by analyzing the major system functions, logical data groups, technical resources, and development activities actually implemented.")
    add_para(doc, "Function Point Analysis (FPA) was used to estimate the functional size of EduLearn. The estimation considers transaction functions, data functions, and general system characteristics. The calculated effort provides a theoretical estimation for planning purposes, while EduLearn was actually developed through iterative, incremental development with continuous live verification, as described in Chapter 1.")

    add_heading2(doc, "6.1 Project Estimation")
    add_para(doc, "Function Point Analysis was selected because EduLearn contains a substantial number of identifiable, user-facing functions and logical data groups spanning three roles. The major steps of the estimation process were: (1) identification of transaction functions, (2) identification of logical data functions, (3) assignment of complexity levels, (4) calculation of Unadjusted Function Points (UFP), (5) evaluation of the Total Degree of Influence (TDI), (6) calculation of the Value Adjustment Factor (VAF), (7) calculation of Adjusted Function Points (AFP), (8) estimation of development effort, and (9) planning of development resources and activities.")

    add_heading2(doc, "6.2 Function Oriented Metrics")
    add_para(doc, "Function-oriented metrics measure functional size based on the functions provided to users and the logical data maintained or referenced by the system. Functions are considered under two categories: Transaction Functions — functions that allow data to be entered, processed, updated, or presented — and Data Functions — logical groups of data maintained by EduLearn or referenced from an external system.")

    add_heading2(doc, "6.3 Unadjusted Function Point Contribution — Transaction Functions")
    add_table(doc, [
        ["SL", "Transaction Function", "Type", "Complexity", "UFP"],
        ["1", "Registration/Login/Logout", "EI", "Average", "4"],
        ["2", "Profile View/Edit", "EI", "Average", "4"],
        ["3", "Instructor Access Request (Google/Email) + OTP Verification", "EI", "High", "6"],
        ["4", "Instructor Application Submission & Admin Review", "EI", "High", "6"],
        ["5", "Course Creation/Edit/Delete (Instructor)", "EI", "High", "6"],
        ["6", "Course Approval & Pricing (Admin)", "EI", "High", "6"],
        ["7", "Course Price Edit / Delete (Admin, any course)", "EI", "Average", "4"],
        ["8", "Content Authoring: Module/Lesson/Assignment/Quiz", "EI", "High", "6"],
        ["9", "Enrollment (Free/Paid) & Cart/Coupon", "EI", "High", "6"],
        ["10", "bKash Checkout & Admin Refund", "EI", "High", "6"],
        ["11", "Lesson Progress & Certificate Issuance", "EO", "High", "7"],
        ["12", "Quiz Attempt & Auto-Grading (due-date enforced)", "EO", "High", "7"],
        ["13", "Assignment Submission (due-date enforced)", "EI", "Average", "4"],
        ["14", "Review Submission/Edit", "EI", "Average", "4"],
        ["15", "Notification Generation & Read-State", "EO", "Average", "5"],
        ["16", "Instructor/Admin Roster & Activity Report (PDF)", "EO", "High", "7"],
        ["17", "Admin Revenue/Analytics Reports (PDF)", "EO", "High", "7"],
        ["18", "Admin User/Category/Coupon Management", "EI", "High", "6"],
        ["19", "AI Chat Assistant", "EI", "Average", "4"],
        ["", "Total", "", "", "105"],
    ], caption="Table 6.1 UFP for Transaction Functions.", size=9.5, col_widths=[0.39, 3.54, 0.59, 0.89, 0.49])
    add_para(doc, "Transaction UFP = 105")

    add_heading2(doc, "6.4 Unadjusted Function Point Contribution — Data Functions")
    add_table(doc, [
        ["SL", "Data Function", "Type", "Complexity", "UFP"],
        ["1", "Users & Roles Data (ApplicationUser, Identity tables)", "ILF", "High", "15"],
        ["2", "Course & Category Data", "ILF", "High", "15"],
        ["3", "Course Content Data (Modules, Lessons, Assignments, Quizzes, Questions, Options)", "ILF", "High", "15"],
        ["4", "Enrollment & Payment Data (Enrollments, Payments, Coupons, Redemptions)", "ILF", "High", "15"],
        ["5", "Assessment Result Data (QuizResults, AssignmentSubmissions, LessonProgress)", "ILF", "High", "15"],
        ["6", "Instructor Access Request & Archive Data", "ILF", "Average", "10"],
        ["7", "Notification Data", "ILF", "Average", "10"],
        ["8", "Review Data", "ILF", "Average", "10"],
        ["9", "Google OAuth Identity Data", "EIF", "Low", "5"],
        ["10", "bKash Payment Gateway Data", "EIF", "Low", "5"],
        ["", "Total", "", "", "115"],
    ], caption="Table 6.2 UFP for Data Functions.", size=9.5, col_widths=[0.39, 3.54, 0.59, 0.89, 0.49])
    add_para(doc, "Internal logical data contribution: ILF UFP = 105. External interface contribution: EIF UFP = 10. Data Function UFP = 105 + 10 = 115.")

    add_heading2(doc, "6.5 Performance and Environmental Impact (TDI)")
    add_table(doc, [
        ["SL", "General System Characteristic", "Degree of Influence"],
        ["1", "Data Communications", "3"], ["2", "Distributed Data Processing", "2"], ["3", "Performance", "4"],
        ["4", "Heavily Used Configuration", "3"], ["5", "Transaction Rate", "3"], ["6", "Online Data Entry", "5"],
        ["7", "End-User Efficiency", "4"], ["8", "Online Update", "5"], ["9", "Complex Processing", "5"],
        ["10", "Reusability", "4"], ["11", "Installation Ease", "3"], ["12", "Operational Ease", "4"],
        ["13", "Multiple Sites", "1"], ["14", "Facilitate Change", "4"], ["", "Total TDI", "50"],
    ], caption="Table 6.3 Total Degree of Influence.", col_widths=[0.49, 3.93, 1.48])
    add_para(doc, "TDI = 50. VAF = 0.65 + (TDI/100) = 0.65 + 0.50 = 1.15.")

    add_heading2(doc, "6.6 Function Point Calculation")
    add_para(doc, "UFP = Transaction UFP + Data Function UFP = 105 + 115 = 220. AFP = UFP × VAF = 220 × 1.15 = 253.")
    add_table(doc, [
        ["Metric", "Value"],
        ["Transaction Function UFP", "105"], ["Internal Logical File UFP", "105"], ["External Interface File UFP", "10"],
        ["Total Data Function UFP", "115"], ["Total UFP", "220"], ["Total Degree of Influence", "50"],
        ["Value Adjustment Factor", "1.15"], ["Adjusted Function Points", "253 FP"],
    ], caption="Table 6.4 Function Point Calculation Summary.", col_widths=[3.44, 2.46])

    add_heading2(doc, "6.7 Effort Estimation")
    add_para(doc, "For planning purposes, a productivity assumption of 12.5 person-hours per Function Point was used. Estimated Effort = AFP × Productivity Rate = 253 × 12.5 = 3,162.5 person-hours. Using 9 working hours per day: Person-Days = 3,162.5 / 9 ≈ 351.4 person-days. Using 22 working days per month: Person-Months = 351.4 / 22 ≈ 15.98 ≈ 16 person-months.")
    add_para(doc, "As with any Function Point-based estimate, this figure represents theoretical human effort, not calendar duration. EduLearn was actually developed by a single student over several weeks of active, iterative, session-based development within the practicum period, working considerably faster than the theoretical estimate would suggest — a difference consistent with the project's tightly-scoped, incremental development style and its heavy reliance on live verification to avoid rework.")

    add_heading2(doc, "6.8 Resource Planning")
    add_table(doc, [
        ["SL", "Resource", "Purpose"],
        ["1", "Visual Studio Code", "Application development"], ["2", "C#", "Backend programming"],
        ["3", "ASP.NET Core MVC (.NET 10)", "Web application framework"], ["4", "Entity Framework Core", "ORM and database migrations"],
        ["5", "Microsoft SQL Server Express", "Local relational database"],
        ["6", "HTML, CSS, Bootstrap 5, JavaScript, Chart.js", "Frontend development and reporting charts"],
        ["7", "Google OAuth 2.0", "Instructor access-request authentication"], ["8", "SMTP / MailKit", "Email notifications and one-time codes"],
        ["9", "bKash Tokenized Checkout API (Sandbox)", "Payment processing"], ["10", "QuestPDF", "Certificate, receipt, and report PDF generation"],
        ["11", "xUnit", "Automated testing"], ["12", "Local File Storage", "Résumé, submission, and image storage"],
        ["13", "Git / GitHub", "Version control"], ["14", "Web Browser / curl", "Manual and scripted live application testing"],
    ], caption="Table 6.5 Development Resources.", size=9.5, col_widths=[0.39, 2.71, 2.8])

    add_heading2(doc, "6.9 Task Scheduling")
    add_para(doc, "EduLearn was developed iteratively across twelve working periods, following an incremental approach in which modules were built, verified against the live system, and refined progressively.")
    weeks = ["Requirement Analysis (W1-2)", "Database and System Design (W1-2)", "Authentication and Role Management (W2-3)",
             "Course Authoring and Content Modules (W2-4)", "Admin Approval and Pricing Workflow (W3-4)",
             "Enrollment, Cart, Coupons (W4-5)", "bKash Checkout and Refunds (W5-6)",
             "Assessment: Quiz Auto-Grading, Assignments, Due Dates (W5-7)", "Progress Tracking and Certification (W7-8)",
             "Instructor Access-Request and OTP Onboarding (W8-9)", "Notifications (W9)", "Roster and Reporting/PDF (W9-10)",
             "Admin/Instructor Content-Preview Access (W10)", "Testing and Debugging (continuous, W1-12)",
             "Final Refinement and Documentation (W11-12)"]
    for w in weeks:
        add_bullet(doc, w)
    add_para(doc, "Table 6.6 (Project Task Scheduling) summarizes this sequence: development started with requirement analysis and database/system design, followed by authentication and role management. Course authoring, content structuring, and the Admin approval-and-pricing workflow were developed next, followed by enrollment, coupons, and bKash checkout. Assessment, progress tracking, and certification followed. The instructor access-request and OTP onboarding flow — the most security-sensitive module — was developed and subsequently hardened after the core platform was stable. Testing and debugging were performed continuously throughout, consistent with the project's live-verification discipline.")

    add_heading2(doc, "6.10 Project Planning Summary")
    add_para(doc, "Based on the identified functions, EduLearn contains 105 transaction-function UFP, 105 internal logical data-function UFP, and 10 external interface-function UFP, totaling 220 UFP. With a Total Degree of Influence of 50, the calculated Value Adjustment Factor is 1.15, resulting in 253 Adjusted Function Points.")
    add_para(doc, "Using the selected productivity assumption of 12.5 person-hours per Function Point, the theoretical development effort is approximately 3,163 person-hours, or 16 person-months. This figure represents estimated human effort under a generic industry productivity assumption and does not indicate the actual calendar duration of this practicum, which was completed by a single student in considerably less time through focused, incremental, continuously-verified development.")

    # ============================================================ CHAPTER 7
    add_heading1(doc, "Chapter 7. System Design")
    add_para(doc, "System design defines the overall structure, components, data flow, database organization, and user interface of EduLearn. The design phase translates the requirements identified in Chapter 3 into a structured technical solution. EduLearn follows a layered web application architecture based on ASP.NET Core MVC, a static-service-based business logic layer, Entity Framework Core for data access, and Microsoft SQL Server for persistent storage.")

    add_heading2(doc, "7.1 System Architectural Design")
    add_para(doc, "EduLearn uses a layered architecture to separate request handling, business logic, data access, and external service integration. The main application is developed using ASP.NET Core MVC (.NET 10) and C#. Controllers receive user requests and coordinate the required work, while calculation-heavy or cross-cutting business logic is factored into static Service classes (QuizGrader, CourseProgressCalculator, CourseRosterService, CourseReportService, ReportPdfService, CertificateService, InvoiceService, CouponService, FileUploadService) so that the same logic can be reused from more than one controller — most visibly, CourseRosterService and CourseReportService are called identically from both InstructorController and AdminController.")
    add_para(doc, "The system also uses a hosted background service (DeadlineReminderBackgroundService) for automated, time-based operations. External integrations include Google OAuth 2.0, the bKash tokenized checkout API, and SMTP email delivery through MailKit.")
    add_figure(doc, "fig_7_1_architecture.png", "Figure 7.1 System Architecture of EduLearn.")
    add_para(doc, "Figure 7.1 illustrates the overall architecture of EduLearn, showing the interaction among users, the web interface, ASP.NET Core MVC, the Service Layer, background services, external services, Entity Framework Core, and the SQL Server database.")

    add_heading2(doc, "7.2 System Internal Design")
    add_para(doc, "The internal design describes the major user-facing interfaces through which users interact with EduLearn. The application uses Razor Views with HTML, CSS, Bootstrap 5, and JavaScript to provide a responsive interface. Different roles have separate dashboards and navigation based on their responsibilities. The following pages are represented by live screenshots of the running application.")
    screens = [
        ("7.2.1 Login Page", "Figure 7.2 Login Page.", "Navigate to /Identity/Account/Login and capture the full page."),
        ("7.2.2 Admin Dashboard", "Figure 7.3 Admin Dashboard.", "Log in as Admin and capture /Admin/Admin (the dashboard landing page)."),
        ("7.2.3 Instructor Dashboard (My Courses)", "Figure 7.4 Instructor Dashboard (My Courses).", "Log in as Instructor and capture /Instructor (My Courses)."),
        ("7.2.4 Student Dashboard (My Enrollments)", "Figure 7.5 Student Dashboard (My Enrollments).", "Log in as Student and capture /Course/MyEnrollments."),
        ("7.2.5 Course Catalog and Course Details Page", "Figure 7.6 Course Catalog and Course Details Page.", "Capture /Course/Index, then a single course's /Course/Details/{id}."),
        ("7.2.6 Content Authoring Interface (Manage Content)", "Figure 7.7 Course Content Authoring Interface (Manage Content).", "As Instructor, open Manage Content for one of your own courses."),
        ("7.2.7 Lesson Viewer with Assignment and Quiz", "Figure 7.8 Lesson Viewer with Assignment and Quiz.", "As Student, open a lesson that has both an assignment and a quiz attached."),
        ("7.2.8 Quiz-Taking Interface", "Figure 7.9 Quiz-Taking Interface.", "As Student, click Take Quiz on any lesson's quiz."),
        ("7.2.9 Cart and Checkout (bKash) Interface", "Figure 7.10 Cart and Checkout (bKash) Interface.", "As Student, add a paid course to Cart and open the bKash checkout screen."),
        ("7.2.10 Instructor Student Roster and Activity Report", "Figure 7.11 Instructor Student Roster and Activity Report.", "As Instructor, open Students, then Report, for one of your courses."),
        ("7.2.11 Admin Course Overview (Instructor Details, Activity Timeline, Roster)", "Figure 7.12 Admin Course Overview (Instructor Details, Activity Timeline, Roster).", "As Admin, open Manage Courses → Overview on any course."),
        ("7.2.12 Admin Revenue and Analytics Reports", "Figure 7.13 Revenue Report Interface.", "As Admin, open the Revenue Report page."),
        ("7.2.13 Admin Coupon and Payment Management", "Figure 7.14 Admin Coupon and Payment Management Interface.", "As Admin, open Coupons, then Payments."),
    ]
    for h, cap, instr in screens:
        add_heading3(doc, h)
        add_screenshot_placeholder(doc, cap, instr)

    add_heading2(doc, "7.3 Data Flow Diagram (DFD)")
    add_para(doc, "A Data Flow Diagram (DFD) represents how data moves between external entities, system processes, and data stores. EduLearn is represented through multiple levels of DFD: a Context Level Diagram, a Level 1 DFD decomposing the system into major functional processes, and Level 2 DFDs decomposing selected processes further.")

    add_heading3(doc, "7.3.1 Context Level Diagram (Level 0)")
    add_para(doc, "The Context Level DFD represents EduLearn as a single central process and shows its interaction with its major external entities: Student, Instructor, Admin, Google OAuth, and the bKash Payment Gateway.")
    add_figure(doc, "fig_7_15_context_dfd.png", "Figure 7.15 Context Level Diagram (Level 0).")

    add_heading3(doc, "7.3.2 Level 1 Data Flow Diagram")
    add_para(doc, "The Level 1 DFD decomposes EduLearn into its major functional processes: Authentication and Instructor Onboarding, Course Creation and Approval, Enrollment and Payment, Lesson/Assignment/Quiz Delivery, Notification Processing, Instructor and Admin Reporting, and Certificate and Receipt Generation.")
    add_figure(doc, "fig_7_16_level1_dfd.png", "Figure 7.16 Level 1 Data Flow Diagram.")

    lvl2 = [
        ("7.3.3 Level 2 DFD – Authentication and Instructor Onboarding", "fig_7_17_dfd_auth_onboarding.png", "Figure 7.17 Level 2 DFD – Authentication and Instructor Onboarding.",
         "Handles Student self-registration (with OTP-verified email) and the full instructor pipeline: access request (Google/email) → Admin approval → rate-limited one-time code → application form → Admin review."),
        ("7.3.4 Level 2 DFD – Course Creation and Approval", "fig_7_18_dfd_course_approval.png", "Figure 7.18 Level 2 DFD – Course Creation and Approval.",
         "Handles Instructor course/content authoring, the mandatory Admin approval-and-pricing gate, and re-submission to Pending on any subsequent edit."),
        ("7.3.5 Level 2 DFD – Enrollment and Payment", "fig_7_19_dfd_enrollment_payment.png", "Figure 7.19 Level 2 DFD – Enrollment and Payment.",
         "Handles free/paid enrollment, cart, coupon validation, bKash checkout, and Admin-issued refunds."),
        ("7.3.6 Level 2 DFD – Lesson, Assignment, and Quiz Delivery", "fig_7_20_dfd_content_delivery.png", "Figure 7.20 Level 2 DFD – Lesson, Assignment, and Quiz Delivery.",
         "Handles lesson viewing and completion, quiz attempts and auto-grading, and assignment submission — each of the latter two gated by an independently-checked due date."),
        ("7.3.7 Level 2 DFD – Notification Processing", "fig_7_21_dfd_notification.png", "Figure 7.21 Level 2 DFD – Notification Processing.",
         "Handles creation of Notification rows for course approval/rejection, new enrollment, new access request, and application decisions, plus read/unread state."),
        ("7.3.8 Level 2 DFD – Instructor and Admin Reporting", "fig_7_22_dfd_reporting.png", "Figure 7.22 Level 2 DFD – Instructor and Admin Reporting.",
         "Handles roster and weekly/monthly activity-report aggregation (shared logic, different access scope) and Admin's platform-wide Revenue/Analytics reports."),
        ("7.3.9 Level 2 DFD – Certificate and Receipt Generation", "fig_7_23_dfd_certificate.png", "Figure 7.23 Level 2 DFD – Certificate and Receipt Generation.",
         "Handles certificate eligibility checking (100% lesson completion) and PDF generation, and payment-receipt PDF generation."),
    ]
    for h, fig, cap, desc in lvl2:
        add_heading3(doc, h)
        add_para(doc, desc)
        add_figure(doc, fig, cap)

    add_heading2(doc, "7.4 Entity Relationship Diagram (ERD)")
    add_para(doc, "An Entity Relationship Diagram (ERD) represents the logical structure of the database and the relationships among EduLearn's entities. The database is designed using normalized relational structures, with a deliberate, per-relationship choice between Cascade and Restrict delete behavior.")
    add_figure(doc, "fig_7_24_erd.png", "Figure 7.24 Entity Relationship Diagram of EduLearn.", width_in=5.8)

    for h, txt in [
        ("7.4.1 User and Role Relationship", "ASP.NET Core Identity's role tables associate each ApplicationUser with one of Student, Instructor, or Admin. ApplicationUser additionally carries IsApproved/IsRejected/IsActive flags used to gate an Instructor's access before full approval."),
        ("7.4.2 Course and Enrollment Relationship", "A Course can have many Enrollments; an Enrollment belongs to exactly one Course and one Student. The Enrollment.Status field is the single source of truth for access — Course-owned content cascades on Course deletion, but the FK back to the student ApplicationUser uses Restrict."),
        ("7.4.3 Course Content Relationship", "Course content forms a strict hierarchy: a Course has many Modules, a Module has many Lessons, and a Lesson may have many Assignments and many Quizzes. All of these are configured Cascade on delete, since they are wholly owned by their parent Course."),
        ("7.4.4 Assessment Result Relationships", "A Quiz can have many QuizResults (updated in place on retake); an Assignment can have many AssignmentSubmissions; a Lesson can have many LessonProgress rows. Each of these FKs back to the student ApplicationUser uses Restrict."),
        ("7.4.5 User and Notification Relationship", "A user can receive many Notifications, created for events including course approval/rejection, new enrollment, and access-request/application decisions, and carrying an IsRead flag."),
        ("7.4.6 User and Review Relationship", "A Student can write at most one Review per Course they have completed."),
        ("7.4.7 Instructor Access Request Relationship", "InstructorAccessRequest is deliberately not tied by foreign key to the eventual ApplicationUser it leads to, and carries its own lifecycle fields — Status, OtpCode, OtpExpiresAt, OtpAttempts, AccessToken, and IsConsumed. RejectedApplicationArchive is similarly a standalone table with no foreign key back to ApplicationUser."),
        ("7.4.8 Coupon and Payment Relationship", "A Coupon can have many CouponRedemptions; a Payment belongs to one Enrollment's Course and Student and records the transaction ID, amount, and status."),
    ]:
        add_heading3(doc, h)
        add_para(doc, txt)

    add_heading3(doc, "7.4.9 Database Design Considerations")
    add_para(doc, "Data Normalization: related information is stored in separate entities rather than duplicated. Referential Integrity and Deliberate Delete Behavior: every foreign key in ApplicationDbContext has an explicit, deliberately chosen delete behavior — Cascade for anything wholly owned by a Course, and Restrict for any foreign key from a student-owned record back to ApplicationUser — so deleting a user account can never silently cascade into deleting unrelated course content, and deleting a course can never silently delete a student's account. Historical/Archive Data: RejectedApplicationArchive demonstrates a standalone archive pattern. Audit Information: timestamp fields support both the audit trail and the activity-timeline/reporting features.")

    add_heading2(doc, "7.5 Design Summary")
    add_para(doc, "The system design of EduLearn combines a layered application architecture, role-based user interfaces, structured data flows, and a normalized relational database with deliberate, relationship-by-relationship delete-behavior choices. The architecture separates presentation, business logic, background processing, external integrations, and data access. The DFDs describe how information moves through EduLearn's major processes, while the ERD demonstrates how the database's entities relate, including two deliberately standalone, non-foreign-keyed tables used specifically to keep sensitive or historical state decoupled from live account records.")

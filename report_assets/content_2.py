# -*- coding: utf-8 -*-
"""Chapters 4-5."""
from docx_engine import *


def build(doc):
    # ============================================================ CHAPTER 4
    add_heading1(doc, "Chapter 4. Analysis")
    add_para(doc, "Analysis modeling provides a structured representation of the functional and behavioral aspects of a software system. It combines textual descriptions with graphical models to explain how users interact with the system, how activities are performed, and how different system processes are connected.")
    add_para(doc, "For EduLearn, the analysis phase focuses on the activities performed by the three roles and on the interaction between users and the platform. Since the system follows a role-based architecture, separate activity diagrams have been prepared for the Admin, Instructor, and Student, plus a dedicated diagram for the instructor access-request and onboarding flow, since it is the most security-sensitive and multi-step process in the system. Swimlane and sequence diagrams are then used to represent interaction between actors and system components for the course-approval-and-enrollment process and the quiz auto-grading process.")

    add_heading2(doc, "4.1 Activity Diagrams")
    add_para(doc, "An activity diagram is a behavioral UML diagram that represents the flow of activities within a system or a particular business process, showing the starting point, the activities performed, decision points, alternative flows, and completion.")
    add_para(doc, "After successful authentication, each EduLearn user is redirected to a role-specific landing area (Admin Dashboard, Instructor “My Courses,” or the public course catalog for Students), with role-appropriate navigation available throughout.")

    add_heading3(doc, "4.1.1 Activity Diagram of Admin Activities.")
    add_para(doc, "The Admin Activity Diagram represents the major activities performed by the platform administrator. The process begins with Admin authentication. After successful login, the Admin can navigate to Manage Users, Manage Categories, Pending Courses, Manage Courses, Access Requests, Revenue/Analytics reports, Payments, or Coupons.")
    add_para(doc, "Through Pending Courses, the Admin reviews a newly submitted or edited course and either approves it — choosing Free or a fixed price at that moment — or rejects it with an optional reason emailed to the Instructor. Through Manage Courses, the Admin can edit the price of, or permanently delete, any course regardless of status. Through Access Requests, the Admin approves or denies instructor onboarding requests; approval triggers the one-time-code email.")
    add_figure(doc, "fig_4_1_activity_admin.png", "Figure 4.1 Activity Diagram of Admin Activities.")
    add_para(doc, "Figure 4.1 illustrates the major activities performed by the Admin.")

    add_heading3(doc, "4.1.2 Activity Diagram of Instructor Activities.")
    add_para(doc, "The Instructor Activity Diagram represents the workflow followed by an Instructor after successful authentication. After login, the Instructor lands on “My Courses,” listing their own courses with status badges (Pending/Approved/Rejected).")
    add_para(doc, "From here, the Instructor can create a new course (submitted as Pending, with no price field), edit an existing course (which returns it to Pending for re-review), or manage a course's content: adding Modules, Lessons, Assignments, and Quizzes. The Instructor can review student feedback and reply to it, review quiz results across their courses, and — for any one course — open its Students roster or generate a weekly/monthly activity report.")
    add_figure(doc, "fig_4_2_activity_instructor.png", "Figure 4.2 Activity Diagram of Instructor Activities.")
    add_para(doc, "Figure 4.2 illustrates the major activities performed by the Instructor.")

    add_heading3(doc, "4.1.3 Activity Diagram of Student Activities")
    add_para(doc, "The Student Activity Diagram represents the core learning workflow of EduLearn. After login (or while browsing anonymously), the Student can search and filter the course catalog and open a course's details page.")
    add_para(doc, "For a free course, clicking Enroll immediately creates an Active enrollment. For a paid course, enrollment creates a Pending entry in the Student's Cart, where a coupon code may optionally be applied before proceeding to bKash checkout. Once enrolled, the Student opens lessons, marks them complete, attempts quizzes (auto-graded on submission, blocked once the due date has passed), and submits assignment files. Once every lesson in a course is complete, a certificate becomes downloadable, and the Student can write a review.")
    add_figure(doc, "fig_4_3_activity_student.png", "Figure 4.3 Activity Diagram of Student Activities.")
    add_para(doc, "Figure 4.3 illustrates the major activities performed by the Student.")

    add_heading3(doc, "4.1.4 Activity Diagram of the Instructor Access-Request and Onboarding Flow")
    add_para(doc, "This activity diagram represents the complete, multi-actor process by which a member of the public becomes an approved Instructor — the most security-sensitive workflow in EduLearn, and the one most revised during development.")
    add_para(doc, "The process begins when a visitor opens /Apply and chooses Continue with Google (triggering an OAuth challenge; the system reads only the verified email claim and immediately signs the transient external-login cookie back out, so this never becomes a persistent site login) or Continue with Email. Either path creates a Pending InstructorAccessRequest row and notifies every Admin. An Admin reviews the request and either denies it or approves it — approval generates a cryptographically random 6-digit code and a separate unguessable access token, sets a 24-hour expiry, and emails both together.")
    add_para(doc, "The applicant clicks the link, submits the code, and the system checks — on every submission, not just the first — whether the request is still Approved, not already consumed, not expired, and under the maximum of 5 incorrect attempts. A correct code redirects to the actual application form, unchanged from before this flow existed. On submission, the system creates the account, marks the request consumed, and notifies every Admin of the new full application. Finally, an Admin reviews the full application and approves (emailing a self-service password-setup link) or rejects (archiving and deleting the account) it.")
    add_figure(doc, "fig_4_4_activity_access_request.png", "Figure 4.4 Activity Diagram of the Instructor Access-Request and Onboarding Flow.")
    add_para(doc, "Figure 4.4 illustrates the complete instructor-onboarding workflow, from the initial Google/email request through Admin approval, one-time-code verification, full application, and final Admin review.")

    add_heading2(doc, "4.2 Swim Lane Diagram")
    add_para(doc, "A swim lane diagram is a specialized form of an activity diagram that divides a workflow into separate lanes according to the actors or system components responsible for performing each activity. It is useful for processes involving multiple participants because it clearly identifies who performs each activity and how responsibility transfers between them.")
    add_heading3(doc, "4.2.1 Swim Lane Diagram of Course Approval and Enrollment")
    add_para(doc, "The swim lane diagram represents the interaction between the Instructor, EduLearn System, Admin, and Student across the full path from course authoring to paid enrollment.")
    add_para(doc, "The process begins when the Instructor creates a course and its content; the System stores it as Pending. The Admin reviews it and approves it, setting its price at that moment (or rejects it, with the reason emailed back to the Instructor). Once Approved, the course becomes visible in the public catalog. A Student browses to it; if free, the System creates an Active enrollment immediately; if paid, the System creates a Pending enrollment in the Student's Cart. The Student optionally applies a coupon and proceeds to bKash checkout; on a successful payment callback, the System activates the enrollment.")
    add_figure(doc, "fig_4_5_swimlane.png", "Figure 4.5 Swim Lane Diagram of Course Approval and Enrollment.")
    add_para(doc, "Figure 4.5 illustrates the distribution of activities among the Instructor, System, Admin, and Student across course authoring, approval and pricing, and paid enrollment.")

    add_heading2(doc, "4.3 Sequence Diagrams")
    add_para(doc, "A sequence diagram represents the chronological interaction between actors and system components, showing how a request moves through different components and how each responds during a particular operation.")
    add_heading3(doc, "4.3.1 Sequence Diagram of Course Approval Flow")
    add_para(doc, "The process begins when the Admin opens Pending Courses and submits an approval decision — Free, or a specific price — through a confirmation modal. The request is processed by AdminController.ApproveCourse, which sets the course's Price and flips its Status to Approved, clears any prior rejection reason, and persists the change through Entity Framework Core. NotificationService then creates a Notification row for the course's Instructor.")
    add_figure(doc, "fig_4_6_seq_course_approval.png", "Figure 4.6 Sequence Diagram of Course Approval Flow.")
    add_para(doc, "Figure 4.6 illustrates the sequence of interactions involved in an Admin approving and pricing a course.")
    add_heading3(doc, "4.3.2 Sequence Diagram of Quiz Attempt and Auto-Grading Flow")
    add_para(doc, "The Student opens a lesson and selects Take Quiz; CourseController.TakeQuiz first checks the quiz's due date and, if it has passed, redirects back to the lesson with an explanatory message instead of rendering the quiz. On submission, CourseController.SubmitQuiz re-checks the due date independently, then passes the quiz and the student's selected option IDs to the static QuizGrader service, which compares selections against the stored correct answers and returns a score and pass/fail outcome. The controller then either updates the student's existing QuizResult row in place (on a retake) or inserts a new one.")
    add_figure(doc, "fig_4_7_seq_quiz_attempt.png", "Figure 4.7 Sequence Diagram of Quiz Attempt and Auto-Grading Flow.")
    add_para(doc, "Figure 4.7 illustrates the sequence of interactions involved in a quiz attempt, including the due-date check performed independently at both the display and submission steps.")

    # ============================================================ CHAPTER 5
    add_heading1(doc, "Chapter 5. Project Management")
    add_para(doc, "Project management is an essential part of software development because it provides a structured approach for planning, organizing, monitoring, and controlling project activities. Effective project management helps ensure that the system is developed within the available time and resources while maintaining the required quality and functionality.")
    add_para(doc, "For EduLearn, project management focused on identifying potential risks, analyzing their probability and impact, and defining mitigation and management strategies. Because this project involved third-party OAuth and payment gateway integration, real money-handling logic, and a locally-hosted database, several of the risks identified below were not merely theoretical — they were actually encountered during development, and this chapter documents both the anticipated risk and, where applicable, what actually happened and how it was resolved.")

    add_heading2(doc, "5.1 Risk Identification")
    add_para(doc, "Risk identification is the process of recognizing potential events or conditions that may negatively affect a software project's development, functionality, security, schedule, or quality. For EduLearn, risks were identified by considering the system architecture, third-party integrations (Google OAuth, bKash), the local database environment, security requirements, and evolving requirements.")
    for b in ["Google OAuth 2.0 integration issues in the instructor access-request flow",
              "bKash payment gateway integration and callback-handling issues",
              "Local SQL Server Express reliability and connectivity issues during development",
              "Unauthorized access or incorrect role-based permissions",
              "File upload and validation issues (résumés, submissions, profile pictures)",
              "Email/SMTP notification delivery failures, including incorrect link generation",
              "Database performance and data-integrity issues as roster/report queries grew more complex",
              "Changing requirements during development (most notably, course-pricing authority)"]:
        add_bullet(doc, b)
    add_para(doc, "Probability Scale — Very Low: <10%; Low: 10–25%; Moderate: 25–50%; High: 50–75%; Very High: >75%.", align="left", indent_first=False)
    add_para(doc, "Risk Effect Scale — Catastrophic: may cause major system failure or prevent successful completion. Serious: may significantly delay development or affect an important function. Tolerable: may cause manageable delays. Insignificant: minimal effect on progress.", align="left", indent_first=False)

    add_heading2(doc, "5.2 Risk Analysis")
    add_table(doc, [
        ["ID", "Possible Risk", "Type", "Prob.", "Effect", "RMMM Plan"],
        ["R01", "Google OAuth integration may be misconfigured or fail.", "Technical / External", "Moderate", "Serious", "Gate the button/challenge behind a config check; verify generated URLs live."],
        ["R02", "bKash checkout/callback may fail to activate an enrollment or reconcile a payment.", "Technical / External", "Moderate", "Serious", "Centralize payment handling; verify enrollment status against the live DB."],
        ["R03", "Local SQL Server Express may become slow/unresponsive.", "Environment", "High", "Serious", "Diagnose via service/memory checks; restart affected process/service."],
        ["R04", "Incorrect role-based authorization may allow unauthorized access.", "Security", "Low", "Catastrophic", "Enforce authorization at the controller/action level; verify per role live."],
        ["R05", "Uploaded files may contain unsupported types or exceed size limits.", "Technical / Security", "Moderate", "Tolerable", "Centralize validation in FileUploadService; reject before persistence."],
        ["R06", "Email notifications may fail, or a generated link may be malformed.", "Integration", "Moderate", "Serious", "Centralize email through IEmailService; verify links by following them."],
        ["R07", "Roster/report queries may become slow (N+1) as data grows.", "Database", "Low", "Serious", "Batch aggregation in CourseRosterService/CourseReportService."],
        ["R08", "Business requirements may change during development.", "Requirements", "Moderate", "Tolerable", "Evaluate blast radius before implementing; keep logic in one owned service."],
    ], caption="Table 5.1 Risk Analysis Table for EduLearn.", size=9, col_widths=[0.38, 1.81, 0.95, 0.57, 0.67, 1.52])
    add_para(doc, "The analysis shows that the most significant realized risk during this practicum was environmental (R03 — local database reliability), not purely a coding defect: several development sessions were interrupted by SQL Server Express becoming unresponsive under memory pressure or after a forced process kill left stale connections behind. The second most instructive realized risk was R06, where a genuinely subtle ASP.NET Core routing behavior — Url.Action silently inheriting the ambient Area value from the calling controller — produced a broken link in a real, sent email; this was only caught because the resulting page was actually clicked, not because the code “looked correct.”")

    add_heading2(doc, "5.3 Risk Planning")
    add_table(doc, [
        ["ID", "Name of the Risk", "Strategy if the Risk Occurs"],
        ["R01", "Google OAuth Integration Failure", "Confirm client credentials/redirect URI; hide the Google option and keep email available if unconfigured."],
        ["R02", "bKash Checkout/Callback Failure", "Inspect callback payload/status; manually correct enrollment/payment state if inconsistent."],
        ["R03", "SQL Server Express Unresponsive", "Check service status/memory first; restart the service or terminate the stuck process."],
        ["R04", "Unauthorized Role Access", "Review [Authorize] attributes and role checks; retest the affected page as each role."],
        ["R05", "Invalid File Upload", "Reject with a specific validation message; confirm with a real oversized/wrong file."],
        ["R06", "Email/Link Delivery Failure", "Rebuild the link with an explicit route/area override; resend and follow the link end-to-end."],
        ["R07", "Roster/Report Query Performance", "Refactor to batch queries; re-verify against a manual database count."],
        ["R08", "Changing Requirements", "Scope the change tightly; add/update automated tests; retest the full workflow live."],
    ], caption="Table 5.2 Risk Planning Table for EduLearn.", size=9, col_widths=[0.38, 1.62, 3.9])

    add_heading2(doc, "5.4 RMMM Plans")
    add_para(doc, "RMMM stands for Risk Mitigation, Monitoring, and Management. The plans below focus on the risks with the greatest relevance to EduLearn's architecture and the ones actually realized during development.")

    rmmm_data = [
        ("5.4.1 RMMM 01 — Google OAuth Integration", "Table 5.3 RMMM Plan No. 1.", "Google OAuth Integration Failure", "Moderate", "Serious", "R01", "Technical / External Service",
         "The instructor access-request flow depends on Google OAuth 2.0. Missing or invalid client credentials, or an unregistered redirect URI, would prevent the flow from completing.",
         "The scheme is registered only when a Client ID and Secret are present; if absent, the button is hidden and the challenge action redirects safely instead of throwing.",
         "Verified live end-to-end — a real Google account was used to complete the challenge, and the resulting captured email and created access-request row were confirmed in the database.",
         "If credentials are revoked, the email-only fallback keeps onboarding usable while the integration is corrected.",
         "Mitigated and verified working, including the graceful fallback when unconfigured."),
        ("5.4.2 RMMM 02 — bKash Payment Gateway Integration", "Table 5.4 RMMM Plan No. 2.", "bKash Checkout/Callback Failure", "Moderate", "Serious", "R02", "Technical / External Service",
         "Enrollment activation for paid courses depends on a successful bKash agreement and payment callback. A failed or malformed callback could leave a student's payment taken without access activated.",
         "Payment handling is centralized in BkashController/BkashPaymentService; enrollment status transitions are handled as an explicit, single responsibility.",
         "Enrollment and Payment table state was checked directly against the database after test checkouts and refunds, not inferred from the UI alone.",
         "A mismatch is corrected directly via the Admin refund action, designed to always revoke access when it reverts a payment.",
         "Mitigated through centralized payment handling and direct database verification."),
        ("5.4.3 RMMM 03 — Local Database Reliability", "Table 5.5 RMMM Plan No. 3.", "SQL Server Express Unresponsive / Memory Pressure", "High", "Serious", "R03", "Environment / Infrastructure",
         "The local SQL Server Express instance repeatedly became slow or fully unresponsive, on at least one occasion becoming stuck in a “Stop Pending” state that a normal restart could not clear.",
         "Diagnosed via service/process status and system memory checks before assuming an application defect; when stuck, the underlying sqlservr.exe process was identified by PID and terminated directly.",
         "Reconfirmed connectivity with a trivial SELECT 1 query before resuming testing.",
         "Treated as an environmental risk to manage around (restart discipline, memory hygiene) rather than an application defect to fix.",
         "Managed; recurred multiple times and was resolved the same way each time once correctly diagnosed."),
        ("5.4.4 RMMM 04 — Unauthorized Role Access", "Table 5.6 RMMM Plan No. 4.", "Unauthorized Role-Based Access", "Low", "Catastrophic", "R04", "Security",
         "EduLearn contains roles with materially different trust levels, including Admin actions that must never be reachable by a Student or Instructor.",
         "Every sensitive controller/action carries an explicit [Authorize(Roles = \"...\")] attribute; Instructor-scoped actions additionally filter by course ownership in the query itself.",
         "Verified by attempting to reach Instructor-only and Admin-only URLs directly while authenticated as a different role.",
         "Any authorization gap found is closed at the controller/action level first; UI-level hiding is treated as a usability nicety, never the actual security boundary.",
         "Mitigated through consistent role- and ownership-based authorization, verified live per role."),
        ("5.4.5 RMMM 05 — File Upload Validation", "Table 5.7 RMMM Plan No. 5.", "Invalid or Oversized File Upload", "Moderate", "Tolerable", "R05", "Technical / Security",
         "EduLearn accepts several kinds of user-uploaded files — instructor résumés, profile pictures, course thumbnails, and assignment submissions — each with different acceptable types and size limits.",
         "FileUploadService centralizes extension and size validation for every upload path.",
         "Invalid uploads were tested directly and confirmed to be rejected with a specific message.",
         "A rejected upload returns the user to the same form with a clear validation message.",
         "Mitigated through centralized validation."),
        ("5.4.6 RMMM 06 — Email Notification and Link Generation", "Table 5.8 RMMM Plan No. 6.", "Email Delivery / Malformed Link Failure", "Moderate", "Serious", "R06", "Integration / Communication",
         "During development, the instructor-onboarding approval email was found to contain a broken link: Url.Action was called from within the Admin-area controller without an explicit area=\"\" override, so it silently inherited the ambient Admin area and generated an incorrect URL.",
         "The route value is now built with an explicit area=\"\"; every generated link that crosses an Area boundary is treated as needing that explicit override.",
         "The fix was confirmed by regenerating a real approval email and following the resulting link end-to-end.",
         "IEmailService centralizes sending; the in-app Notification system provides a fallback signal.",
         "Mitigated; the specific defect was found and corrected during this practicum through live testing, not code review alone."),
        ("5.4.7 RMMM 07 — Roster and Reporting Query Performance", "Table 5.9 RMMM Plan No. 7.", "Roster/Activity-Report Query Performance", "Low", "Serious", "R07", "Database / Performance",
         "The student roster and weekly/monthly activity report aggregate across Enrollments, LessonProgress, QuizResults, and AssignmentSubmissions for every student in a course — a naive per-student query pattern would not scale.",
         "CourseRosterService and CourseReportService compute per-course aggregates in a small, fixed number of batched queries.",
         "Verified against real seeded course/enrollment/activity data, with report totals cross-checked against a manual database count.",
         "Any new report metric is added as another batched aggregate in the same service.",
         "Mitigated through batched aggregation, confirmed correct against real data."),
        ("5.4.8 RMMM 08 — Changing Requirements (Course Pricing Authority)", "Table 5.10 RMMM Plan No. 8.", "Changing Business Requirement — Course Pricing Authority", "Moderate", "Tolerable", "R08", "Requirements",
         "Course pricing was originally set by the Instructor at course creation. Partway through development, the requirement changed: only an Admin may set pricing, and only at course-approval time.",
         "Because pricing logic was already isolated to specific sections of the Instructor and Admin controllers, the change was implemented and re-verified without touching unrelated modules.",
         "Verified live: an Instructor's create-course form no longer accepts a price, and a crafted request is still ignored server-side rather than merely hidden.",
         "The change was implemented, tested, and confirmed working, including an updated automated regression test.",
         "Managed; implemented, tested, and confirmed working."),
    ]
    for heading, tcap, name, prob, impact, rid, rtype, desc, mit, mon, mgmt, status in rmmm_data:
        add_heading3(doc, heading)
        add_table(doc, [["Item", "Description"], ["Name", name], ["Probability", prob], ["Impact", impact], ["Risk ID", rid], ["Risk Type", rtype]],
                   caption=tcap, size=10, col_widths=[1.28, 4.62])
        add_para(doc, "Description: " + desc)
        add_para(doc, "Mitigation: " + mit)
        add_para(doc, "Monitoring: " + mon)
        add_para(doc, "Management: " + mgmt)
        add_para(doc, "Current Status: " + status)

    add_heading2(doc, "5.5 Risk Management Summary")
    add_para(doc, "EduLearn's development surfaced a mix of conventional software risks (authorization, validation, changing requirements) and two risks that are easy to under-appreciate until they actually happen: local infrastructure reliability (R03) and a subtle, silent routing defect (R06) that produced a real broken link in a real sent email. Both were caught and resolved only because of the project's live-verification discipline — testing the actual running system and its actual database and actual emails, rather than trusting that correct-looking code is correct-behaving code.")
    add_para(doc, "This risk-management approach helped maintain EduLearn's reliability while allowing new requirements (most notably, the pricing-authority change) to be incorporated without unnecessary architectural disruption.")

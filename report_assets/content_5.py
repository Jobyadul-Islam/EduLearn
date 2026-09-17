# -*- coding: utf-8 -*-
"""Chapters 9-10, References, Plagiarism Report."""
from docx_engine import *


def build(doc):
    add_heading1(doc, "Chapter 9. Ethical Considerations")
    add_para(doc, "Ethical considerations are an important part of the software development lifecycle, particularly for a system that collects, processes, and stores personal information, academic records, and payment data. EduLearn handles information such as user names, email addresses, résumés, course content, quiz answers and results, assignment submissions, payment and coupon-redemption records, and reviews. Ethical responsibilities related to privacy, security, fairness, transparency, accountability, and responsible use must therefore be considered throughout the system's development and operation.")

    add_heading2(doc, "9.1 Ethical Considerations in the Software Development Process")
    for h, txt in [
        ("9.1.1 Data Privacy and Security", "EduLearn stores personal information and financially sensitive information. Passwords are hashed via ASP.NET Core Identity's default hasher, never stored or transmitted in plain text — reinforced when the instructor-approval email was changed to send a secure, single-use password-setup link rather than a real password in plain text. Role- and ownership-based authorization restrict access so a user can see only the information relevant to their role. Google OAuth is used narrowly — solely to read a verified email address during instructor onboarding — and is explicitly prevented from ever becoming a persistent login."),
        ("9.1.2 Responsible Handling of Student and Course Data", "Student progress, quiz results, and assignment submissions are personal academic records. EduLearn restricts this information by role: a Student sees only their own results; an Instructor sees results only for their own courses' students; an Admin can see any course's data, but that capability is itself gated behind the Admin role. The system also deliberately separates a rejected instructor applicant's live account (which is deleted) from a permanent, access-restricted archive of what they submitted."),
        ("9.1.3 Fairness and Non-Discrimination", "Quiz grading is applied identically and mechanically by QuizGrader to every student's submission, based solely on stored correct answers. Course approval and pricing decisions are made by an Admin under one consistent workflow. Deadline enforcement applies the same rule to every student on a given assignment or quiz, with no exceptions built into the code."),
        ("9.1.4 Transparency and Accountability", "EduLearn maintains timestamped records of enrollment, lesson completion, quiz attempts, assignment submissions, course approval/rejection decisions (with reasons), and payment/refund actions. The Instructor and Admin activity-timeline and reporting features exist specifically to make platform activity visible and auditable rather than opaque. Admin actions that materially affect a user are all logged as Notification events to the affected user."),
        ("9.1.5 Responsible Use of Payment and Financial Information", "EduLearn's payment data is used strictly to reconcile access against payment and to support legitimate reporting and dispute resolution. Access to the Payments ledger and refund action is restricted to Admin. bKash's own tokenized checkout API means EduLearn never directly handles or stores raw payment credentials."),
        ("9.1.6 Privacy in Google OAuth Integration", "Because Google OAuth could, in principle, be used to retrieve more identity information than is actually needed, EduLearn's implementation deliberately reads only the verified email claim and discards everything else, immediately signing out the transient external-login cookie regardless of whether the flow succeeded."),
        ("9.1.7 Professional Responsibility", "Professional responsibility was considered throughout EduLearn's development. The system was built using structured practices — MVC architecture, a dedicated Service Layer, database migrations, input and file validation, and both automated and live testing — and an explicit discipline of never claiming a feature “works” without verifying it against the actual running system."),
    ]:
        add_heading3(doc, h)
        add_para(doc, txt)

    add_heading2(doc, "9.2 Sustainability in the Software Development Process")
    for h, txt in [
        ("9.2.1 Economic Sustainability", "EduLearn is built on freely available, open-source-friendly technologies, keeping licensing costs low. Centralizing course delivery, enrollment, payment, and reporting reduces the manual, spreadsheet-based overhead that a fragmented toolchain would otherwise require."),
        ("9.2.2 Technical Sustainability", "The layered MVC-plus-Service-Layer architecture allows a feature to be modified or extended without a wide-reaching rewrite — demonstrated in practice when course-pricing authority moved from Instructor to Admin mid-project, and when the student-roster/reporting feature was added and immediately reused, unmodified, by both the Instructor and Admin sides of the application."),
        ("9.2.3 Social Sustainability", "EduLearn supports more equitable, transparent access to course content: free-preview lessons let a prospective student evaluate a paid course before committing money, and coupon codes support discounted access. Deadline enforcement and automatic grading remove opportunities for inconsistent, manually-applied leniency."),
        ("9.2.4 Maintainability and Future Sustainability", "The separation of Controllers, Services, ViewModels, and Models supports ongoing maintenance. New features can be developed using the existing architectural pattern — as demonstrated by the coupon system, the roster/reporting feature, and the hardened instructor-onboarding flow."),
    ]:
        add_heading3(doc, h)
        add_para(doc, txt)

    add_heading2(doc, "9.3 Ethical and Sustainability Summary")
    add_para(doc, "EduLearn was developed with consideration for data privacy, security, fairness, transparency, accountability, professional responsibility, and long-term sustainability. Password hashing, role- and ownership-based authorization, narrowly-scoped OAuth use, and rate-limited/expiring one-time codes protect user and platform data. Consistent, mechanical grading and deadline enforcement support fairness. Timestamped, auditable records and Notification-driven communication support transparency and accountability.")
    add_para(doc, "From a sustainability perspective, the layered MVC-plus-Service architecture, migration-tracked SQL Server database, and disciplined, incremental development approach provide a maintainable foundation, evidenced concretely by the mid-project pricing-authority change and the coupon/roster/reporting features added without disrupting existing functionality.")

    # ============================================================ CHAPTER 10
    add_heading1(doc, "Chapter 10. Conclusion")
    add_heading2(doc, "10.1 Preface")
    add_para(doc, "This concluding chapter reflects on the practicum experience and summarizes EduLearn's development. The project focused on designing and implementing a structured, role-based e-learning platform covering course delivery, enrollment and payment, assessment, certification, instructor onboarding, and reporting.")
    add_para(doc, "The practicum provided an opportunity to apply theoretical software engineering knowledge to a genuinely complete, non-trivial application. The development process covered requirement analysis, system design, database design, implementation, testing, and ethical evaluation, and provided practical experience in ASP.NET Core MVC, C#, Entity Framework Core, SQL Server, authentication and authorization, third-party OAuth and payment gateway integration, background services, PDF generation, and — distinctively — a discipline of continuous, live, evidence-based verification rather than code-review-only confidence.")

    add_heading2(doc, "10.2 Practicum and Its Value")
    add_para(doc, "The practicum provided significant value in both technical skill development and professional discipline. Through EduLearn's development, practical experience was gained in ASP.NET Core MVC application development, C# programming, Entity Framework Core migrations, SQL Server database design, role-based authentication and authorization, and a static-service-based application architecture.")
    add_para(doc, "A particularly valuable part of the practicum was implementing security-conscious, multi-step workflows correctly: the instructor access-request pipeline required getting rate limiting, expiry, and idempotent request handling right simultaneously, and directly exposed a real, subtle ASP.NET Core routing defect that would have gone unnoticed without deliberately following the actual generated link rather than trusting the code that produced it.")
    add_para(doc, "The practicum also strengthened understanding of payment-adjacent system design: correctly linking an Enrollment's status to a Payment's status, and correctly reversing that link on refund, required careful attention to state consistency in a way that a purely content-delivery feature would not have demanded. Implementing due-date enforcement for both assignments and quizzes — and specifically checking it independently at both the page-render and the submission-handling steps — reinforced that a UI-only restriction is not a real restriction at all.")
    add_para(doc, "Another significant learning outcome was the value of factoring business logic into reusable Service classes: CourseRosterService and CourseReportService being written once and correctly reused, unmodified, from both the Instructor and Admin controllers demonstrated concretely how a clean separation between “who is allowed to call this” and “what does this actually compute” pays off the moment a second caller appears.")
    add_para(doc, "The internship experience at Touch & Solve Technologies Ltd. complemented this independent development work by providing additional exposure to professional web-development practice, reinforcing the technical discipline applied throughout EduLearn's design and implementation.")
    add_para(doc, "Overall, the practicum provided valuable experience across the complete web application development lifecycle, strengthening technical, analytical, database-design, debugging, testing, documentation, and problem-solving skills, and provided genuine insight into professional, evidence-based software development practice.")

    add_heading2(doc, "10.3 Future Plan")
    for h, txt in [
        ("Live/Automated Google OAuth Verification in CI", "the Google OAuth path currently requires a manually-configured Client ID/Secret and a real Google account; a mocked or sandboxed OAuth flow would allow this path to be included in automated regression testing."),
        ("Real-Time Notifications", "the current notification system is populated on page load; a future version could push updates in real time (e.g., via SignalR)."),
        ("Advanced Analytics", "more advanced dashboards, trend charts over longer periods, and additional per-instructor or per-category performance indicators could be introduced."),
        ("Mobile Application", "a dedicated mobile client could let Students and Instructors access core EduLearn functions conveniently from a phone."),
        ("Cloud Deployment and Scalability", "the system can be deployed to managed cloud infrastructure with a managed database service, centralized file storage, automated backups, and monitoring."),
        ("Additional Payment Gateways", "support for gateways beyond bKash would broaden the platform's applicability outside markets where bKash is the dominant option."),
        ("Advanced Security", "multi-factor authentication, stronger password policies, and more granular audit logging could be added for production-grade deployment."),
        ("Expanded Automated Test Coverage", "extending automated coverage to the instructor-onboarding pipeline's Google OAuth path and to more of the payment/refund flow would further reduce reliance on manual live verification."),
    ]:
        p = doc.add_paragraph()
        r = p.add_run(h + ": "); set_run_font(r, 12, bold=True)
        r2 = p.add_run(txt); set_run_font(r2, 12)
        p.paragraph_format.space_after = Pt(8)
        p.paragraph_format.first_line_indent = Inches(0.3)

    add_heading2(doc, "10.4 Conclusion")
    add_para(doc, "In conclusion, EduLearn represents a structured, maintainable, and functionally complete web-based e-learning platform developed to support course delivery, assessment, enrollment, payment, and administration. The system was developed using ASP.NET Core MVC, C#, Entity Framework Core, and SQL Server, with Bootstrap, HTML, CSS, JavaScript, and Chart.js supporting the user interface. Its architecture combines MVC components, ViewModels, a Service Layer, migration-tracked database management, a background service, external integrations, file handling, notifications, and PDF reporting.")
    add_para(doc, "The system provides role-based functionality for Student, Instructor, and Admin users, with access restrictions grounded in server-side authorization and, where relevant, resource ownership rather than interface convention alone. The course lifecycle — authoring, Admin approval and pricing, structured content, and enforced deadlines — provides a centralized, consistent approach to delivering and assessing course material.")
    add_para(doc, "The instructor-onboarding pipeline — a Google OAuth or email access request, Admin approval, a rate-limited and time-limited one-time code, and only then the actual application — demonstrates a deliberately more secure alternative to a manually-issued PIN, and its development directly surfaced and led to fixing a real, otherwise-invisible ASP.NET Core routing defect, reinforcing the value of live verification over code review alone.")
    add_para(doc, "From a technical perspective, this project provided practical experience in authentication, authorization, database normalization with deliberate delete-behavior design, third-party OAuth and payment-gateway integration, background processing, file validation, PDF generation, automated and live testing, debugging, and software documentation.")
    add_para(doc, "The system also has defined limitations within its current scope: the Google OAuth path is not yet covered by automated regression tests, and the platform currently targets a single payment gateway (bKash). These limitations provide clear, scoped opportunities for future enhancement without requiring changes to the existing architectural foundation.")
    add_para(doc, "From the perspective of the practicum, developing EduLearn provided a valuable opportunity to transform theoretical software engineering knowledge into a working, defensible system — one whose correctness claims in this report are backed by actual, reproducible verification rather than by the code merely “looking right.” Overall, the developed EduLearn platform successfully establishes a centralized, role-based, secure, and extensible e-learning system, while providing a strong, evidence-verified foundation for future enhancement.")

    # ============================================================ REFERENCES
    add_heading1(doc, "References")
    refs = [
        "Freeman, A. (2024) Pro ASP.NET Core 8: Develop Cloud-Ready Web Applications Using MVC, Blazor, and Razor Pages. 10th edn. Berkeley: Apress.",
        "Elmasri, R. and Navathe, S.B. (2016) Fundamentals of Database Systems. 7th edn. Boston: Pearson.",
        "Bass, L., Clements, P. and Kazman, R. (2021) Software Architecture in Practice. 4th edn. Boston: Addison-Wesley Professional.",
        "Fowler, M. (2018) Refactoring: Improving the Design of Existing Code. 2nd edn. Boston: Addison-Wesley Professional.",
        "Microsoft Corporation (2025) 'ASP.NET Core MVC overview', Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/overview (Accessed: 12 September 2026).",
        "Microsoft Corporation (2025) 'Entity Framework Core', Microsoft Learn. Available at: https://learn.microsoft.com/en-us/ef/core/ (Accessed: 12 September 2026).",
        "Microsoft Corporation (2025) 'Introduction to Identity on ASP.NET Core', Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity (Accessed: 12 September 2026).",
        "Microsoft Corporation (2023) 'Hosted services and background tasks in ASP.NET Core', Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services (Accessed: 12 September 2026).",
        "Google LLC (2025) 'Using OAuth 2.0 to Access Google APIs', Google Identity Documentation. Available at: https://developers.google.com/identity/protocols/oauth2 (Accessed: 12 September 2026).",
        "bKash Limited (2025) 'bKash Payment Gateway (PGW) API Documentation'. Available at: https://developer.bka.sh/ (Accessed: 12 September 2026).",
        "Silberschatz, A., Galvin, P.B. and Gagne, G. (2018) Operating System Concepts. 10th edn. Hoboken: John Wiley & Sons.",
        "Touch & Solve Technologies Ltd. (2026) 'About Us'. Available at: https://www.touchandsolve.com/about_us (Accessed: 17 September 2026).",
    ]
    for r in refs:
        add_para(doc, r, align="left", indent_first=False, space_after=8)

    # ============================================================ PLAGIARISM
    add_heading1(doc, "Plagiarism Report")
    add_para(doc, "(Plagiarism should be less than 30%)", align="left", indent_first=False)
    add_para(doc, "Attach the proof here.", align="left", indent_first=False)

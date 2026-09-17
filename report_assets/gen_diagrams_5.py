"""Figures 7.17-7.23 — the seven Level 2 DFDs."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *

def level2(filename, left_entities, procs, stores, flows, w=10, h=6.5):
    """left_entities: [(label,y,style)]; procs: [(num,label,x,y)]; stores: [(label,x,y,w)];
       flows: [(x1,y1,x2,y2,label,curve)]"""
    fig, ax = new_fig(w, h)
    for label, y, style in left_entities:
        rect_sharp(ax, 8, y, 14, 7, label, style=style, fontsize=7.3, bold=True)
    for num, label, x, y in procs:
        circle_process(ax, x, y, 10, f"{num}\n{label}", style="system", fontsize=6.8)
    for label, x, y, sw in stores:
        open_rect(ax, x, y, sw, 7, label, fontsize=6.5)
    for x1, y1, x2, y2, label, curve in flows:
        arrow(ax, x1, y1, x2, y2, label=label, curve=curve, lw=1.0)
    save(fig, filename)


# 7.17 Auth & Instructor Onboarding
level2("fig_7_17_dfd_auth_onboarding.png",
    [("Visitor /\nApplicant", 15, "student"), ("Admin", 45, "admin")],
    [("1.1", "Register /\nLogin", 30, 15), ("1.2", "Create Access\nRequest\n(Google / Email)", 55, 30),
     ("1.3", "Admin Approves\n→ Send OTP", 55, 45), ("1.4", "Verify OTP &\nSubmit Application", 80, 30)],
    [("D1  Users & Roles", 30, 55, 22), ("D1b  Access\nRequests", 80, 55, 20)],
    [(15,15,25,15,None,0), (15,45,50,45,None,0), (35,17,50,28,None,0.1),
     (60,32,60,42,None,0), (60,45,75,32,None,-0.1), (85,34,85,50,None,0.05),
     (30,20,30,50,None,0), (55,50,55,50,None,0)], h=6.5)

# 7.18 Course Creation & Approval
level2("fig_7_18_dfd_course_approval.png",
    [("Instructor", 15, "instructor"), ("Admin", 45, "admin")],
    [("2.1", "Create / Edit\nCourse & Content", 40, 15), ("2.2", "Review\nCourse", 65, 30),
     ("2.3", "Set Price &\nApprove / Reject", 40, 45)],
    [("D2  Courses & Content", 40, 58, 26)],
    [(15,15,32,15,None,0), (48,17,58,26,None,0.1), (65,36,65,45,None,0.1),
     (60,45,48,45,None,0.05), (15,45,30,45,None,0), (40,20,40,52,None,0)])

# 7.19 Enrollment & Payment
level2("fig_7_19_dfd_enrollment_payment.png",
    [("Student", 10, "student"), ("bKash\nGateway", 38, "external"), ("Admin", 55, "admin")],
    [("3.1", "Enroll\n(Free / Paid)", 32, 10), ("3.2", "Apply\nCoupon", 55, 10),
     ("3.3", "bKash\nCheckout", 32, 30), ("3.4", "Admin\nRefund", 55, 30)],
    [("D3  Enrollments & Payments", 45, 46, 34)],
    [(10,10,25,10,None,0), (39,10,48,10,None,0), (22,13,25,27,None,0.1),
     (15,38,25,32,"request / confirm",0.1), (15,55,48,32,"issue refund",0.05),
     (32,35,32,42,None,0), (55,35,55,42,None,0)])

# 7.20 Lesson / Assignment / Quiz Delivery
level2("fig_7_20_dfd_content_delivery.png",
    [("Student", 20, "student")],
    [("4.1", "View Lesson &\nMark Complete", 35, 12), ("4.2", "Check Due\nDate", 60, 12),
     ("4.3", "Attempt Quiz &\nAuto-Grade", 35, 32), ("4.4", "Submit\nAssignment", 60, 32)],
    [("D5  Progress,\nQuiz & Assignment\nResults", 47, 48, 26), ("D2  Courses\n& Content", 80, 12, 20)],
    [(15,20,28,13,None,0), (42,12,52,12,None,0), (65,17,65,25,None,-0.1,),
     (65,25,42,30,None,-0.05), (15,20,28,33,None,0.1), (48,32,35,40,None,0.05),
     (60,37,50,42,None,-0.05), (72,15,72,12,None,0)])

# 7.21 Notification Processing
level2("fig_7_21_dfd_notification.png",
    [("Course /\nEnrollment /\nAccess Events", 25, "system")],
    [("5.1", "Detect\nTriggering Event", 40, 15), ("5.2", "Create\nNotification Row", 65, 15),
     ("5.3", "Render Unread /\nRead in Bell", 65, 35)],
    [("D4  Notifications", 40, 45, 20), ("D1  Users", 90, 25, 16)],
    [(15,25,30,17,None,0), (50,15,55,15,None,0), (65,20,65,30,None,0),
     (65,20,50,42,None,0.1), (72,18,86,23,None,0.1), (60,38,50,44,None,0.05)])

# 7.22 Instructor / Admin Reporting
level2("fig_7_22_dfd_reporting.png",
    [("Instructor", 15, "instructor"), ("Admin\n(any course)", 45, "admin")],
    [("6.1", "Aggregate\nStudent Roster", 40, 12), ("6.2", "Aggregate Weekly /\nMonthly Activity", 40, 32),
     ("6.3", "Generate\nPDF Report", 70, 22)],
    [("D1  Users", 15, 45, 16), ("D5  Progress &\nResults", 40, 50, 22), ("D3  Enrollments\n& Payments", 70, 45, 22)],
    [(15,15,30,13,None,0), (15,45,30,32,None,0.05), (45,45,45,33,None,-0.1), (45,12,35,45,None,0.05),
     (48,15,60,20,None,0.05), (48,32,60,24,None,-0.05), (70,27,70,42,None,0),
     (65,16,67,44,None,0.15)])

# 7.23 Certificate & Receipt Generation
level2("fig_7_23_dfd_certificate.png",
    [("Student", 20, "student")],
    [("7.1", "Check 100%\nLesson Completion", 40, 12), ("7.2", "Generate\nCertificate PDF", 65, 12),
     ("7.3", "Generate\nReceipt PDF", 65, 32)],
    [("D5  Lesson\nProgress", 40, 45, 20), ("D3  Payments", 65, 45, 18)],
    [(15,20,28,13,None,0), (52,12,58,12,None,0), (15,20,28,33,None,0.15),
     (40,17,40,42,None,0), (65,17,65,26,None,0), (65,35,65,42,None,0)])

print("Batch 5 complete.")

"""Figures 7.15 (Context DFD) and 7.16 (Level 1 DFD)."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *

# ---------------------------------------------- Figure 7.15 Context Level DFD
fig, ax = new_fig(9, 7)
circle_process(ax, 45, 35, 17, "0.0\nEduLearn\nSystem", style="system", fontsize=9)
rect_sharp(ax, 8, 12, 16, 8, "Student", style="student", fontsize=8, bold=True)
rect_sharp(ax, 8, 35, 16, 8, "Instructor", style="instructor", fontsize=8, bold=True)
rect_sharp(ax, 8, 58, 16, 8, "Admin", style="admin", fontsize=8, bold=True)
rect_sharp(ax, 82, 20, 18, 8, "Google OAuth", style="external", fontsize=8, bold=True)
rect_sharp(ax, 82, 50, 18, 8, "bKash Gateway", style="external", fontsize=8, bold=True)

arrow(ax, 16, 12, 33, 28, label="Enroll, Submit\nWork, Payment")
arrow(ax, 33, 22, 16, 15, label="Courses, Grades,\nCertificate", curve=-0.15)
arrow(ax, 16, 35, 33, 35, label="Author Content")
arrow(ax, 33, 32, 16, 32, label="Content Status", curve=-0.1)
arrow(ax, 16, 58, 33, 42, label="Approve, Manage,\nSet Price")
arrow(ax, 33, 48, 16, 55, label="Reports, Requests", curve=-0.15)
arrow(ax, 62, 30, 74, 22, label="Auth Request")
arrow(ax, 74, 24, 62, 33, label="Verified Email", curve=0.15)
arrow(ax, 62, 40, 74, 48, label="Payment Request")
arrow(ax, 74, 46, 62, 37, label="Payment Confirmation", curve=0.15)
save(fig, "fig_7_15_context_dfd.png")

# ---------------------------------------------- Figure 7.16 Level 1 DFD
fig, ax = new_fig(11, 9)
procs = [
    ("1.0\nAuthentication &\nInstructor Onboarding", 20, 12),
    ("2.0\nCourse Creation\n& Approval", 55, 12),
    ("3.0\nEnrollment &\nPayment", 90, 12),
    ("4.0\nContent Delivery &\nAssessment", 20, 40),
    ("5.0\nNotification\nProcessing", 55, 40),
    ("6.0\nInstructor / Admin\nReporting", 90, 40),
    ("7.0\nCertificate &\nReceipt Generation", 55, 65),
]
for text, x, y in procs:
    circle_process(ax, x, y, 12, text, style="system", fontsize=6.8)

open_rect(ax, 20, 78, 20, 8, "D1  Users & Roles", fontsize=6.8)
open_rect(ax, 45, 78, 20, 8, "D2  Courses & Content", fontsize=6.8)
open_rect(ax, 70, 78, 24, 8, "D3  Enrollments &\nPayments", fontsize=6.8)
open_rect(ax, 95, 78, 20, 8, "D4  Notifications", fontsize=6.8)

rect_sharp(ax, 3, 12, 10, 7, "Student", style="student", fontsize=6.5, bold=True)
rect_sharp(ax, 3, 40, 10, 7, "Instructor", style="instructor", fontsize=6.5, bold=True)
rect_sharp(ax, 3, 65, 10, 7, "Admin", style="admin", fontsize=6.5, bold=True)
rect_sharp(ax, 107, 12, 12, 7, "Google /\nbKash", style="external", fontsize=6.5, bold=True)

arrow(ax, 8, 12, 14, 12, lw=1.0)
arrow(ax, 8, 40, 14, 40, lw=1.0)
arrow(ax, 8, 65, 14, 65, lw=1.0)
arrow(ax, 101, 12, 107, 12, lw=1.0)
arrow(ax, 20, 18, 20, 74, curve=0.1, lw=0.9)
arrow(ax, 55, 18, 45, 74, curve=0.1, lw=0.9)
arrow(ax, 90, 18, 70, 74, curve=-0.1, lw=0.9)
arrow(ax, 20, 46, 45, 78, curve=-0.15, lw=0.9)
arrow(ax, 32, 12, 43, 12, lw=0.9)
arrow(ax, 67, 12, 78, 12, lw=0.9)
arrow(ax, 20, 24, 20, 34, lw=0.9)
arrow(ax, 32, 40, 43, 40, label="Events", lw=0.9)
arrow(ax, 67, 40, 78, 40, label="Report Data", lw=0.9)
arrow(ax, 55, 46, 55, 59, label="Completion", lw=0.9)
arrow(ax, 55, 71, 55, 78, lw=0.9)
save(fig, "fig_7_16_level1_dfd.png")

print("Batch 4 complete.")

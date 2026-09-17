"""Figures 4.1-4.4 (Activity diagrams) and 4.5 (Swim lane)."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *

def flow(ax, points, **kw):
    for i in range(len(points) - 1):
        x1, y1 = points[i]
        x2, y2 = points[i+1]
        arrow(ax, x1, y1, x2, y2, **kw)

# ---------------------------------------------------- Figure 4.1 Admin Activity
fig, ax = new_fig(8, 10.5)
terminal(ax, 40, 4, "Start")
box(ax, 40, 11, 24, 5, "Login (Email & Password)", style="admin")
diamond(ax, 40, 19, 20, 7, "Valid\nCredentials?", style="student")
box(ax, 68, 19, 20, 5, "Show Error", style="external", fontsize=7.5)
box(ax, 40, 28, 26, 5, "Admin Dashboard", style="admin", bold=True)
tasks = ["Pending Courses\n(Approve & Price)", "Manage Courses\n(Edit Price / Delete)",
         "Access Requests\n(Approve / Deny)", "Manage Users &\nCategories", "Revenue / Analytics\nReports & Coupons"]
xs = [8, 24, 40, 56, 72]
for x, t in zip(xs, tasks):
    box(ax, x, 40, 15, 8, t, style="instructor", fontsize=7)
    arrow(ax, 40, 30.5, x, 36, curve=0.05, lw=1.0)
diamond(ax, 40, 52, 22, 7, "Continue\nWorking?", style="student")
for x in xs:
    arrow(ax, x, 44, 40, 48.5, curve=0.05, lw=1.0)
box(ax, 40, 61, 22, 5, "Logout", style="admin")
terminal(ax, 40, 68, "End")
arrow(ax, 40, 6.5, 40, 8.5)
arrow(ax, 40, 13.5, 40, 15.5)
arrow(ax, 50, 19, 58, 19, label="Invalid")
arrow(ax, 68, 21.5, 40, 26, curve=-0.15)
arrow(ax, 40, 22.5, 40, 25.5, label="Valid")
arrow(ax, 40, 55.5, 40, 58.5, label="No")
arrow(ax, 51, 52, 65, 30, label="Yes", curve=-0.25)
arrow(ax, 40, 63.5, 40, 65.5)
save(fig, "fig_4_1_activity_admin.png")

# ---------------------------------------------------- Figure 4.2 Instructor Activity
fig, ax = new_fig(8, 10.5)
terminal(ax, 40, 4, "Start")
box(ax, 40, 11, 24, 5, "Login (Email & Password)", style="instructor")
box(ax, 40, 19, 26, 5, "My Courses Dashboard", style="instructor", bold=True)
tasks = ["Create / Edit\nCourse", "Manage Content\n(Modules, Lessons,\nQuiz, Assignment)",
         "View Students &\nGenerate Report", "Reply to\nReviews", "View Quiz\nResults"]
xs = [8, 24, 40, 56, 72]
for x, t in zip(xs, tasks):
    box(ax, x, 30, 15, 8.5, t, style="student", fontsize=7)
    arrow(ax, 40, 21.5, x, 25.5, curve=0.05, lw=1.0)
diamond(ax, 40, 43, 22, 7, "Continue\nWorking?", style="admin")
for x in xs:
    arrow(ax, x, 34.5, 40, 39.5, curve=0.05, lw=1.0)
box(ax, 40, 53, 22, 5, "Logout", style="instructor")
terminal(ax, 40, 60, "End")
arrow(ax, 40, 6.5, 40, 8.5)
arrow(ax, 40, 13.5, 40, 16.5)
arrow(ax, 40, 46.5, 40, 50.5, label="No")
arrow(ax, 51, 43, 65, 21, label="Yes", curve=-0.25)
arrow(ax, 40, 55.5, 40, 57.5)
save(fig, "fig_4_2_activity_instructor.png")

# ---------------------------------------------------- Figure 4.3 Student Activity
fig, ax = new_fig(8, 11)
terminal(ax, 40, 4, "Start")
box(ax, 40, 11, 30, 5, "Browse / Search Course Catalog", style="student")
box(ax, 40, 19, 22, 5, "Open Course Details", style="student")
diamond(ax, 40, 27, 20, 7, "Free or\nPaid?", style="admin")
box(ax, 16, 36, 20, 6, "Enroll — Active\nImmediately", style="instructor", fontsize=7.2)
box(ax, 64, 36, 24, 8, "Add to Cart → Apply\nCoupon → bKash Checkout", style="external", fontsize=7)
box(ax, 40, 47, 26, 5, "View Lessons &\nMark Complete", style="student")
box(ax, 20, 56, 20, 6, "Take Quiz\n(Auto-Graded)", style="student", fontsize=7.5)
box(ax, 60, 56, 22, 6, "Submit Assignment\n(Before Due Date)", style="student", fontsize=7.2)
diamond(ax, 40, 66, 24, 7, "All Lessons\nComplete?", style="admin")
box(ax, 40, 76, 26, 5, "Download Certificate\n& Write Review", style="instructor", fontsize=7.2)
terminal(ax, 40, 84, "End")

arrow(ax, 40, 6.5, 40, 8.5)
arrow(ax, 40, 13.5, 40, 16.5)
arrow(ax, 40, 22.5, 40, 23.5)
arrow(ax, 30, 27, 16, 33, label="Free")
arrow(ax, 50, 27, 64, 32, label="Paid")
arrow(ax, 16, 39, 30, 45, curve=-0.1)
arrow(ax, 64, 40, 46, 45, curve=0.1)
arrow(ax, 33, 50, 20, 53, curve=-0.05)
arrow(ax, 47, 50, 60, 53, curve=0.05)
arrow(ax, 20, 59, 35, 63, curve=-0.05)
arrow(ax, 60, 59, 45, 63, curve=0.05)
arrow(ax, 40, 69.5, 40, 73.5, label="Yes")
arrow(ax, 52, 66, 60, 56, label="No — keep learning", curve=-0.2)
arrow(ax, 40, 78.5, 40, 81.5)
save(fig, "fig_4_3_activity_student.png")

# ---------------------------------------------------- Figure 4.4 Access Request Activity
fig, ax = new_fig(8, 12)
terminal(ax, 40, 4, "Start")
box(ax, 40, 11, 28, 5, "Open /Apply", style="student")
diamond(ax, 40, 19, 22, 7, "Google or\nEmail?", style="admin")
box(ax, 16, 28, 22, 6, "Google OAuth\nChallenge", style="external", fontsize=7.3)
box(ax, 64, 28, 22, 6, "Type & Validate\nEmail Address", style="external", fontsize=7.3)
box(ax, 40, 37, 30, 5, "Create Pending\nInstructorAccessRequest", style="admin", fontsize=7.5)
box(ax, 40, 45, 26, 5, "Admin Reviews Request", style="admin")
diamond(ax, 40, 53, 20, 7, "Approve?", style="student")
box(ax, 68, 53, 20, 5, "Email Denial\nNotice", style="external", fontsize=7.3)
box(ax, 40, 63, 32, 6, "Email OTP Code + Access\nToken Link (24h, 5 attempts)", style="instructor", fontsize=7)
diamond(ax, 40, 73, 22, 7, "Correct\nCode?", style="student")
box(ax, 40, 83, 28, 5, "Full Application Form\n(Resume Upload)", style="instructor", fontsize=7.3)
box(ax, 40, 91, 26, 5, "Admin Reviews\nApplication", style="admin")
diamond(ax, 40, 99, 22, 7, "Approve?", style="student")
box(ax, 16, 108, 22, 6, "Email Password-Setup\nLink — Instructor Active", style="instructor", fontsize=6.8)
box(ax, 64, 108, 22, 6, "Archive Details &\nDelete Account", style="external", fontsize=6.8)
terminal(ax, 40, 116, "End")

arrow(ax, 40, 6.5, 40, 8.5); arrow(ax, 40, 13.5, 40, 15.5)
arrow(ax, 30, 19, 16, 25, label="Google"); arrow(ax, 50, 19, 64, 25, label="Email")
arrow(ax, 16, 31, 34, 35, curve=-0.1); arrow(ax, 64, 31, 46, 35, curve=0.1)
arrow(ax, 40, 39.5, 40, 42.5); arrow(ax, 40, 47.5, 40, 49.5)
arrow(ax, 40, 56.5, 40, 60.5, label="Yes")
arrow(ax, 50, 53, 58, 53, label="No")
arrow(ax, 40, 66, 40, 69.5)
arrow(ax, 29, 73, 12, 73, label="Wrong —\nretry / lock", curve=0.0)
arrow(ax, 12, 73, 12, 63, curve=0.2)
arrow(ax, 12, 63, 24, 63, curve=0.15)
arrow(ax, 40, 76.5, 40, 80.5, label="Correct")
arrow(ax, 40, 85.5, 40, 88.5)
arrow(ax, 40, 93.5, 40, 95.5)
arrow(ax, 29, 99, 16, 105, label="Yes")
arrow(ax, 51, 99, 64, 105, label="No")
arrow(ax, 16, 111, 34, 114, curve=-0.1)
arrow(ax, 64, 111, 46, 114, curve=0.1)
save(fig, "fig_4_4_activity_access_request.png")

# ---------------------------------------------------- Figure 4.5 Swim Lane
fig, ax = new_fig(10, 8)
lanes = [("Instructor", "instructor"), ("EduLearn System", "system"), ("Admin", "admin"), ("Student", "student")]
lane_w = swimlane_columns(ax, 100, 80, lanes, top_margin=6)
c0, c1, c2, c3 = lane_w*0.5, lane_w*1.5, lane_w*2.5, lane_w*3.5

box(ax, c0, 15, 20, 6, "Create Course\n& Content", style="instructor", fontsize=7.3)
box(ax, c1, 22, 20, 6, "Store as\nPending", style="system", fontsize=7.3)
box(ax, c2, 29, 20, 6, "Review Course", style="admin", fontsize=7.3)
diamond(ax, c2, 38, 18, 7, "Approve?", style="admin")
box(ax, c0, 47, 20, 6, "Revise &\nResubmit", style="instructor", fontsize=7.2)
box(ax, c1, 47, 20, 6, "Set Price,\nStatus = Approved", style="system", fontsize=7)
box(ax, c3, 56, 20, 6, "Browse & Enroll\n(Free / Paid)", style="student", fontsize=7)
box(ax, c1, 64, 20, 6, "Create Enrollment\n(Active / Pending)", style="system", fontsize=6.8)
box(ax, c3, 72, 20, 6, "bKash Checkout\n(if Paid)", style="student", fontsize=7.3)
box(ax, c1, 72, 20, 6, "Activate Enrollment\n& Create Payment", style="system", fontsize=6.8)

arrow(ax, c0, 18, c1, 19, curve=0.1)
arrow(ax, c1, 25, c2, 26, curve=0.1)
arrow(ax, c2, 32, c2, 34.5)
arrow(ax, c2-8, 41, c0, 44, label="No", curve=-0.1)
arrow(ax, c0, 50, c1, 45, curve=0.15)
arrow(ax, c2+8, 41, c1, 44, label="Yes", curve=0.1)
arrow(ax, c1, 50, c3, 53, curve=0.15)
arrow(ax, c3, 59, c1, 61, curve=-0.15)
arrow(ax, c1, 67, c3, 69, curve=0.15)
arrow(ax, c3, 75, c1, 75, curve=0.0)
save(fig, "fig_4_5_swimlane.png")

print("Batch 2 complete.")

"""Figures 1.1, 1.2, 3.1, 7.1, 7.24 (process model, stack, use case, architecture, ERD)."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *
import math

# ---------------------------------------------------------------- Figure 1.1
fig, ax = new_fig(9, 6.2)
steps = ["1. Requirement\nUnderstanding", "2. Planning", "3. Design", "4. Implementation",
         "5. Testing", "6. Debugging", "7. Refinement"]
cx0, cy0, R = 45, 30, 22
n = len(steps)
pos = []
for i, s in enumerate(steps):
    ang = math.pi/2 - i * (2*math.pi/n)
    x = cx0 + R*math.cos(ang)
    y = cy0 - R*math.sin(ang)
    pos.append((x, y))
box(ax, cx0, cy0, 13, 6, "Agile\nDevelopment\nCycle", style="system", fontsize=9.5, bold=True, z=3)
for i, (x, y) in enumerate(pos):
    box(ax, x, y, 12.5, 6.5, steps[i], style="admin" if i % 2 == 0 else "instructor", fontsize=8)
for i in range(n):
    x1, y1 = pos[i]
    x2, y2 = pos[(i+1) % n]
    arrow(ax, x1, y1, x2, y2, curve=0.18)
save(fig, "fig_1_1_agile.png")

# ---------------------------------------------------------------- Figure 1.2
fig, ax = new_fig(9, 6)
box(ax, 15, 12, 22, 15, "Frontend Layer\n\nHTML / CSS\nBootstrap 5\nJavaScript / Chart.js", style="student", fontsize=8)
box(ax, 45, 12, 26, 15, "Application Layer\n\nASP.NET Core MVC (C#)\nService Layer\nEntity Framework Core", style="admin", fontsize=8, bold=False)
box(ax, 75, 8, 22, 13, "External Services\n\nGoogle OAuth 2.0\nbKash Payment API\nSMTP (MailKit)", style="external", fontsize=7.8)
box(ax, 75, 24, 22, 11, "Background Services\n\nDeadline Reminder\nBackground Service", style="instructor", fontsize=7.8)
box(ax, 45, 34, 26, 10, "Database Layer\n\nMicrosoft SQL Server", style="db", fontsize=8.5)
arrow(ax, 26, 12, 32, 12)
arrow(ax, 58, 12, 64, 9)
arrow(ax, 58, 15, 64, 22)
arrow(ax, 45, 19.5, 45, 29)
save(fig, "fig_1_2_stack.png")

# ---------------------------------------------------------------- Figure 3.1 Use Case
fig, ax = new_fig(11, 8.5)
box(ax, 55, 2.8, 96, 2.2, "System Boundary: EduLearn", style="neutral", fontsize=10, bold=True, z=1)
rect_sharp(ax, 55, 45, 96, 80, "", style="neutral", z=0)

actor_figure(ax, 6, 20, "Student", scale=0.5)
actor_figure(ax, 6, 45, "Instructor", scale=0.5)
actor_figure(ax, 6, 70, "Admin", scale=0.5)
actor_figure(ax, 104, 20, "Google\nOAuth", scale=0.5, color="#E03131")
actor_figure(ax, 104, 45, "bKash\nGateway", scale=0.5, color="#E03131")

student_uc = ["Register / Login", "Browse & Enroll\nin Courses", "View Lessons &\nMark Complete",
              "Take Quiz\n(Auto-Graded)", "Submit\nAssignment", "Cart, Coupon &\nbKash Checkout",
              "Download\nCertificate / Receipt", "Write Review", "Chat with\nAI Assistant"]
y0 = 8
for i, t in enumerate(student_uc):
    usecase_ellipse(ax, 30, y0 + i*8.3, t, w=20, h=6.3, style="student", fontsize=6.8)
    arrow(ax, 9, 20, 20, y0 + i*8.3, curve=0.05, lw=0.9)

instr_uc = ["Create / Edit\nCourse", "Author Modules,\nLessons, Quizzes",
            "Reply to Reviews", "View Quiz Results", "View Student\nRoster & Report"]
for i, t in enumerate(instr_uc):
    usecase_ellipse(ax, 55, 8 + i*8, t, w=18, h=6.3, style="instructor", fontsize=6.8)
    arrow(ax, 9, 45, 46, 8 + i*8, curve=0.05, lw=0.9)

admin_uc = ["Approve Course\n& Set Price", "Manage Users &\nCategories",
            "Approve Access\nRequest / Application", "Course Overview\n(Any Course)",
            "Revenue & Analytics\nReports", "Manage Payments\n& Coupons"]
for i, t in enumerate(admin_uc):
    usecase_ellipse(ax, 80, 6 + i*8, t, w=18, h=6.3, style="admin", fontsize=6.8)
    arrow(ax, 9, 70, 71, 6 + i*8, curve=-0.05, lw=0.9)

arrow(ax, 101, 20, 88 + 9, 6+2*8, curve=0.1, lw=1.0, color="#E03131")
arrow(ax, 101, 45, 60, 8+5*8-4, curve=0.1, lw=1.0, color="#E03131")
save(fig, "fig_3_1_usecase.png")

# ---------------------------------------------------------------- Figure 7.1 Architecture
fig, ax = new_fig(9, 7)
box(ax, 45, 5, 60, 6, "Users (Student / Instructor / Admin) — Web Browser", style="neutral", fontsize=9, bold=True)
box(ax, 45, 14, 60, 6, "Presentation Layer — Razor Views, Bootstrap 5, JavaScript, Chart.js", style="student", fontsize=8.3)
box(ax, 45, 23, 60, 6, "ASP.NET Core MVC — Controllers, ViewModels, Routing", style="admin", fontsize=8.3)
box(ax, 30, 32, 32, 7, "Service Layer\nQuizGrader, CourseProgressCalculator,\nCourseRosterService, ReportPdfService, ...", style="instructor", fontsize=7.2)
box(ax, 68, 32, 26, 7, "Background Services\nDeadlineReminder\nBackgroundService", style="instructor", fontsize=7.4)
box(ax, 15, 43, 22, 8, "External: Google\nOAuth 2.0", style="external", fontsize=7.6)
box(ax, 45, 43, 22, 8, "External: bKash\nPayment Gateway", style="external", fontsize=7.6)
box(ax, 75, 43, 22, 8, "External: SMTP\n(MailKit Email)", style="external", fontsize=7.6)
box(ax, 45, 53, 40, 6, "Entity Framework Core (Data Access Layer)", style="neutral", fontsize=8.3)
box(ax, 45, 61, 36, 6.5, "Microsoft SQL Server Database", style="db", fontsize=9, bold=True)
for (x1,y1,x2,y2) in [(45,8,45,11),(45,17,45,20),(45,26,30,28.5),(45,26,68,28.5),
                       (30,35.5,15,39),(30,35.5,45,39),(68,35.5,75,39),
                       (15,47,45,50),(45,47,45,50),(75,47,45,50),
                       (45,56,45,57.7)]:
    arrow(ax, x1, y1, x2, y2)
save(fig, "fig_7_1_architecture.png")

# ---------------------------------------------------------------- Figure 7.24 ERD
fig, ax = new_fig(11, 8.2)
def ent(cx, cy, w, h, title, attrs, style="admin"):
    box(ax, cx, cy, w, h, "", style=style, z=2)
    ax.text(cx, cy - h/2 + 1.6, title, ha="center", fontsize=7.6, fontweight="bold", zorder=4)
    ax.plot([cx-w/2+0.5, cx+w/2-0.5], [cy-h/2+2.8, cy-h/2+2.8], color="#495057", linewidth=0.8, zorder=4)
    ax.text(cx, cy+1.2, attrs, ha="center", va="top", fontsize=5.9, zorder=4, linespacing=1.6)

ent(15, 8, 22, 16, "ApplicationUser", "Id (PK)\nFullName, Email\nRole, IsApproved\nIsActive")
ent(45, 8, 22, 13, "Category", "Id (PK)\nName")
ent(75, 8, 24, 16, "Course", "Id (PK)\nInstructorId (FK)\nCategoryId (FK)\nPrice, Status")
ent(75, 28, 22, 13, "Module", "Id (PK)\nCourseId (FK)\nTitle, Order")
ent(75, 46, 22, 13, "Lesson", "Id (PK)\nModuleId (FK)\nTitle")
ent(55, 62, 20, 13, "Assignment", "Id (PK)\nLessonId (FK)\nDueDate")
ent(95, 62, 20, 13, "Quiz", "Id (PK)\nLessonId (FK)\nDueDate")
ent(15, 28, 22, 13, "Enrollment", "Id (PK)\nCourseId, StudentId\nStatus")
ent(15, 46, 22, 13, "Payment", "Id (PK)\nCourseId, StudentId\nAmount, Status")
ent(15, 64, 22, 13, "Review", "Id (PK)\nCourseId, StudentId\nRating")
ent(37, 46, 20, 13, "LessonProgress", "Id (PK)\nLessonId, StudentId")
ent(37, 64, 20, 13, "Notification", "Id (PK)\nUserId (FK)\nIsRead")
ent(55, 78, 22, 13, "AssignmentSubmission", "Id (PK)\nAssignmentId, StudentId")
ent(95, 78, 22, 13, "QuizResult", "Id (PK)\nQuizId, StudentId\nScore")

rels = [
    (15,16,15,21,"1","M"), (45,14.5,45,20,"1","M"), (75,16,75,21.5,"1","M"),
    (75,34.5,75,39.5,"1","M"), (75,52.5,55,55.5,"1","M"), (75,52.5,95,55.5,"1","M"),
    (26,8,64,8,"M","1"), (15,34.5,15,39.5,"1","M"), (26,28,55,71.5,"1","M"),
    (26,46,27,46,"1","M"), (15,52.5,15,57.5,"1","M"), (26,46,27,64,"1","M"),
]
for (x1,y1,x2,y2,l1,l2) in rels:
    arrow(ax, x1, y1, x2, y2, color="#868E96", lw=0.9, style="-")
    ax.text(x1+0.5, y1-0.5, l1, fontsize=6.5, color="#495057", zorder=5)
    ax.text(x2-0.5, y2+0.8, l2, fontsize=6.5, color="#495057", zorder=5)
save(fig, "fig_7_24_erd.png")

print("Batch 1 complete.")

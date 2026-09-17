"""Figures 4.6, 4.7 — Sequence diagrams."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *

def lifeline_header(ax, cx, label, style, top=6, bottom=90, fontsize=8):
    box(ax, cx, top, 18, 6, label, style=style, fontsize=fontsize, bold=True)
    ax.plot([cx, cx], [top + 3, bottom], color="#868E96", linewidth=1.1, linestyle=(0, (4, 3)), zorder=1)

def msg(ax, x1, x2, y, label, dashed=False, color="#212529"):
    arrow(ax, x1, y, x2, y, label=None, color=color, lw=1.2, dashed=dashed)
    mx = (x1 + x2) / 2
    ax.text(mx, y - 1.6, label, ha="center", va="bottom", fontsize=6.6, color="#212529",
            bbox=dict(facecolor="white", edgecolor="none", alpha=0.9, pad=1.0))

# ---------------------------------------------- Figure 4.6 Course Approval Sequence
fig, ax = new_fig(10, 8)
xs = {"Admin": 8, "AdminController": 30, "Database": 52, "NotificationService": 74, "Instructor": 94}
styles = {"Admin": "admin", "AdminController": "system", "Database": "db",
          "NotificationService": "instructor", "Instructor": "instructor"}
for name, x in xs.items():
    lifeline_header(ax, x, name, styles[name], bottom=68, fontsize=6.8)

y = 16
msg(ax, xs["Admin"], xs["AdminController"], y, "POST ApproveCourse(id, isFree, price)")
y += 9
msg(ax, xs["AdminController"], xs["Database"], y, "Set Price, Status = Approved")
y += 9
msg(ax, xs["Database"], xs["AdminController"], y, "SaveChangesAsync()", dashed=True)
y += 9
msg(ax, xs["AdminController"], xs["NotificationService"], y, "CreateNotification(instructorId)")
y += 9
msg(ax, xs["NotificationService"], xs["Database"], y, "Insert Notification row")
y += 9
msg(ax, xs["NotificationService"], xs["Instructor"], y, "Notification appears\nin bell (next request)")
y += 9
msg(ax, xs["AdminController"], xs["Admin"], y, "Redirect: \"Course approved.\"", dashed=True)
save(fig, "fig_4_6_seq_course_approval.png")

# ---------------------------------------------- Figure 4.7 Quiz Attempt Sequence
fig, ax = new_fig(10, 9)
xs = {"Student": 10, "CourseController": 35, "QuizGrader": 60, "Database": 85}
styles = {"Student": "student", "CourseController": "system", "QuizGrader": "instructor", "Database": "db"}
for name, x in xs.items():
    lifeline_header(ax, x, name, styles[name], bottom=82, fontsize=7.5)

y = 16
msg(ax, xs["Student"], xs["CourseController"], y, "GET TakeQuiz(quizId)")
y += 8
msg(ax, xs["CourseController"], xs["Database"], y, "Load Quiz + DueDate")
y += 8
msg(ax, xs["CourseController"], xs["CourseController"], y, "if (Now > DueDate) redirect\nwith “deadline passed”", dashed=True)
y += 9
msg(ax, xs["CourseController"], xs["Student"], y, "Render quiz questions")
y += 9
msg(ax, xs["Student"], xs["CourseController"], y, "POST SubmitQuiz(answers)")
y += 8
msg(ax, xs["CourseController"], xs["Database"], y, "Re-check DueDate (independent)")
y += 8
msg(ax, xs["CourseController"], xs["QuizGrader"], y, "Grade(quiz, selectedOptionIds)")
y += 8
msg(ax, xs["QuizGrader"], xs["CourseController"], y, "score, passed", dashed=True)
y += 8
msg(ax, xs["CourseController"], xs["Database"], y, "Upsert QuizResult (in place on retake)")
y += 8
msg(ax, xs["CourseController"], xs["Student"], y, "Redirect: Quiz Result page")
save(fig, "fig_4_7_seq_quiz_attempt.png")

print("Batch 3 complete.")

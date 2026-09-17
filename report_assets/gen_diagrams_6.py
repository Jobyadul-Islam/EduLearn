"""Figure 2.1 — Organizational Structure of Touch & Solve Technologies Ltd."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from diagram_lib import *

fig, ax = new_fig(9, 5.5)
box(ax, 45, 8, 30, 7, "Chief Executive Officer", style="admin", fontsize=9, bold=True)
depts = ["Software & Web\nDevelopment", "IT Infrastructure,\nNetworking & Surveillance",
         "Customized Software\n(POS, Institute / Hospital\nManagement)", "Client Support &\nBusiness Operations"]
xs = [10, 33, 58, 82]
for x, d in zip(xs, depts):
    box(ax, x, 22, 20, 9, d, style="instructor", fontsize=7.2)
    arrow(ax, 45, 12, x, 17, curve=0.05, lw=1.0)
box(ax, 10, 35, 18, 6, "Development\nTeam", style="student", fontsize=7)
arrow(ax, 10, 26.5, 10, 32, lw=1.0)
save(fig, "fig_2_1_org_structure.png")
print("Batch 6 complete.")

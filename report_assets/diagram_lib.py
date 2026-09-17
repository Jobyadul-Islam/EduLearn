"""Small reusable box/arrow/diamond diagramming helpers built on matplotlib,
used to generate every structural figure (Use Case, Activity, Swimlane,
Sequence, Architecture, DFD, ERD) for the EduLearn practicum report."""

import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as patches
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch, Circle, Ellipse

OUT_DIR = os.path.join(os.path.dirname(__file__), "figures")
os.makedirs(OUT_DIR, exist_ok=True)

PALETTE = {
    "admin":      ("#E7ECFF", "#4C6EF5"),
    "instructor": ("#E3FCEF", "#12B886"),
    "student":    ("#FFF3BF", "#E8A400"),
    "system":     ("#F1E9FE", "#7048E8"),
    "external":   ("#FFE3E0", "#E03131"),
    "db":         ("#F1F3F5", "#495057"),
    "neutral":    ("#F8F9FA", "#495057"),
    "start_end":  ("#212529", "#212529"),
}


def new_fig(w, h, title=None):
    fig, ax = plt.subplots(figsize=(w, h), dpi=220)
    ax.set_xlim(0, w * 10)
    ax.set_ylim(0, h * 10)
    ax.axis("off")
    ax.invert_yaxis()
    if title:
        ax.text(w * 5, 0.15, title, ha="center", va="top", fontsize=11, fontweight="bold", color="#1A1A1A")
    return fig, ax


def box(ax, cx, cy, w, h, text, style="neutral", fontsize=8.3, bold=False, rounding=0.12, z=2):
    fc, ec = PALETTE.get(style, PALETTE["neutral"])
    rect = FancyBboxPatch((cx - w / 2, cy - h / 2), w, h,
                           boxstyle=f"round,pad=0.03,rounding_size={rounding}",
                           linewidth=1.3, edgecolor=ec, facecolor=fc, zorder=z)
    ax.add_patch(rect)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize,
            fontweight="bold" if bold else "normal", color="#1A1A1A", zorder=z + 1, wrap=True)
    return rect


def rect_sharp(ax, cx, cy, w, h, text, style="neutral", fontsize=8.3, bold=False, z=2):
    fc, ec = PALETTE.get(style, PALETTE["neutral"])
    rect = patches.Rectangle((cx - w / 2, cy - h / 2), w, h,
                              linewidth=1.3, edgecolor=ec, facecolor=fc, zorder=z)
    ax.add_patch(rect)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize,
            fontweight="bold" if bold else "normal", color="#1A1A1A", zorder=z + 1, wrap=True)
    return rect


def open_rect(ax, cx, cy, w, h, text, style="db", fontsize=8.0, z=2):
    """Gane-Sarson style open-ended rectangle, used for a DFD data store."""
    fc, ec = PALETTE.get(style, PALETTE["db"])
    y0 = cy - h / 2
    x0 = cx - w / 2
    ax.plot([x0, x0 + w], [y0, y0], color=ec, linewidth=1.4, zorder=z)
    ax.plot([x0, x0 + w], [y0 + h, y0 + h], color=ec, linewidth=1.4, zorder=z)
    ax.plot([x0, x0], [y0, y0 + h], color=ec, linewidth=1.4, zorder=z)
    rect = patches.Rectangle((x0, y0), w, h, linewidth=0, facecolor=fc, zorder=z - 1)
    ax.add_patch(rect)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize, color="#1A1A1A", zorder=z + 1, wrap=True)


def circle_process(ax, cx, cy, r, text, style="system", fontsize=8.0, z=2):
    fc, ec = PALETTE.get(style, PALETTE["system"])
    c = Ellipse((cx, cy), r * 2, r * 1.35, linewidth=1.4, edgecolor=ec, facecolor=fc, zorder=z)
    ax.add_patch(c)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize, color="#1A1A1A", zorder=z + 1, wrap=True)


def diamond(ax, cx, cy, w, h, text, style="student", fontsize=7.8, z=2):
    fc, ec = PALETTE.get(style, PALETTE["student"])
    pts = [(cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2), (cx - w / 2, cy)]
    poly = patches.Polygon(pts, closed=True, facecolor=fc, edgecolor=ec, linewidth=1.3, zorder=z)
    ax.add_patch(poly)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize, color="#1A1A1A", zorder=z + 1, wrap=True)


def terminal(ax, cx, cy, text, w=14, h=5, fontsize=9.5, z=2):
    fc, ec = "#212529", "#212529"
    rect = FancyBboxPatch((cx - w / 2, cy - h / 2), w, h, boxstyle="round,pad=0.03,rounding_size=0.25",
                           linewidth=0, facecolor=fc, zorder=z)
    ax.add_patch(rect)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize, color="white", fontweight="bold", zorder=z + 1)


def arrow(ax, x1, y1, x2, y2, label=None, color="#495057", curve=0.0, style="-|>", lw=1.3, z=5, dashed=False):
    a = FancyArrowPatch((x1, y1), (x2, y2), arrowstyle=style, mutation_scale=13,
                         color=color, linewidth=lw, connectionstyle=f"arc3,rad={curve}",
                         zorder=z, linestyle="dashed" if dashed else "solid")
    ax.add_patch(a)
    if label:
        mx, my = (x1 + x2) / 2, (y1 + y2) / 2 + (curve * 3)
        ax.text(mx, my, label, ha="center", va="center", fontsize=7,
                color="#212529", zorder=z + 1,
                bbox=dict(facecolor="white", edgecolor="none", alpha=0.88, pad=1.2))


def swimlane_columns(ax, w, h, lanes, top_margin=0.5):
    """lanes: list of (label, style). Draws vertical lane separators + headers."""
    n = len(lanes)
    lane_w = w / n
    for i, (label, style) in enumerate(lanes):
        fc, ec = PALETTE.get(style, PALETTE["neutral"])
        x0 = i * lane_w
        header = patches.Rectangle((x0, 0), lane_w, top_margin, linewidth=1.2,
                                    edgecolor=ec, facecolor=fc, zorder=1)
        ax.add_patch(header)
        ax.text(x0 + lane_w / 2, top_margin / 2, label, ha="center", va="center",
                fontsize=9, fontweight="bold", color="#1A1A1A", zorder=2)
        body = patches.Rectangle((x0, top_margin), lane_w, h - top_margin, linewidth=1.0,
                                  edgecolor="#CED4DA", facecolor="white", zorder=0)
        ax.add_patch(body)
    return lane_w


def actor_figure(ax, cx, cy, label, scale=0.45, color="#343A40"):
    head_r = 0.16 * scale / 0.45
    ax.add_patch(Circle((cx, cy - 0.55 * scale / 0.45), head_r, facecolor="white", edgecolor=color, linewidth=1.4, zorder=3))
    ax.plot([cx, cx], [cy - 0.38 * scale / 0.45, cy + 0.15 * scale / 0.45], color=color, linewidth=1.6, zorder=3)
    ax.plot([cx - 0.28 * scale / 0.45, cx + 0.28 * scale / 0.45], [cy - 0.2 * scale / 0.45, cy - 0.2 * scale / 0.45], color=color, linewidth=1.6, zorder=3)
    ax.plot([cx, cx - 0.22 * scale / 0.45], [cy + 0.15 * scale / 0.45, cy + 0.5 * scale / 0.45], color=color, linewidth=1.6, zorder=3)
    ax.plot([cx, cx + 0.22 * scale / 0.45], [cy + 0.15 * scale / 0.45, cy + 0.5 * scale / 0.45], color=color, linewidth=1.6, zorder=3)
    ax.text(cx, cy + 0.72 * scale / 0.45, label, ha="center", va="top", fontsize=8, fontweight="bold", color=color, zorder=3)


def usecase_ellipse(ax, cx, cy, text, w=2.3, h=0.68, style="system", fontsize=7.6):
    fc, ec = PALETTE.get(style, PALETTE["system"])
    e = Ellipse((cx, cy), w, h, facecolor=fc, edgecolor=ec, linewidth=1.2, zorder=2)
    ax.add_patch(e)
    ax.text(cx, cy, text, ha="center", va="center", fontsize=fontsize, color="#1A1A1A", zorder=3, wrap=True)


def _autocrop(ax, pad=3.0):
    xs, ys = [], []
    for p in ax.patches:
        try:
            bb = p.get_window_extent(renderer=None) if False else None
        except Exception:
            bb = None
    # Simpler: use each patch's path extents in data coords via get_extents on get_path with transform
    for p in ax.patches:
        try:
            ext = p.get_path().get_extents(p.get_patch_transform())
            xs += [ext.x0, ext.x1]
            ys += [ext.y0, ext.y1]
        except Exception:
            pass
    for t in ax.texts:
        try:
            x, y = t.get_position()
            xs.append(x)
            ys.append(y)
        except Exception:
            pass
    if not xs or not ys:
        return
    x0, x1 = min(xs) - pad, max(xs) + pad
    y0, y1 = min(ys) - pad, max(ys) + pad
    # axis is inverted (data y grows downward) — keep that orientation
    if ax.yaxis_inverted():
        ax.set_ylim(y1, y0)
    else:
        ax.set_ylim(y0, y1)
    ax.set_xlim(x0, x1)


def save(fig, filename):
    ax = fig.axes[0]
    _autocrop(ax)
    path = os.path.join(OUT_DIR, filename)
    fig.savefig(path, bbox_inches="tight", facecolor="white", pad_inches=0.15)
    plt.close(fig)
    print(f"saved {filename}")
    return path

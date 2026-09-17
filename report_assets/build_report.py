# -*- coding: utf-8 -*-
from docx_engine import new_document
import content_1, content_2, content_3, content_4, content_5
import os

doc = new_document()
content_1.build(doc)
content_2.build(doc)
content_3.build(doc)
content_4.build(doc)
content_5.build(doc)

out_path = os.path.join(os.path.dirname(__file__), "..", "EduLearn_Practicum_Report.docx")
doc.save(out_path)
print("Saved:", os.path.abspath(out_path))
print("Total paragraphs:", len(doc.paragraphs))
print("Total tables:", len(doc.tables))

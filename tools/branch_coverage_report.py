#!/usr/bin/env python3
"""Tạo báo cáo branch coverage (markdown) từ coverage.cobertura.xml của Coverlet.

Dùng: branch_coverage_report.py <cobertura.xml> <method|class|project|solution> <tên> [<tên mục tiêu phụ>]
  method   : <tên> = tên method (ví dụ applyMigrationsForVersion)
  class    : <tên> = tên class (ví dụ EditorBeatmap); gộp cả nested/closure class
  project  : <tên> = tên package/assembly (ví dụ osu.Game.Rulesets.Osu)
  solution : <tên> = bỏ qua; cộng dồn mọi package
Branch lấy từ <line branch="True" condition-coverage="x% (c/v)">; branches-covered/valid = tổng c/v.
"""
import re
import sys
import xml.etree.ElementTree as ET


def branches(el):
    c = v = 0
    # Chỉ đếm line trong <method>: <class> lặp lại các line này nên đếm cả hai sẽ nhân đôi.
    ms = [el] if el.tag == "method" else list(el.iter("method"))
    for ln in (l for m in ms for l in m.iter("line")):
        m = re.search(r"\((\d+)/(\d+)\)", ln.get("condition-coverage", ""))
        if ln.get("branch") == "True" and m:
            c += int(m[1]); v += int(m[2])
    return c, v


def fmt(name, c, v):
    return f"| {name} | {c} | {v} | {(c / v if v else 0):.4f} | {(100 * c / v if v else 0):.2f}% |"


HEAD = "| Đối tượng | branches-covered | branches-valid | branch-rate | % |\n|---|---|---|---|---|"


def main():
    path, level, name = sys.argv[1], sys.argv[2], (sys.argv[3] if len(sys.argv) > 3 else "")
    root = ET.parse(path).getroot()
    pk = root.findall(".//package")
    rows = []
    if level == "method":
        meths = [(cl.get("name"), m) for p in pk for cl in p.iter("class") for m in cl.iter("method") if m.get("name") == name]
        for cn, m in meths:
            rows.append(fmt(f"{cn}.{name}{m.get('signature')}", *branches(m)))
        c = sum(branches(m)[0] for _, m in meths); v = sum(branches(m)[1] for _, m in meths)
        rows.append(fmt(f"**Tổng method `{name}`**", c, v))
    elif level == "class":
        cls = [cl for p in pk for cl in p.iter("class") if re.fullmatch(rf"(.*\.)?{re.escape(name)}(/.*)?", cl.get("name"))]
        for cl in cls:
            rows.append(fmt(cl.get("name"), *branches(cl)))
        rows.append(fmt(f"**Tổng class `{name}` (gồm nested)**", sum(branches(c)[0] for c in cls), sum(branches(c)[1] for c in cls)))
    elif level == "project":
        p = next(p for p in pk if p.get("name") == name)
        rows.append(fmt(f"**{name}**", *branches(p)))
        rows.append(f"| _branch-rate trong file (package)_ | | | {p.get('branch-rate')} | |")
    else:
        tc = tv = 0
        for p in pk:
            c, v = branches(p); tc += c; tv += v
            rows.append(fmt(p.get("name"), c, v))
        rows.append(fmt("**Tổng solution**", tc, tv))
        rows.append(f"| _root file: branches-covered/valid_ | {root.get('branches-covered')} | {root.get('branches-valid')} | {root.get('branch-rate')} | |")
    print(HEAD); print("\n".join(rows))


main()

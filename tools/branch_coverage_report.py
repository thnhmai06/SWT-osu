#!/usr/bin/env python3
"""Tạo báo cáo branch coverage (markdown) từ coverage.cobertura.xml của Coverlet.

Dùng: branch_coverage_report.py <cobertura.xml> <method|class|project|solution> <tên> [<tên mục tiêu phụ>]
  Nhiều file cobertura (phân cách bằng dấu phẩy): gộp rồi in tổng theo package + tổng solution.
  method   : <tên> = tên method (ví dụ applyMigrationsForVersion)
  class    : <tên> = tên class (ví dụ EditorBeatmap); gộp cả nested/closure class
  project  : <tên> = tên package/assembly (ví dụ osu.Game.Rulesets.Osu)
  solution : <tên> = bỏ qua; cộng dồn mọi package
Tuỳ chọn (đứng sau các tham số trên):
  --exclude a.json,b.json  JSON của `ConditionCoverage list-excluded`; in thêm bảng "Pure logic only" + số branch loại theo nhóm.
  --groups 4|8             4: nhóm DI/Database/Online/~* bị ẩn (như `report --hide-skipped true`); 8: tách đủ nhóm (mặc định 8).
  --expect C/V             tổng whole phải đúng bằng C/V, sai thì dừng.
Branch lấy từ <line branch="True" condition-coverage="x% (c/v)">; branches-covered/valid = tổng c/v.
"""
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict


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


def merged_packages(paths):
    """Gộp nhiều file Cobertura (mỗi project test một file): mỗi line lấy số branch phủ lớn nhất giữa các lần chạy."""
    best = {}
    for path in paths:
        for p in ET.parse(path).getroot().findall(".//package"):
            for cl in p.iter("class"):
                for m in cl.iter("method"):
                    for ln in m.iter("line"):
                        mm = re.search(r"\((\d+)/(\d+)\)", ln.get("condition-coverage", ""))
                        if ln.get("branch") == "True" and mm:
                            k = (p.get("name"), cl.get("name"), m.get("name"), m.get("signature"), ln.get("number"))
                            c, v = best.get(k, (0, 0))
                            best[k] = (max(c, int(mm[1])), int(mm[2]))
    pk = {}
    for k, (c, v) in best.items():
        a, b = pk.get(k[0], (0, 0))
        pk[k[0]] = (a + c, b + v)
    return pk


def merged_entries(paths):
    """Mỗi line branch (package, class, method, signature, line) một lần, lấy covered lớn nhất giữa các file; kèm filename của class."""
    best = {}
    for path in paths:
        for p in ET.parse(path).getroot().findall(".//package"):
            for cl in p.iter("class"):
                for m in cl.iter("method"):
                    for ln in m.iter("line"):
                        mm = re.search(r"\((\d+)/(\d+)\)", ln.get("condition-coverage", ""))
                        if ln.get("branch") == "True" and mm:
                            k = (p.get("name"), cl.get("name"), m.get("name"), m.get("signature"), int(ln.get("number")))
                            c, _ = best.get(k, (0, 0, ""))[:2]
                            best[k] = (max(c, int(mm[1])), int(mm[2]), cl.get("filename"))
    return [(*k, c, v, f) for k, (c, v, f) in best.items()]


def in_scope(e, level, name):
    pkg, cls, meth = e[0], e[1], e[2]
    if level == "method":
        return meth == name
    if level == "class":
        return re.fullmatch(rf"(.*\.)?{re.escape(name)}(/.*)?", cls) is not None
    if level == "project":
        return pkg == name
    return True


class Classifier:
    """Tra (file, line) → nhóm loại thô (giữ tiền tố ~), '' nếu logic thuần. Type lồng trong cùng file: lấy type hẹp nhất; không có thì member."""

    def __init__(self, jsons):
        self.files, self.spans = set(), defaultdict(list)
        for j in jsons:
            d = json.load(open(j, encoding="utf-8"))
            self.files.update(d["Files"])
            for sp in d["Spans"]:
                self.spans[sp["File"]].append(sp)

    def raw(self, file, line):
        if file not in self.files:
            raise SystemExit(f"LỖI: file Cobertura chưa được phân loại (không có trong list-excluded): {file}")
        cand = [sp for sp in self.spans.get(file, []) if sp["StartLine"] <= line <= sp["EndLine"]]
        for kind in ("type", "member"):
            k = [sp for sp in cand if sp["Kind"] == kind]
            if k:
                return min(k, key=lambda sp: sp["EndLine"] - sp["StartLine"])["Group"]
        return ""


HIDDEN = "Ngoài phạm vi (ẩn khỏi báo cáo condition)"
ORDER8 = ["UI", "Input", "Host/IO", "Database/Online", "DI", "TestInfra"]
ORDER4 = ["UI", "Input", "Host/IO", "TestInfra", HIDDEN]


def bucket(raw, groups):
    if raw == "":
        return "pure"
    if groups == 4 and (raw in ("DI", "Database/Online") or raw.startswith("~")):
        return HIDDEN
    return raw.lstrip("~")


def ratio(c, v):
    return "0/0 (không có branch)" if v == 0 else f"{c}/{v} ({100 * c / v:.2f}%)"


def pure_logic(paths, level, name, jsons, groups, expect):
    ents = [e for e in merged_entries(paths) if in_scope(e, level, name)]
    cl = Classifier(jsons)
    order = ORDER8 if groups == 8 else ORDER4
    per = defaultdict(lambda: defaultdict(lambda: [0, 0]))  # package -> bucket -> [c, v]
    for pkg, cls, meth, sig, line, c, v, fn in ents:
        b = bucket(cl.raw(fn, line), groups)
        per[pkg][b][0] += c
        per[pkg][b][1] += v
    pkgs = sorted(per)
    tot = defaultdict(lambda: [0, 0])
    rows_whole, rows_pure, rows_ex = [], [], []
    for pkg in pkgs + ["**Tổng**"]:
        d = tot if pkg == "**Tổng**" else per[pkg]
        wc = sum(x[0] for x in d.values()); wv = sum(x[1] for x in d.values())
        pc, pv = d["pure"] if "pure" in d else [0, 0]
        ec = sum(d[g][0] for g in order if g in d); ev = sum(d[g][1] for g in order if g in d)
        assert wc == pc + ec and wv == pv + ev, f"whole != pure + excluded cho {pkg}"
        rows_whole.append(f"| {pkg} | {wc} | {wv} | {(100 * wc / wv if wv else 0):.2f}% |")
        rows_pure.append(f"| {pkg} | {pc} | {pv} | {(f'{100 * pc / pv:.2f}%' if pv else '0/0 (không có branch)')} |")
        rows_ex.append(f"| {pkg} | " + " | ".join(f"{d[g][0]}/{d[g][1]}" if g in d else "0/0" for g in order) + f" | {ec}/{ev} |")
        if pkg != "**Tổng**":
            for g, x in d.items():
                tot[g][0] += x[0]; tot[g][1] += x[1]
    if expect is not None:
        tc = sum(x[0] for x in tot.values()); tv = sum(x[1] for x in tot.values())
        assert (tc, tv) == expect, f"whole {tc}/{tv} khác kỳ vọng {expect[0]}/{expect[1]}"
    out = ["### Whole (toàn bộ, giữ nguyên số ở trên)", "", "| Project | branches-covered | branches-valid | % |", "|---|---|---|---|", *rows_whole, "",
           "### Pure logic only (chỉ logic thuần)", "", "| Project | branches-covered | branches-valid | % |", "|---|---|---|---|", *rows_pure, "",
           "### Branch bị loại theo nhóm (covered/valid)", "", "| Project | " + " | ".join(order) + " | Tổng bị loại |", "|---|" + "---|" * (len(order) + 1), *rows_ex, "",
           "Kiểm tra: whole = pure logic + tổng các nhóm bị loại (đúng cho từng project và tổng)."]
    return "\n".join(out)


def main():
    argv = sys.argv[1:]
    opts = {}
    for key in ("--exclude", "--groups", "--expect"):
        if key in argv:
            i = argv.index(key); opts[key] = argv[i + 1]; del argv[i:i + 2]
    path, level, name = argv[0], argv[1], (argv[2] if len(argv) > 2 else "")
    if "--exclude" in opts:
        expect = tuple(int(x) for x in opts["--expect"].split("/")) if "--expect" in opts else None
        print(pure_logic(path.split(","), level, name, opts["--exclude"].split(","), int(opts.get("--groups", 8)), expect))
        return
    if "," in path:
        tot = merged_packages(path.split(","))
        print(HEAD)
        for n, (c, v) in sorted(tot.items()):
            print(fmt(n, c, v))
        print(fmt("**Tổng solution**", sum(c for c, _ in tot.values()), sum(v for _, v in tot.values())))
        return
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

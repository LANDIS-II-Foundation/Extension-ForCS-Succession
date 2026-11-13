#!/usr/bin/env python3
"""Check a case's totals against the statements in its `checks` file.

Usage: check.py <totals.csv> <checks> <cases_dir> [<work_dir> <case> ...]

Each line of <checks> is one statement about a column of totals.csv in one year:

  <column> <time> <op> <value>

<op> is one of == != > >= < <=. <value> is a number, or case:<name> for the same column and
year in that case's results: from <work_dir>/<name>/digest/totals.csv if <name> is one of the
listed cases (those run with this one), and otherwise from testing/cases/<name>/expected/totals.csv.
Blank lines and lines starting with # are ignored. Exits 1 if any statement is false.
"""
import csv
import operator
import pathlib
import sys

OPS = {"==": operator.eq, "!=": operator.ne, ">": operator.gt, ">=": operator.ge,
       "<": operator.lt, "<=": operator.le}


def read(path):
    with open(path, newline="") as fh:
        return {int(r["Time"]): r for r in csv.DictReader(fh)}


def main(totals, checks, cases, work=None, *ran):
    tot = read(totals)
    failed = 0
    for n, line in enumerate(pathlib.Path(checks).read_text().splitlines(), 1):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        try:
            col, t, op, ref = line.split()
            t = int(t)
            if ref.startswith("case:"):
                name = ref[5:]
                if name in ran:
                    other = read(pathlib.Path(work) / name / "digest" / "totals.csv")
                else:
                    other = read(pathlib.Path(cases) / name / "expected" / "totals.csv")
                want = float(other[t][col])
            else:
                want = float(ref)
            got = float(tot[t][col])
            ok = OPS[op](got, want)
        except (ValueError, KeyError, OSError) as e:
            sys.exit(f"check.py: {checks} line {n}: cannot evaluate '{line}': {e!r}")
        print(f"{'ok  ' if ok else 'FAIL'} {line}  ({col} at {t} = {got:g}, against {want:g})")
        failed += not ok
    if failed:
        sys.exit(f"check.py: {failed} check(s) failed")


if __name__ == "__main__":
    if len(sys.argv) < 4:
        sys.exit(__doc__)
    main(*sys.argv[1:])

#!/usr/bin/env python3
"""Summarise a ForC Succession run and check its carbon balance.

Usage: digest.py <run_dir> <out_dir>

Writes to <out_dir>:
  totals.csv      per year: cells, and landscape sums of ABio, BBio, TotalDOM, NPP, Rh, NEP, NBP
                  and ToFPS (log_Summary.csv), plus the number of records in log_FluxBio.csv and
                  log_FluxDOM.csv (disturbance transfers). Readable in a diff, so a change in
                  behaviour shows up in review.
  sha256sums.txt  checksums of the ForC logs, which catch any change at all.

Carbon balance, on every cell and every year that both log_BiomassC.csv and log_Pools.csv cover:
  Eco(t) - Eco(t-1) = NBP(t), Eco = live carbon (Wood + Leaf + CrsRoot + FineRoot, all cohorts)
  + dead organic matter (the 10 DOM pools, all species).
NBP is NEP less the carbon disturbances send to the air and to products, so the identity holds
when every gram leaving the live pools is booked somewhere. The logs print one decimal, so the
check allows 0.05 g C m-2 for every printed value the identity sums. Exits 1 if it fails.
"""
import csv
import hashlib
import pathlib
import sys
from collections import defaultdict

LOGS = ["log_Summary.csv", "log_Pools.csv", "log_BiomassC.csv", "log_Flux.csv",
        "log_FluxBio.csv", "log_FluxDOM.csv"]
SUMMARY = ["ABio", "BBio", "TotalDOM", "NPP", "Rh", "NEP", "NBP", "ToFPS"]
POOLS = ["VF_A", "VF_B", "Fast_A", "Fast_B", "MED", "Slow_A", "Slow_B", "Sng_Stem", "Sng_Oth", "Extra"]
LIVE = ["Wood", "Leaf", "CrsRoot", "FineRoot"]
ROUNDING = 0.05  # half the last printed digit


def rows(path):
    if not path.exists():
        return
    with open(path, newline="") as fh:
        r = csv.reader(fh)
        hdr = [h.strip() for h in next(r)]
        for rec in r:
            rec = [x.strip() for x in rec]
            if any(rec):
                yield dict(zip(hdr, rec))


def key(r):
    return int(r["Time"]), int(r["row"]), int(r["column"])


def main(run, out):
    run, out = pathlib.Path(run), pathlib.Path(out)
    out.mkdir(parents=True, exist_ok=True)

    with open(out / "sha256sums.txt", "w") as fh:
        for f in LOGS:
            if (run / f).exists():
                fh.write(f"{hashlib.sha256((run / f).read_bytes()).hexdigest()}  {f}\n")

    tot = defaultdict(lambda: defaultdict(float))
    summ = {}
    for r in rows(run / "log_Summary.csv"):
        t = int(r["Time"])
        tot[t]["cells"] += 1
        for c in SUMMARY:
            tot[t][c] += float(r[c])
        summ[key(r)] = float(r["NBP"])
    for f, col in (("log_FluxBio.csv", "FluxBio_records"), ("log_FluxDOM.csv", "FluxDOM_records")):
        for r in rows(run / f):
            tot[int(r["Time"])][col] += 1
    if not tot:
        sys.exit("digest.py: no rows in log_Summary.csv")
    cols = ["cells"] + SUMMARY + ["FluxBio_records", "FluxDOM_records"]
    with open(out / "totals.csv", "w") as fh:
        fh.write("Time," + ",".join(cols) + "\n")
        for t in sorted(tot):
            v = [f"{tot[t][c]:.0f}" if c in ("cells", "FluxBio_records", "FluxDOM_records")
                 else f"{tot[t][c]:.1f}" for c in cols]
            fh.write(f"{t}," + ",".join(v) + "\n")

    live, dom = defaultdict(float), defaultdict(float)
    terms = defaultdict(int)  # printed values summed into each cell-year's Eco
    for r in rows(run / "log_BiomassC.csv"):
        live[key(r)] += sum(float(r[c]) for c in LIVE)
        terms[key(r)] += len(LIVE)
    for r in rows(run / "log_Pools.csv"):
        dom[key(r)] += sum(float(r[c]) for c in POOLS)
        terms[key(r)] += len(POOLS)
    checked, worst, where = 0, None, None
    for k, nbp in summ.items():
        p = (k[0] - 1, k[1], k[2])
        if k not in dom or p not in dom:
            continue
        res = (live[k] + dom[k]) - (live[p] + dom[p]) - nbp
        allowed = ROUNDING * (terms[k] + terms[p] + 1)
        checked += 1
        if worst is None or abs(res) / allowed > abs(worst[0]) / worst[1]:
            worst, where = (res, allowed), k
    if checked == 0:
        print("carbon balance: not checked (needs log_BiomassC and log_Pools every year)")
        return
    res, allowed = worst
    print(f"carbon balance: {checked} cell-years; closest to failing: |dEco - NBP| = {abs(res):.2f} "
          f"g C m-2 (allowed {allowed:.2f}) at time {where[0]}, cell ({where[1]}, {where[2]})")
    if abs(res) > allowed:
        sys.exit(f"digest.py: carbon balance fails by {res:.2f} g C m-2 (allowed {allowed:.2f})")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])

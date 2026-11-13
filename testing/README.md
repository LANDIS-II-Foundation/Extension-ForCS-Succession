# Testing

The spreadsheets and `v8 Scenario/` are the earlier, hand-run testing material. The automated test cases are in `cases/`; `v8 Scenario` also runs as one of them, `v8-scenario`.

Each case is a small LANDIS-II scenario that runs ForC Succession. It runs inside the LANDIS-II Docker image, with a freshly built ForC Succession DLL mounted over the image's copy. For each run, `digest.py` writes:

- `totals.csv`: landscape totals per year from `log_Summary.csv` (ABio, BBio, TotalDOM, NPP, Rh, NEP, NBP, ToFPS), and the number of disturbance records in `log_FluxBio.csv` and `log_FluxDOM.csv`;
- `sha256sums.txt`: checksums of the six ForC logs.

A case passes when both files match its `expected/` copies, when every cell passes the carbon balance in every year, and when the statements in its `checks` file, if it has one, hold. The balance requires the change in live carbon plus dead organic matter to equal NBP, to within the rounding of the printed logs (0.05 g C m-2 per value summed).

The workflow `.github/workflows/test-cases.yml` runs every case on pull requests that change `src/` or `testing/`.

## Run locally

Needs Docker and Python 3.

```
testing/build.sh <landis image@sha256:...> build                 # build the DLL into build/
testing/run-cases.sh build/Landis.Extension.Succession.ForC-v4.dll <landis image@sha256:...>
```

The image digest the workflow uses is `LANDIS_IMAGE` in `.github/workflows/test-cases.yml`. Each case runs in `testing/work/<name>/`, which keeps the outputs, `console.log`, and the diffs of a failing case.

## A change that is meant to change results

Run with `--update` to rewrite `expected/`, then commit the new files with the change. The diff of `totals.csv` shows the effect in review. A case that fails its balance or its checks is not updated.

```
testing/run-cases.sh --update build/Landis.Extension.Succession.ForC-v4.dll <landis image@sha256:...>
```

## Adding a case

Create `testing/cases/<name>/` with either:

- `inputs/`, holding the scenario file and every input it reads; or
- a file `source`, holding one line that names an input directory relative to the repository root, e.g. `testing/v8 Scenario`.

The scenario file is `scenario.txt` unless a file `scenario` in the case names another.

Keep cases small: a few cells, a few years, and every ForC output written every year (`ForCSOutput 1 1 1 1`), so the balance check covers every year. Then run with `--update` to write `expected/`.

A case can also state what its outputs must show, in a file `checks`, one statement a line, `<column> <time> <op> <value>`:

```
# Fire burns every cell but kills no cohort; the fire's DOM rows still act.
FluxBio_records 1 == 0
FluxDOM_records 1 > 0
```

The column is one of `totals.csv`'s, and `<op>` is one of `== != > >= < <=`. A value `case:<name>` takes the same column and year from that case, as in `spinup-no-fire-dom`'s `TotalDOM 1 != case:dom-a-spinup`: from its results in the same run if it ran, and otherwise from its `expected/totals.csv`. All cases run before any is checked. `check.py` evaluates them.

## Cases

| Case | What it exercises |
|---|---|
| `v8-scenario` | `testing/v8 Scenario`: 9 x 9 cells, 100 years, ForC Succession only |
| `dom-a-spinup` | 6 x 6 cells, Hw and Bl, soil and biomass spin-up, one year, no disturbance |
| `dom-b-fire` | as `dom-a-spinup`, plus Dynamic Fire and Dynamic Fuels |
| `dom-c-clearcut` | as `dom-a-spinup`, plus a Biomass Harvest clearcut |
| `dom-d-wind` | as `dom-a-spinup`, plus Original Wind |
| `fire-no-death` | as `dom-b-fire`, with species too fire-tolerant to die; the fire's DOM rows still act (#9) |
| `harvest-no-cut` | as `dom-c-clearcut`, with a prescription that matches no cohort; the harvest DOM rows still act (#9) |
| `partial-cut` | Biomass Harvest cutting 30 % of a single cohort (#11) |
| `spinup-no-fire-dom` | as `dom-a-spinup`, with no fire DOM rows; must differ from `dom-a-spinup` (#9) |
| `spinup-off` | as `dom-a-spinup`, with the soil spin-up off (DOM from `AmountT0`) |
| `spinup-off-table` | as `spinup-off`, with a `SpinUpReturnIntervals` table, which must be ignored |
| `spinup-ri-young` | as `spinup-young`, with a 250-year return interval and a harvest ending the predecessor (#15) |
| `spinup-ri-young-fire` | as `spinup-ri-young`, with a severity-4 fire ending the predecessor (#15) |
| `spinup-ri-mortality-fraction` | as `spinup-ri-young`, with a 450-year return interval (longer than Bl's longevity) and `SpinupMortalityFraction` 0.02; age mortality plus the fraction is capped at the cohort's biomass |
| `spinup-ri-map` | as `spinup-ri-young`, with a `SpinUpLastDisturbanceMap` giving the cell a fire; must equal `spinup-ri-young-fire` (#15) |
| `spinup-young` | one cell, a 25-year-old Hw cohort, with Bl listed but absent (#14) |
| `spinup-young-bl-zero` | as `spinup-young`, with Bl's `AmountT0` at 0; must be identical to `spinup-young` (#14) |
| `wind-no-rows` | as `dom-d-wind`, with no wind rows in the disturbance matrix; killed biomass goes to DOM |
| `wind-other-rows` | as `dom-d-wind`, with its wind rows given as `other`; wind uses them |

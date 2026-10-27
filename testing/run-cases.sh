#!/usr/bin/env bash
## Run the test cases with a ForC Succession DLL inside the LANDIS-II image, and compare each
## case's digest with its expected/ files.
##
## Usage: testing/run-cases.sh [--update] <forc_dll> <landis_image@sha256:...> [case ...]
##
## A case is a directory testing/cases/<name>/ with
##   inputs/     the scenario's input files, or a file `source` naming an input directory
##               relative to the repository root (e.g. testing/v8 Scenario);
##   scenario    optional; the name of the scenario file (default scenario.txt);
##   expected/   totals.csv and sha256sums.txt, written by digest.py.
## Each case runs in a fresh copy, testing/work/<name>/ (Docker, 2 CPUs, 4 GB), with the DLL mounted
## over the image's copy. digest.py then summarises the ForC logs and checks the carbon balance.
## --update writes the new digests into expected/ instead of comparing them.
set -euo pipefail
update=0
if [ "${1:-}" = --update ]; then update=1; shift; fi
[ $# -ge 2 ] || { sed -n '2,15p' "$0" | sed 's/^## \{0,1\}//'; exit 2; }
dll=$(cd "$(dirname "$1")" && pwd)/$(basename "$1")
img=$2
shift 2
[ -f "$dll" ] || { echo "run-cases.sh: no DLL at $dll" >&2; exit 2; }
[[ $img =~ @sha256:[0-9a-f]{64}$ ]] || { echo "run-cases.sh: pin the image by digest, got: $img" >&2; exit 2; }
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/.." && pwd)
build=/opt/landis-ii/Core-Model-v8-LINUX/build
name=$(basename "$dll")

if [ $# -gt 0 ]; then cases=("$@"); else
  mapfile -t cases < <(find "$here/cases" -mindepth 1 -maxdepth 1 -type d -printf '%f\n' | sort)
fi
[ ${#cases[@]} -gt 0 ] || { echo "run-cases.sh: no cases" >&2; exit 2; }

failed=()
for c in "${cases[@]}"; do
  cdir=$here/cases/$c
  [ -d "$cdir" ] || { echo "run-cases.sh: no case $c" >&2; exit 2; }
  if [ -f "$cdir/source" ]; then src=$repo/$(head -1 "$cdir/source"); else src=$cdir/inputs; fi
  [ -d "$src" ] || { echo "run-cases.sh: $c: no inputs at $src" >&2; exit 2; }
  scen=scenario.txt
  [ -f "$cdir/scenario" ] && scen=$(head -1 "$cdir/scenario")
  w=$here/work/$c
  rm -rf "$w"
  mkdir -p "$w"
  cp -r "$src/." "$w/"
  set +e
  docker run --rm --cpus 2 --memory 4g --user "$(id -u):$(id -g)" -e HOME=/tmp \
    -v "$w:/sim" -w /sim \
    -v "$dll:$build/extensions/$name:ro" -v "$dll:$build/Release/$name:ro" \
    --entrypoint dotnet "$img" "$build/Release/Landis.Console.dll" "$scen" > "$w/console.log" 2>&1
  rc=$?
  set -e
  if [ $rc -ne 0 ]; then
    echo "FAIL $c: LANDIS-II exited $rc (testing/work/$c/console.log)"
    tail -5 "$w/console.log" | sed 's/^/     /'
    failed+=("$c")
    continue
  fi
  if ! python3 -I "$here/digest.py" "$w" "$w/digest" > "$w/digest.log" 2>&1; then
    echo "FAIL $c: digest.py (testing/work/$c/digest.log)"
    sed 's/^/     /' "$w/digest.log" | tail -5
    failed+=("$c")
    continue
  fi
  if [ $update -eq 1 ]; then
    mkdir -p "$cdir/expected"
    cp "$w/digest/totals.csv" "$w/digest/sha256sums.txt" "$cdir/expected/"
    echo "UPDATED $c"
  elif diff -u "$cdir/expected/totals.csv" "$w/digest/totals.csv" > "$w/totals.diff" &&
    diff -u "$cdir/expected/sha256sums.txt" "$w/digest/sha256sums.txt" > "$w/sha256sums.diff"; then
    echo "PASS $c"
  else
    echo "FAIL $c: output differs from expected/ (testing/work/$c/*.diff)"
    head -20 "$w/totals.diff" "$w/sha256sums.diff" 2> /dev/null | sed 's/^/     /'
    failed+=("$c")
  fi
done

echo "run-cases.sh: ${#cases[@]} cases, ${#failed[@]} failed${failed:+: ${failed[*]}}"
[ ${#failed[@]} -eq 0 ]

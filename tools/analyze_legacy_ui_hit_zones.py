#!/usr/bin/env python3
import argparse
import fnmatch
import json
from pathlib import Path


def rect_right(rect):
    return float(rect["X"]) + float(rect["Width"])


def rect_bottom(rect):
    return float(rect["Y"]) + float(rect["Height"])


def overlap(a, b):
    left = max(float(a["X"]), float(b["X"]))
    top = max(float(a["Y"]), float(b["Y"]))
    right = min(rect_right(a), rect_right(b))
    bottom = min(rect_bottom(a), rect_bottom(b))
    width = right - left
    height = bottom - top
    if width <= 0 or height <= 0:
        return None
    return {
        "x": left,
        "y": top,
        "width": width,
        "height": height,
        "area": width * height,
    }


def is_internal_path(a_path, b_path):
    return b_path.startswith(f"{a_path}/") or a_path.startswith(f"{b_path}/")


def format_rect(rect):
    return (
        f"x={float(rect['X']):.2f} y={float(rect['Y']):.2f} "
        f"w={float(rect['Width']):.2f} h={float(rect['Height']):.2f}"
    )


def find_snapshot_files(paths):
    if not paths:
        default_dir = Path("artifacts/ui-hit-zones")
        return sorted(default_dir.glob("*-hit-zones.json"))

    files = []
    for raw_path in paths:
        path = Path(raw_path)
        if path.is_dir():
            files.extend(sorted(path.glob("*-hit-zones.json")))
        else:
            files.append(path)
    return files


def analyze_file(path, args):
    with path.open("r", encoding="utf-8") as handle:
        snapshot = json.load(handle)

    findings = []
    for context in snapshot.get("Contexts", []):
        context_name = context.get("Name", "")
        if not fnmatch.fnmatch(context_name, args.context):
            continue

        zones = [
            zone for zone in context.get("Zones", [])
            if (args.include_hidden or zone.get("Visible", False))
            and float(zone["Bounds"]["Width"]) > 0
            and float(zone["Bounds"]["Height"]) > 0
        ]

        for i, a in enumerate(zones):
            for b in zones[i + 1:]:
                if not args.include_internal and is_internal_path(a["Path"], b["Path"]):
                    continue

                hit = overlap(a["Bounds"], b["Bounds"])
                if hit is None or hit["area"] < args.min_area:
                    continue

                findings.append({
                    "file": path.name,
                    "context": context_name,
                    "area": hit["area"],
                    "overlap": hit,
                    "a": a,
                    "b": b,
                })

    return findings


def print_findings(findings, limit):
    if not findings:
        print("No overlapping hit zones found.")
        return

    findings = sorted(findings, key=lambda item: (item["file"], item["context"], -item["area"]))
    if limit > 0:
        findings = findings[:limit]

    for item in findings:
        a = item["a"]
        b = item["b"]
        hit = item["overlap"]
        print(f"{item['file']} :: {item['context']} :: area={item['area']:.2f}")
        print(f"  overlap: x={hit['x']:.2f} y={hit['y']:.2f} w={hit['width']:.2f} h={hit['height']:.2f}")
        print(f"  A: {a['Path']} [{a['Kind']}] {format_rect(a['Bounds'])}")
        print(f"  B: {b['Path']} [{b['Kind']}] {format_rect(b['Bounds'])}")


def main():
    parser = argparse.ArgumentParser(
        description="Analyze LegacyUiHitZoneSnapshot JSON files for overlapping interactable zones."
    )
    parser.add_argument(
        "paths",
        nargs="*",
        help="Snapshot files or directories. Defaults to artifacts/ui-hit-zones.",
    )
    parser.add_argument(
        "--context",
        default="*",
        help="Context glob, for example active, tab-1, or *expanded.",
    )
    parser.add_argument(
        "--min-area",
        type=float,
        default=1.0,
        help="Minimum overlap area in pixels.",
    )
    parser.add_argument(
        "--include-hidden",
        action="store_true",
        help="Include zones hidden by tab/page visibility.",
    )
    parser.add_argument(
        "--include-internal",
        action="store_true",
        help="Include parent-child overlaps inside composed controls.",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=0,
        help="Maximum findings to print. 0 means no limit.",
    )
    args = parser.parse_args()

    files = find_snapshot_files(args.paths)
    existing_files = [path for path in files if path.exists()]
    missing_files = [path for path in files if not path.exists()]

    for path in missing_files:
        print(f"warning: missing snapshot file: {path}")

    findings = []
    for path in existing_files:
        findings.extend(analyze_file(path, args))

    print(f"Analyzed {len(existing_files)} snapshot file(s).")
    print_findings(findings, args.limit)


if __name__ == "__main__":
    main()

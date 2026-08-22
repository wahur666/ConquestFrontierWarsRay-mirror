#!/usr/bin/env python3

from __future__ import annotations

import json
from pathlib import Path
import xml.etree.ElementTree as ET


def repo_root_from_script() -> Path:
    return Path(__file__).resolve().parents[3]


def typed_text(parent: ET.Element, child_name: str) -> str:
    child = parent.find(child_name)
    if child is None or child.text is None:
        raise ValueError(f"Missing text node '{child_name}'.")
    return child.text.strip()


def collect_untracked_interface_assets(interface_dir: Path, referenced_filenames: set[str]) -> tuple[dict[str, dict[str, str]], dict[str, dict[str, str]]]:
    actual_filenames_by_lower = {
        path.name.lower(): path.name for path in interface_dir.iterdir() if path.is_file()
    }
    referenced_filenames_lower = {filename.lower() for filename in referenced_filenames}
    untracked_filenames = sorted(
        (
            actual_filenames_by_lower[lower_filename]
            for lower_filename in actual_filenames_by_lower.keys() - referenced_filenames_lower
        ),
        key=str.lower,
    )

    image: dict[str, dict[str, str]] = {}
    atlas_candidates: dict[str, dict[str, str]] = {}

    for filename in untracked_filenames:
        path = Path(filename)
        extension = path.suffix.lower()

        if extension == ".tga":
            image_id = path.stem
            image[image_id] = {
                "vfxShapeId": image_id,
                "filename": filename,
            }
            continue

        if extension in {".png", ".json"} and path.stem.lower().endswith("_atlas"):
            atlas_entry = atlas_candidates.setdefault(path.stem, {})
            if extension == ".png":
                atlas_entry["filename"] = filename
            else:
                atlas_entry["metaJson"] = filename
            continue

    atlas: dict[str, dict[str, str]] = {}
    for atlas_key in sorted(atlas_candidates, key=str.lower):
        atlas_entry = atlas_candidates[atlas_key]
        atlas_id = atlas_key[:-6] if atlas_key.lower().endswith("_atlas") else atlas_key
        atlas[atlas_id] = {
            "vfxShapeId": atlas_id,
            "filename": atlas_entry.get("filename", ""),
            "metaJson": atlas_entry.get("metaJson", ""),
        }

    return (
        dict(sorted(image.items(), key=lambda item: item[0].lower())),
        atlas,
    )


def load_atlas_png_from_meta(interface_dir: Path, meta_json: str) -> str:
    atlas_json_path = interface_dir / meta_json
    if not atlas_json_path.exists():
        raise FileNotFoundError(f"Missing atlas JSON: {atlas_json_path}")

    atlas_document = json.loads(atlas_json_path.read_text(encoding="utf-8"))
    atlas_png = atlas_document.get("meta", {}).get("atlas")
    if not atlas_png or not isinstance(atlas_png, str):
        raise ValueError(f"Atlas JSON does not contain meta.atlas: {atlas_json_path}")
    return atlas_png


def main() -> int:
    repo_root = repo_root_from_script()
    assets_root = repo_root / "assets"
    animate_dir = assets_root / "DB" / "xml" / "GenData.db" / "GT_ANIMATE"
    vfx_shape_dir = assets_root / "DB" / "xml" / "GenData.db" / "GT_VFXSHAPE"
    interface_dir = assets_root / "interface"
    output_path = assets_root / "DB" / "vfx-animation-data.json"

    image: dict[str, dict[str, str]] = {}
    atlas: dict[str, dict[str, str]] = {}
    referenced_filenames: set[str] = set()

    for animate_path in sorted(animate_dir.glob("*.xml"), key=lambda path: path.name.lower()):
        animate_root = ET.fromstring(animate_path.read_text(encoding="utf-8"))
        animate_id = animate_root.attrib["id"]
        vfx_type = typed_text(animate_root, "vfxType")

        vfx_path = vfx_shape_dir / f"{vfx_type}.xml"
        if not vfx_path.exists():
            raise FileNotFoundError(f"Missing GT_VFXSHAPE XML for '{animate_id}': {vfx_path}")

        vfx_root = ET.fromstring(vfx_path.read_text(encoding="utf-8"))
        vfx_shape_id = vfx_root.attrib["id"]
        filename = typed_text(vfx_root, "filename")
        extension = Path(filename).suffix.lower()

        if extension == ".tga":
            referenced_filenames.add(filename)
            image[animate_id] = {
                "vfxShapeId": vfx_shape_id,
                "filename": filename,
            }
            continue

        if extension == ".shp":
            meta_json = f"{Path(filename).stem}_atlas.json"
            atlas_png = load_atlas_png_from_meta(interface_dir, meta_json)

            referenced_filenames.add(meta_json)
            referenced_filenames.add(atlas_png)
            atlas[animate_id] = {
                "vfxShapeId": vfx_shape_id,
                "filename": atlas_png,
                "metaJson": meta_json,
            }
            continue

        raise ValueError(f"Unsupported GT_VFXSHAPE filename extension for '{animate_id}': {filename}")

    untracked_image, untracked_atlas = collect_untracked_interface_assets(interface_dir, referenced_filenames)
    image.update(untracked_image)
    atlas.update(untracked_atlas)

    output = {
        "image": dict(sorted(image.items(), key=lambda item: item[0].lower())),
        "atlas": dict(sorted(atlas.items(), key=lambda item: item[0].lower())),
    }

    output_path.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {output_path}")
    print(f"image={len(image)} atlas={len(atlas)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

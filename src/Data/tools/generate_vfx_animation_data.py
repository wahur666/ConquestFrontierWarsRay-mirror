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


def main() -> int:
    repo_root = repo_root_from_script()
    assets_root = repo_root / "assets"
    animate_dir = assets_root / "DB" / "xml" / "GenData.db" / "GT_ANIMATE"
    vfx_shape_dir = assets_root / "DB" / "xml" / "GenData.db" / "GT_VFXSHAPE"
    interface_dir = assets_root / "interface"
    output_path = assets_root / "DB" / "vfx-animation-data.json"

    image: dict[str, dict[str, str]] = {}
    atlas: dict[str, dict[str, str]] = {}

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
            image[animate_id] = {
                "vfxShapeId": vfx_shape_id,
                "filename": filename,
            }
            continue

        if extension == ".shp":
            meta_json = f"{Path(filename).stem}_atlas.json"
            atlas_json_path = interface_dir / meta_json
            if not atlas_json_path.exists():
                raise FileNotFoundError(
                    f"Missing atlas JSON for '{animate_id}' derived from '{filename}': {atlas_json_path}"
                )

            atlas_document = json.loads(atlas_json_path.read_text(encoding="utf-8"))
            atlas_png = atlas_document.get("meta", {}).get("atlas")
            if not atlas_png or not isinstance(atlas_png, str):
                raise ValueError(
                    f"Atlas JSON for '{animate_id}' does not contain meta.atlas: {atlas_json_path}"
                )

            atlas[animate_id] = {
                "vfxShapeId": vfx_shape_id,
                "filename": atlas_png,
                "metaJson": meta_json,
            }
            continue

        raise ValueError(f"Unsupported GT_VFXSHAPE filename extension for '{animate_id}': {filename}")

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

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[4]
CONQUEST = ROOT / "src" / "Conquest"
OUT_DIR = ROOT / "ConquestFrontierWarsRay" / "src" / "Gloabls" / "Generated"

RC_PATH = CONQUEST / "Globals.rc"
RESOURCE_HEADER_PATH = CONQUEST / "resource.h"
OUTPUT_PATH = OUT_DIR / "LegacyGlobalsRc.json"
FULL_OUTPUT_PATH = OUT_DIR / "LegacyGlobalsRc.full.json"

HEADER_WITH_ID_RE = re.compile(
    r"^(?P<identifier>[A-Za-z0-9_]+)\s+"
    r"(?P<kind>TEXTINCLUDE|MENU|CURSOR|DIALOGEX|DIALOG)\b"
    r"(?P<rest>.*)$"
)
STRINGTABLE_RE = re.compile(r"^STRINGTABLE\b(?P<rest>.*)$")
DEFINE_RE = re.compile(r"^#define\s+([A-Za-z_][A-Za-z0-9_]*)\s+(.+?)\s*$")
QUOTED_STRING_RE = re.compile(r'"((?:[^"\\]|\\.)*)"')


@dataclass
class LineReader:
    lines: list[str]
    index: int = 0

    def has_next(self) -> bool:
        return self.index < len(self.lines)

    def peek(self) -> str:
        return self.lines[self.index]

    def next(self) -> tuple[int, str]:
        line_no = self.index + 1
        line = self.lines[self.index]
        self.index += 1
        return line_no, line


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def decode_rc_string(token: str) -> str:
    return bytes(token, "utf-8").decode("unicode_escape")


def extract_quoted_strings(text: str) -> list[str]:
    return [decode_rc_string(match.group(1)) for match in QUOTED_STRING_RE.finditer(text)]


def parse_resource_defines(text: str) -> dict[str, Any]:
    by_name: dict[str, int | str] = {}
    by_value: dict[str, list[str]] = {}

    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line.startswith("#define"):
            continue
        match = DEFINE_RE.match(line)
        if not match:
            continue
        name = match.group(1)
        value_text = match.group(2).strip()
        try:
            value: int | str = int(value_text, 0)
            value_key = str(value)
        except ValueError:
            value = value_text
            value_key = value_text

        by_name[name] = value
        by_value.setdefault(value_key, []).append(name)

    return {
        "by_name": by_name,
        "by_value": by_value,
    }


def split_csv_tokens(text: str) -> list[str]:
    tokens: list[str] = []
    current: list[str] = []
    in_quotes = False
    escape = False

    for char in text:
        if escape:
            current.append(char)
            escape = False
            continue
        if char == "\\":
            current.append(char)
            escape = True
            continue
        if char == '"':
            current.append(char)
            in_quotes = not in_quotes
            continue
        if char == "," and not in_quotes:
            tokens.append("".join(current).strip())
            current = []
            continue
        current.append(char)

    tail = "".join(current).strip()
    if tail:
        tokens.append(tail)
    return tokens


def collect_begin_end_block(reader: LineReader) -> dict[str, Any]:
    block_lines: list[dict[str, Any]] = []
    depth = 0

    while reader.has_next():
        line_no, line = reader.next()
        stripped = line.strip()
        block_lines.append({"line": line_no, "text": line})
        if stripped == "BEGIN":
            depth += 1
        elif stripped == "END":
            depth -= 1
            if depth == 0:
                break

    if depth != 0:
        raise RuntimeError("Unbalanced BEGIN/END block in RC file")

    return {
        "start_line": block_lines[0]["line"],
        "end_line": block_lines[-1]["line"],
        "lines": block_lines,
    }


def collect_dialog_block(reader: LineReader) -> dict[str, Any]:
    block_lines: list[dict[str, Any]] = []
    depth = 0
    saw_begin = False

    while reader.has_next():
        line_no, line = reader.next()
        stripped = line.strip()
        block_lines.append({"line": line_no, "text": line})
        if stripped == "BEGIN":
            depth += 1
            saw_begin = True
        elif stripped == "END":
            depth -= 1
            if saw_begin and depth == 0:
                break

    if not saw_begin or depth != 0:
        raise RuntimeError("Unbalanced dialog block in RC file")

    return {
        "start_line": block_lines[0]["line"],
        "end_line": block_lines[-1]["line"],
        "lines": block_lines,
    }


def parse_menu_block(block: dict[str, Any]) -> dict[str, Any]:
    lines = block["lines"]
    index = 1  # skip BEGIN

    def parse_nodes() -> list[dict[str, Any]]:
        nonlocal index
        nodes: list[dict[str, Any]] = []
        while index < len(lines):
            current = lines[index]
            text = current["text"].strip()
            if text == "END":
                index += 1
                break
            if text == "SEPARATOR":
                nodes.append({"kind": "SEPARATOR", "line": current["line"]})
                index += 1
                continue
            if text.startswith("POPUP "):
                strings = extract_quoted_strings(text)
                label = strings[0] if strings else ""
                popup = {"kind": "POPUP", "line": current["line"], "label": label}
                index += 1
                begin_line = lines[index]["text"].strip()
                if begin_line != "BEGIN":
                    raise RuntimeError(f"Expected BEGIN after POPUP at line {current['line']}")
                index += 1
                popup["children"] = parse_nodes()
                nodes.append(popup)
                continue
            if text.startswith("MENUITEM "):
                tokens = split_csv_tokens(text[len("MENUITEM "):])
                label = extract_quoted_strings(tokens[0])[0] if tokens and tokens[0].startswith('"') else tokens[0]
                item: dict[str, Any] = {
                    "kind": "MENUITEM",
                    "line": current["line"],
                    "label": label,
                }
                if len(tokens) > 1:
                    item["command"] = tokens[1]
                if len(tokens) > 2:
                    item["flags"] = tokens[2:]
                nodes.append(item)
                index += 1
                continue

            nodes.append({"kind": "RAW", "line": current["line"], "text": current["text"]})
            index += 1

        return nodes

    return {"items": parse_nodes()}


def parse_stringtable_block(block: dict[str, Any]) -> dict[str, Any]:
    entries: list[dict[str, Any]] = []
    buffer: list[dict[str, Any]] = []

    for current in block["lines"][1:-1]:
        text = current["text"].strip()
        if not text:
            continue
        if text.startswith("//"):
            continue
        buffer.append(current)
        combined = " ".join(item["text"].strip() for item in buffer)
        strings = extract_quoted_strings(combined)
        if not strings:
            continue
        identifier = buffer[0]["text"].strip().split()[0]
        value = "".join(strings)
        entries.append(
            {
                "identifier": identifier,
                "line_start": buffer[0]["line"],
                "line_end": buffer[-1]["line"],
                "value": value,
                "raw": [item["text"] for item in buffer],
            }
        )
        buffer = []

    if buffer:
        entries.append(
            {
                "identifier": buffer[0]["text"].strip().split()[0],
                "line_start": buffer[0]["line"],
                "line_end": buffer[-1]["line"],
                "value": None,
                "raw": [item["text"] for item in buffer],
            }
        )

    return {"entries": entries}


def parse_textinclude_block(block: dict[str, Any]) -> dict[str, Any]:
    content = "".join(
        decoded
        for current in block["lines"][1:-1]
        for decoded in extract_quoted_strings(current["text"])
    )
    return {"content": content}


def parse_dialog_block(header_rest: str, block: dict[str, Any]) -> dict[str, Any]:
    lines = block["lines"]
    controls: list[dict[str, Any]] = []
    properties: dict[str, Any] = {}
    raw_controls: list[dict[str, Any]] = []

    current_control: list[dict[str, Any]] = []

    def flush_control() -> None:
        nonlocal current_control
        if not current_control:
            return
        combined = " ".join(item["text"].strip() for item in current_control)
        leading = current_control[0]["text"].strip().split(None, 1)[0]
        payload = combined[len(leading):].strip()
        controls.append(
            {
                "control_type": leading,
                "line_start": current_control[0]["line"],
                "line_end": current_control[-1]["line"],
                "tokens": split_csv_tokens(payload),
                "raw": [item["text"] for item in current_control],
            }
        )
        current_control = []

    for current in lines[:-1]:
        text = current["text"].rstrip()
        stripped = text.strip()
        if not stripped:
            continue
        if stripped.startswith("STYLE "):
            properties["style"] = stripped[len("STYLE "):]
            continue
        if stripped.startswith("CAPTION "):
            strings = extract_quoted_strings(stripped)
            properties["caption"] = strings[0] if strings else stripped[len("CAPTION "):]
            continue
        if stripped.startswith("FONT "):
            properties["font"] = stripped[len("FONT "):]
            continue
        if stripped == "BEGIN":
            continue

        first_token = stripped.split(None, 1)[0]
        if first_token in {"LTEXT", "EDITTEXT", "CONTROL", "DEFPUSHBUTTON", "PUSHBUTTON", "GROUPBOX", "LISTBOX", "COMBOBOX", "RTEXT", "CTEXT", "CHECKBOX"}:
            flush_control()
            current_control = [current]
            continue

        if current_control:
            current_control.append(current)
        else:
            raw_controls.append({"line": current["line"], "text": current["text"]})

    flush_control()

    return {
        "geometry": [part.strip() for part in split_csv_tokens(header_rest.strip())] if header_rest.strip() else [],
        "properties": properties,
        "controls": controls,
        "raw_lines": raw_controls,
    }


def parse_cursor_resource(header_rest: str) -> dict[str, Any]:
    strings = extract_quoted_strings(header_rest)
    return {"path": strings[0] if strings else header_rest.strip()}


def parse_rc_file(rc_text: str, resource_defines: dict[str, Any]) -> dict[str, Any]:
    lines = rc_text.splitlines()
    reader = LineReader(lines)

    directives: list[dict[str, Any]] = []
    resources: list[dict[str, Any]] = []

    counts = {
        "textinclude": 0,
        "menu": 0,
        "cursor": 0,
        "dialog": 0,
        "stringtable": 0,
    }

    while reader.has_next():
        line_no, line = reader.next()
        stripped = line.strip()
        if not stripped:
            continue

        match = HEADER_WITH_ID_RE.match(stripped)
        if match or STRINGTABLE_RE.match(stripped):
            if match:
                identifier = match.group("identifier")
                kind = match.group("kind")
                rest = match.group("rest")
            else:
                identifier = None
                kind = "STRINGTABLE"
                rest = STRINGTABLE_RE.match(stripped).group("rest")

            if kind in {"TEXTINCLUDE", "MENU", "DIALOG", "DIALOGEX", "STRINGTABLE"}:
                if kind == "STRINGTABLE":
                    header_identifier = None
                else:
                    header_identifier = identifier

                if kind in {"DIALOG", "DIALOGEX"}:
                    block = collect_dialog_block(reader)
                else:
                    while reader.has_next() and not reader.peek().strip():
                        reader.next()
                    if not reader.has_next() or reader.peek().strip() != "BEGIN":
                        raise RuntimeError(f"Expected BEGIN after {kind} at line {line_no}")
                    block = collect_begin_end_block(reader)

                entry: dict[str, Any] = {
                    "kind": kind,
                    "identifier": header_identifier,
                    "line_start": line_no,
                    "line_end": block["end_line"],
                    "header": stripped,
                    "header_rest": rest.strip(),
                    "raw_block": [item["text"] for item in block["lines"]],
                }

                if kind == "TEXTINCLUDE":
                    counts["textinclude"] += 1
                    entry["parsed"] = parse_textinclude_block(block)
                elif kind == "MENU":
                    counts["menu"] += 1
                    entry["parsed"] = parse_menu_block(block)
                elif kind in {"DIALOG", "DIALOGEX"}:
                    counts["dialog"] += 1
                    entry["parsed"] = parse_dialog_block(rest, block)
                elif kind == "STRINGTABLE":
                    counts["stringtable"] += 1
                    entry["parsed"] = parse_stringtable_block(block)

                if header_identifier and header_identifier in resource_defines["by_name"]:
                    entry["resource_id"] = resource_defines["by_name"][header_identifier]

                resources.append(entry)
                continue

            if kind == "CURSOR":
                counts["cursor"] += 1
                entry = {
                    "kind": kind,
                    "identifier": identifier,
                    "line_start": line_no,
                    "line_end": line_no,
                    "header": stripped,
                    "header_rest": rest.strip(),
                    "parsed": parse_cursor_resource(rest),
                }
                if identifier in resource_defines["by_name"]:
                    entry["resource_id"] = resource_defines["by_name"][identifier]
                resources.append(entry)
                continue

        directives.append({"line": line_no, "text": line})

    return {
        "source_file": str(RC_PATH),
        "resource_header": str(RESOURCE_HEADER_PATH),
        "counts": counts,
        "directives": directives,
        "resources": resources,
    }


def build_output() -> dict[str, Any]:
    resource_defines = parse_resource_defines(read_text(RESOURCE_HEADER_PATH))
    rc = parse_rc_file(read_text(RC_PATH), resource_defines)

    return {
        "schema": "conquest.globals.rc.v1",
        "source": {
            "rc_file": rc["source_file"],
            "resource_header_file": rc["resource_header"],
        },
        "resource_defines": resource_defines,
        "summary": {
            "directive_count": len(rc["directives"]),
            "resource_count": len(rc["resources"]),
            **rc["counts"],
        },
        "directives": rc["directives"],
        "resources": rc["resources"],
    }


def classify_string_identifier(identifier: str) -> str:
    if not identifier.startswith("IDS_"):
        return "other"
    remainder = identifier[4:]
    if "_" not in remainder:
        return remainder.lower()
    return remainder.split("_", 1)[0].lower()


def simplify_menu_item(item: dict[str, Any]) -> dict[str, Any]:
    simplified = {"kind": item["kind"]}
    if "label" in item:
        simplified["label"] = item["label"]
    if "command" in item:
        simplified["command"] = item["command"]
    if "flags" in item:
        simplified["flags"] = item["flags"]
    if "children" in item:
        simplified["children"] = [simplify_menu_item(child) for child in item["children"]]
    return simplified


def simplify_dialog_control(control: dict[str, Any]) -> dict[str, Any]:
    tokens = control["tokens"]
    simplified: dict[str, Any] = {
        "kind": control["control_type"],
        "line_start": control["line_start"],
        "line_end": control["line_end"],
    }
    if tokens:
        simplified["args"] = tokens
    if tokens and tokens[0].startswith('"'):
        strings = extract_quoted_strings(tokens[0])
        if strings:
            simplified["text"] = strings[0]
    return simplified


def build_compact_output(full: dict[str, Any]) -> dict[str, Any]:
    resources = full["resources"]
    defines = full["resource_defines"]["by_name"]

    textincludes: list[dict[str, Any]] = []
    menus: list[dict[str, Any]] = []
    cursors: list[dict[str, Any]] = []
    dialogs: list[dict[str, Any]] = []
    string_blocks: list[dict[str, Any]] = []
    string_entries: list[dict[str, Any]] = []
    string_prefix_counts: dict[str, int] = {}

    for resource in resources:
        kind = resource["kind"]
        identifier = resource.get("identifier")
        if kind == "TEXTINCLUDE":
            textincludes.append(
                {
                    "id": identifier,
                    "line_start": resource["line_start"],
                    "line_end": resource["line_end"],
                    "content": resource["parsed"]["content"],
                }
            )
        elif kind == "MENU":
            menus.append(
                {
                    "name": identifier,
                    "id": defines.get(identifier),
                    "line_start": resource["line_start"],
                    "line_end": resource["line_end"],
                    "items": [simplify_menu_item(item) for item in resource["parsed"]["items"]],
                }
            )
        elif kind == "CURSOR":
            path = resource["parsed"]["path"]
            cursors.append(
                {
                    "name": identifier,
                    "id": defines.get(identifier),
                    "line": resource["line_start"],
                    "path": path,
                    "variant": "bw" if "\\BW\\" in path or "\\bw\\" in path else "color",
                }
            )
        elif kind in {"DIALOG", "DIALOGEX"}:
            dialogs.append(
                {
                    "name": identifier,
                    "id": defines.get(identifier),
                    "kind": kind,
                    "line_start": resource["line_start"],
                    "line_end": resource["line_end"],
                    "geometry": resource["parsed"]["geometry"],
                    "caption": resource["parsed"]["properties"].get("caption"),
                    "style": resource["parsed"]["properties"].get("style"),
                    "font": resource["parsed"]["properties"].get("font"),
                    "controls": [simplify_dialog_control(control) for control in resource["parsed"]["controls"]],
                }
            )
        elif kind == "STRINGTABLE":
            block_entries: list[dict[str, Any]] = []
            for entry in resource["parsed"]["entries"]:
                identifier = entry["identifier"]
                prefix = classify_string_identifier(identifier)
                string_prefix_counts[prefix] = string_prefix_counts.get(prefix, 0) + 1
                flat_entry = {
                    "name": identifier,
                    "id": defines.get(identifier),
                    "prefix": prefix,
                    "line_start": entry["line_start"],
                    "line_end": entry["line_end"],
                    "value": entry["value"],
                }
                block_entries.append(flat_entry)
                string_entries.append(flat_entry)

            string_blocks.append(
                {
                    "line_start": resource["line_start"],
                    "line_end": resource["line_end"],
                    "entry_count": len(block_entries),
                    "entries": block_entries,
                }
            )

    cursor_groups: dict[str, list[dict[str, Any]]] = {"color": [], "bw": []}
    for cursor in cursors:
        cursor_groups[cursor["variant"]].append(cursor)

    return {
        "schema": "conquest.globals.rc.v2",
        "source": full["source"],
        "summary": {
            **full["summary"],
            "string_entry_count": len(string_entries),
            "string_prefix_counts": dict(sorted(string_prefix_counts.items())),
        },
        "text_includes": textincludes,
        "menus": menus,
        "cursors": {
            "all": cursors,
            "by_variant": cursor_groups,
        },
        "dialogs": dialogs,
        "strings": {
            "blocks": string_blocks,
            "entries": string_entries,
        },
    }


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    full_payload = build_output()
    compact_payload = build_compact_output(full_payload)
    FULL_OUTPUT_PATH.write_text(json.dumps(full_payload, indent=2), encoding="utf-8")
    OUTPUT_PATH.write_text(json.dumps(compact_payload, indent=2), encoding="utf-8")
    print(f"Wrote {OUTPUT_PATH}")
    print(f"Wrote {FULL_OUTPUT_PATH}")
    print(json.dumps(compact_payload["summary"], indent=2))


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
Convert the latest CHANGELOG.md section to Steam Workshop BBCode.

Usage:
    python3 changelog_to_bbcode.py              # latest version
    python3 changelog_to_bbcode.py 2.1.0        # specific version
    python3 changelog_to_bbcode.py | xclip -sel clip   # copy to clipboard
"""

import re
import sys
import json
from pathlib import Path

ROOT = Path(__file__).parent


def get_version(arg=None):
    if arg:
        return arg
    manifest = ROOT / ".release-please-manifest.json"
    if manifest.exists():
        return json.loads(manifest.read_text())["."]
    raise SystemExit("No version argument and .release-please-manifest.json not found.")


def extract_section(changelog: str, version: str) -> str:
    lines = changelog.splitlines()
    out, found = [], False
    for line in lines:
        if re.match(rf"^## \[{re.escape(version)}\]", line):
            found = True
            continue
        if found and re.match(r"^## \[", line):
            break
        if found:
            out.append(line)
    # strip leading/trailing blank lines
    while out and not out[0].strip():
        out.pop(0)
    while out and not out[-1].strip():
        out.pop()
    return "\n".join(out)


def md_to_bb(text: str) -> str:
    # URLs first — before bold/code introduce extra brackets
    text = re.sub(r'\[([^\[\]]+)\]\(([^)]+)\)', r'[url=\2]\1[/url]', text)
    text = re.sub(r'\*\*(.+?)\*\*', r'[b]\1[/b]', text)
    text = re.sub(r'`(.+?)`',        r'[b]\1[/b]', text)
    return text


def convert(section: str) -> str:
    lines = section.splitlines()
    out, in_list = [], False

    for line in lines:
        # heading
        m = re.match(r'^#{1,3}\s+(.*)', line)
        if m:
            if in_list:
                out.append('[/list]')
                in_list = False
            out.append(f'[h1]{m.group(1).strip()}[/h1]')
            continue

        # bullet
        m = re.match(r'^[\*\-]\s+(.*)', line)
        if m:
            if not in_list:
                out.append('[list]')
                in_list = True
            out.append(f'[*] {md_to_bb(m.group(1).strip())}')
            continue

        if in_list:
            out.append('[/list]')
            in_list = False

        if line.strip() == '':
            out.append('')
            continue

        out.append(md_to_bb(line))

    if in_list:
        out.append('[/list]')

    return '\n'.join(out)


def main():
    version = get_version(sys.argv[1] if len(sys.argv) > 1 else None)
    changelog = (ROOT / "CHANGELOG.md").read_text()
    section = extract_section(changelog, version)

    if not section:
        print(f"No section found for version {version} in CHANGELOG.md", file=sys.stderr)
        sys.exit(1)

    bbcode = convert(section)
    release_url = f"https://github.com/roberto-naharro/BuildingThemes2/releases/tag/v{version}"
    output = f"{bbcode}\n\n[url={release_url}]Full release notes on GitHub[/url]"

    print(output)


if __name__ == "__main__":
    main()

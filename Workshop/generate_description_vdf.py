#!/usr/bin/env python3
"""Generate /tmp/description.vdf for steamcmd — description-only update (no content upload).

Used by .github/workflows/workshop-update-description.yml.
Omits contentfolder and previewfile so only the Workshop page description is updated.
"""
import os

workspace = os.environ['GITHUB_WORKSPACE']

desc_path = os.path.join(workspace, 'Workshop', 'description.txt')
with open(desc_path, 'r') as f:
    description = f.read()


def vdf_escape(s):
    """Escape a string for use as a VDF value (single-line or multiline via \\n)."""
    return s.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '').replace('\n', '\\n')


vdf = (
    '"workshopitem"\n'
    '{\n'
    '\t"appid"\t\t\t"255710"\n'
    '\t"publishedfileid"\t"3708105182"\n'
    '\t"description"\t\t"' + vdf_escape(description) + '"\n'
    '\t"changenote"\t\t"Description update"\n'
    '}\n'
)

with open('/tmp/description.vdf', 'w') as f:
    f.write(vdf)

print("Generated /tmp/description.vdf")

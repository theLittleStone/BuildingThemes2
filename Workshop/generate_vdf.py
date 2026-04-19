#!/usr/bin/env python3
"""Generate /tmp/item.vdf for steamcmd workshop_build_item.

Reads GITHUB_WORKSPACE and CHANGE_NOTE from the environment.
Used by .github/workflows/workshop-deploy.yml.
"""
import os

workspace = os.environ['GITHUB_WORKSPACE']
note = os.environ.get('CHANGE_NOTE', 'Update')

desc_path = os.path.join(workspace, 'Workshop', 'description.txt')


def vdf_escape(s):
    """Escape a string for use as a VDF single-line value."""
    return s.replace('\\', '\\\\').replace('"', '\\"').replace('\r', '').replace('\n', ' ')


vdf = (
    '"workshopitem"\n'
    '{\n'
    '\t"appid"\t\t\t"255710"\n'
    '\t"publishedfileid"\t"3708105182"\n'
    '\t"contentfolder"\t\t"' + workspace + '/dist/BuildingThemes2"\n'
    '\t"previewfile"\t\t"' + workspace + '/Workshop/PreviewImage.png"\n'
    '\t"descriptionfile"\t"' + desc_path + '"\n'
    '\t"changenote"\t\t"' + vdf_escape(note)[:7900] + '"\n'
    '}\n'
)

with open('/tmp/item.vdf', 'w') as f:
    f.write(vdf)

print("Generated /tmp/item.vdf:")
print(vdf)

#!/usr/bin/env python3
"""Rewrite href="/..." and action="/..." to AppPath.H in Razor views."""
import re
import pathlib

root = pathlib.Path(__file__).resolve().parents[1] / "src" / "MovasseghiShop.Web"
skip = {"_AppPathScript.cshtml"}

href_re = re.compile(r'href="(/[^"]*)"')
action_re = re.compile(r'action="(/[^"]*)"')


def repl(m: re.Match) -> str:
    path = m.group(1)
    full = m.group(0)
    if "AppPath.H" in full or "@Url." in full or "@" in path:
        return full
    esc = path.replace("\\", "\\\\").replace('"', '\\"')
    attr = full.split("=", 1)[0]
    return f'{attr}="@AppPath.H("{esc}", Context)"'


for p in root.rglob("*.cshtml"):
    if p.name in skip:
        continue
    text = p.read_text(encoding="utf-8")
    new = href_re.sub(repl, text)
    new = action_re.sub(repl, new)
    if new != text:
        p.write_text(new, encoding="utf-8")
        print("fixed", p.relative_to(root.parent.parent))

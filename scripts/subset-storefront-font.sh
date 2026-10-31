#!/usr/bin/env bash
# Optional regeneration, not a deployment dependency: pip install fonttools brotli
# The original remains the fallback for every character outside this coverage.
set -euo pipefail
cd "$(dirname "$0")/.."
pyftsubset src/MovasseghiShop.Web/wwwroot/fonts/Vazirmatn-var.woff2 \
  --output-file=src/MovasseghiShop.Web/wwwroot/fonts/Vazirmatn-storefront.woff2 \
  --flavor=woff2 \
  --unicodes='U+0000-00FF,U+0600-06FF,U+0750-077F,U+08A0-08FF,U+2000-206F,U+20A0-20CF,U+2212,U+FB50-FDFF,U+FE70-FEFF' \
  --layout-features='*' --name-IDs='*' --name-languages='*' --name-legacy
chmod 644 src/MovasseghiShop.Web/wwwroot/fonts/Vazirmatn-storefront.woff2
python - <<'PY'
from pathlib import Path
from fontTools.ttLib import TTFont
import re
root = Path('src/MovasseghiShop.Web/wwwroot')
original = set(TTFont(root/'fonts/Vazirmatn-var.woff2').getBestCmap())
common = set(TTFont(root/'fonts/Vazirmatn-storefront.woff2').getBestCmap())
remaining = sorted(original-common)
ranges = []
for point in remaining:
    if ranges and point == ranges[-1][1]+1: ranges[-1][1] = point
    else: ranges.append([point,point])
coverage = ','.join(f'U+{a:X}-{b:X}' if a != b else f'U+{a:X}' for a,b in ranges)
css = root/'css/site.css'
text = css.read_text()
text = re.sub(r'\n  /\* Full-font fallback coverage \*/\n  unicode-range: [^;]+;', '', text)
text = text.replace('  font-display: swap;', '  font-display: swap;\n  /* Full-font fallback coverage */\n  unicode-range: '+coverage+';', 1)
css.write_text(text)
PY

"""Freeze one weight of a variable font into a static font file (dev tool, docs/DIRECTION_ARTISTIQUE.md section 6.2).

TextMeshPro renders a variable font at its default instance only; some families (Big Shoulders Stencil) ship as a variable
font whose default is the thinnest weight. This writes the weight the game uses as a plain static font.

Usage (fontTools, pinned by the developer's Python environment; nothing of it reaches a build):

    python tools/fonts/instance_font.py <variable.ttf> <wght> <output.ttf>

The OFL allows modified versions; a family with a Reserved Font Name must be renamed first (check its OFL.txt).
"""

import sys

from fontTools.ttLib import TTFont
from fontTools.varLib import instancer


def main():
    if len(sys.argv) != 4:
        sys.exit("usage: instance_font.py <variable.ttf> <wght> <output.ttf>")
    source, weight, output = sys.argv[1], float(sys.argv[2]), sys.argv[3]
    font = TTFont(source)
    static = instancer.instantiateVariableFont(font, {"wght": weight}, updateFontNames=True)
    static.save(output)
    print("Wrote", output)


if __name__ == "__main__":
    main()

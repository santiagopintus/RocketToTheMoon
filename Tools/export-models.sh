#!/bin/bash
# Exports every .blend in ModelSources/ to an .fbx in Assets/Models/ (Unity cannot read .blend on the CI server).
# Run it from anywhere after editing a model in Blender, then open Unity.

BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

for blend in "$ROOT"/ModelSources/*.blend; do
    name="$(basename "$blend" .blend)"
    echo "Exporting $name..."
    "$BLENDER" -b "$blend" --python "$ROOT/Tools/blender_export.py" -- "$ROOT/Assets/Models/$name.fbx" || exit 1
done
echo "Done."

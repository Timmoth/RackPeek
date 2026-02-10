#!/bin/bash


RESOLUTION="1366,768"  # width,height
OUTPUT_DIR="./webui_screenshots"
GIF_OUTPUT="webui_screenshots/output.gif"
DELAY=200  # delay between frames in GIF (ms)


# -----------------------------
# Convert to GIF using ImageMagick
# -----------------------------
echo "Creating GIF..."
convert -delay $DELAY -loop 0 "$OUTPUT_DIR"/*.png "$GIF_OUTPUT"

echo "Done! GIF saved to $GIF_OUTPUT"

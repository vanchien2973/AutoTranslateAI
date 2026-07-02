#!/usr/bin/env bash
# Verifies the media CLI tools the Worker pipeline depends on are installed and runnable.
# Used both at image build time (fail fast) and via `docker compose run --rm --entrypoint verify-tools worker`.
set -euo pipefail

echo "Verifying media tools on PATH..."

missing=0
for tool in ffmpeg yt-dlp demucs python3; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        echo "  MISSING: $tool" >&2
        missing=1
    fi
done

if [ "$missing" -ne 0 ]; then
    echo "One or more media tools are missing." >&2
    exit 1
fi

echo "  ffmpeg : $(ffmpeg -version | head -n 1)"
echo "  yt-dlp : $(yt-dlp --version)"
echo "  demucs : $(python3 -c 'import demucs; print(demucs.__version__)')"
echo "All media tools OK."

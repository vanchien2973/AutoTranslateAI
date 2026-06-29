#!/usr/bin/env bash
# Verifies the media CLI tools the Worker pipeline depends on are installed and runnable.
# Run standalone:  docker compose run --rm --entrypoint verify-tools worker
# Also runs automatically during the worker image build (see Dockerfile.worker).
set -euo pipefail

fail=0
check() {
  local name="$1"; shift
  if out="$("$@" 2>&1)"; then
    printf '  [OK]  %-8s %s\n' "$name" "$(printf '%s' "$out" | head -n1)"
  else
    printf '  [FAIL] %-8s (command: %s)\n' "$name" "$*"
    fail=1
  fi
}

echo "Verifying Worker CLI tools..."
check ffmpeg  ffmpeg -version
check ffprobe ffprobe -version
check yt-dlp  yt-dlp --version
check python  python3 --version
check torch   python3 -c "import torch; print('torch', torch.__version__, '(cuda:', torch.cuda.is_available(), ')')"
check demucs  python3 -c "import demucs; print('demucs', demucs.__version__)"
check demucs-cli demucs --help

if [ "$fail" -ne 0 ]; then
  echo "One or more tools are missing." >&2
  exit 1
fi
echo "All Worker CLI tools verified."

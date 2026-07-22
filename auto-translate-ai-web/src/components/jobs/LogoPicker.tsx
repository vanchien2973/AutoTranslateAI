"use client";
import { ImageUp, Loader2, X } from "lucide-react";
import { useEffect, useRef, useState, type ChangeEvent } from "react";
import { Button } from "@/components/ui/Button";
import { Field } from "@/components/ui/Field";
import { useUploadLogo } from "@/hooks/useUploadLogo";
import { validateLogoFile } from "@/lib/api/media";
import { cn } from "@/lib/utils";
import { LogoPosition, type LogoPositionValue } from "@/types/job";

export interface LogoSelection {
  storageKey: string;
  position: LogoPositionValue;
  scalePercent: number;
  margin: number;
}

const CORNERS: { value: LogoPositionValue; label: string; className: string }[] = [
  { value: LogoPosition.TopLeft, label: "Top left", className: "top-1 left-1" },
  { value: LogoPosition.TopRight, label: "Top right", className: "top-1 right-1" },
  { value: LogoPosition.BottomLeft, label: "Bottom left", className: "bottom-1 left-1" },
  { value: LogoPosition.BottomRight, label: "Bottom right", className: "right-1 bottom-1" },
];

export function LogoPicker({
  value,
  onChange,
}: {
  value: LogoSelection | null;
  onChange: (value: LogoSelection | null) => void;
}) {
  const upload = useUploadLogo();
  const [preview, setPreview] = useState<string | null>(null);
  const [localError, setLocalError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(
    () => () => {
      if (preview) URL.revokeObjectURL(preview);
    },
    [preview],
  );

  function onFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;

    const problem = validateLogoFile(file);
    if (problem) {
      setLocalError(problem);
      return;
    }
    setLocalError(null);

    setPreview((current) => {
      if (current) URL.revokeObjectURL(current);
      return URL.createObjectURL(file);
    });

    upload.mutate(file, {
      onSuccess: ({ storageKey }) =>
        onChange({
          storageKey,
          position: value?.position ?? LogoPosition.BottomRight,
          scalePercent: value?.scalePercent ?? 0.1,
          margin: value?.margin ?? 16,
        }),
    });
  }

  function clear() {
    setPreview((current) => {
      if (current) URL.revokeObjectURL(current);
      return null;
    });
    setLocalError(null);
    upload.reset();
    onChange(null);
    if (inputRef.current) inputRef.current.value = "";
  }

  const error = localError ?? upload.error?.message ?? null;

  return (
    <Field
      label="Logo / watermark"
      error={error ?? undefined}
      hint="Optional. PNG, JPEG, or WebP up to 2 MB, burned into the rendered video."
    >
      <input
        ref={inputRef}
        type="file"
        accept="image/png,image/jpeg,image/webp"
        onChange={onFile}
        className="sr-only"
        id="logo-file"
      />

      {!value && !upload.isPending ? (
        <label
          htmlFor="logo-file"
          className="border-hairline bg-console text-muted hover:border-cyan/40 hover:text-fg flex cursor-pointer items-center justify-center gap-2 rounded-md border border-dashed px-3 py-4 text-sm transition-colors"
        >
          <ImageUp aria-hidden className="size-4" />
          Choose an image
        </label>
      ) : (
        <div className="border-hairline bg-console space-y-3 rounded-md border p-3">
          <div className="flex items-start gap-3">
            {/* 16:9 stand-in for the video frame, so the corner choice reads at a glance. */}
            <div className="border-hairline bg-panel relative h-20 w-36 shrink-0 overflow-hidden rounded border">
              {preview && value && (
                // next/image cannot optimize a local blob: URL, and this preview never leaves the page.
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={preview}
                  alt=""
                  className={cn(
                    "absolute object-contain",
                    CORNERS.find((corner) => corner.value === value.position)?.className,
                  )}
                  style={{ height: `${value.scalePercent * 100}%` }}
                />
              )}
              {upload.isPending && (
                <span className="text-muted absolute inset-0 grid place-items-center">
                  <Loader2 aria-hidden className="size-4 animate-spin" />
                </span>
              )}
            </div>

            <Button type="button" size="sm" variant="ghost" onClick={clear}>
              <X aria-hidden />
              Remove
            </Button>
          </div>

          {value && (
            <>
              <div>
                <span className="text-muted mb-1.5 block text-xs tracking-wide uppercase">
                  Position
                </span>
                <div className="grid grid-cols-2 gap-1">
                  {CORNERS.map((corner) => (
                    <button
                      key={corner.value}
                      type="button"
                      onClick={() => onChange({ ...value, position: corner.value })}
                      className={cn(
                        "rounded px-2 py-1.5 text-xs transition-colors",
                        value.position === corner.value
                          ? "bg-cyan/15 text-cyan"
                          : "text-muted hover:text-fg",
                      )}
                    >
                      {corner.label}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label
                  htmlFor="logo-scale"
                  className="text-muted mb-1.5 flex items-center justify-between text-xs tracking-wide uppercase"
                >
                  Size
                  <span className="timecode text-fg">
                    {Math.round(value.scalePercent * 100)}% of height
                  </span>
                </label>
                <input
                  id="logo-scale"
                  type="range"
                  min={1}
                  max={50}
                  value={Math.round(value.scalePercent * 100)}
                  onChange={(event) =>
                    onChange({ ...value, scalePercent: Number(event.target.value) / 100 })
                  }
                  className="accent-cyan w-full"
                />
              </div>
            </>
          )}
        </div>
      )}
    </Field>
  );
}

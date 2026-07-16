import { cn } from "@/lib/utils";

const BARS = [0.45, 0.9, 0.6, 1, 0.5];

export function LevelMeter({ active = false }: { active?: boolean }) {
  return (
    <span aria-hidden className="flex h-5 items-end gap-[2px]">
      {BARS.map((scale, index) => (
        <span
          key={index}
          style={{
            height: `${scale * 100}%`,
            animationDelay: `${index * 110}ms`,
          }}
          className={cn("w-[2px] rounded-full", active ? "tally-bar bg-amber" : "bg-cyan/50")}
        />
      ))}
    </span>
  );
}

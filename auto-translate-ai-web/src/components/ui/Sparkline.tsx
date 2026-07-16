const WIDTH = 64;
const HEIGHT = 20;

export function Sparkline({ values, color }: { values: number[]; color: string }) {
  if (values.length < 2) {
    return <svg width={WIDTH} height={HEIGHT} aria-hidden />;
  }

  const step = WIDTH / (values.length - 1);
  const points = values
    .map((value, index) => {
      const x = index * step;
      const y = HEIGHT - (Math.min(100, Math.max(0, value)) / 100) * HEIGHT;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");

  return (
    <svg width={WIDTH} height={HEIGHT} viewBox={`0 0 ${WIDTH} ${HEIGHT}`} aria-hidden>
      <polyline
        points={points}
        fill="none"
        stroke={color}
        strokeWidth={1.5}
        strokeLinejoin="round"
        strokeLinecap="round"
      />
    </svg>
  );
}

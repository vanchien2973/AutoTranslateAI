"use client";

import { Canvas, useFrame } from "@react-three/fiber";
import { useMemo, useRef } from "react";
import * as THREE from "three";

interface BandProps {
  y: number;
  color: string;
  seed: number;
  speed: number;
  amplitude: number;
  animate: boolean;
}

const POINTS = 96;
const WIDTH = 9;

function Band({ y, color, seed, speed, amplitude, animate }: BandProps) {
  const ref = useRef<THREE.Points>(null);

  const positions = useMemo(() => {
    const array = new Float32Array(POINTS * 3);
    for (let i = 0; i < POINTS; i++) {
      array[i * 3] = (i / (POINTS - 1) - 0.5) * WIDTH;
      array[i * 3 + 1] = y;
      array[i * 3 + 2] = 0;
    }
    return array;
  }, [y]);

  useFrame((state) => {
    if (!animate || !ref.current) return;
    const t = state.clock.elapsedTime * speed;
    const attr = ref.current.geometry.getAttribute("position") as THREE.BufferAttribute;
    for (let i = 0; i < POINTS; i++) {
      const x = i / (POINTS - 1);
      const wave = Math.sin(x * 12 + t + seed) * 0.5 + Math.sin(x * 27 - t * 1.3 + seed) * 0.5;
      attr.setY(i, y + wave * amplitude);
    }
    attr.needsUpdate = true;
  });

  return (
    <points ref={ref}>
      <bufferGeometry>
        <bufferAttribute attach="attributes-position" args={[positions, 3]} />
      </bufferGeometry>
      <pointsMaterial color={color} size={0.075} sizeAttenuation transparent opacity={0.9} />
    </points>
  );
}

export interface ProcessingVisualizerProps {
  percent: number;
  animate: boolean;
}

export default function ProcessingVisualizer({ percent, animate }: ProcessingVisualizerProps) {
  const energy = 0.35 + (Math.min(100, Math.max(0, percent)) / 100) * 0.55;

  return (
    <Canvas camera={{ position: [0, 0, 6], fov: 45 }} style={{ height: 220 }} dpr={[1, 2]}>
      <Band y={0.9} color="#4fd1c5" seed={0} speed={1.1} amplitude={energy} animate={animate} />
      <Band
        y={-0.9}
        color="#e8a33d"
        seed={2.4}
        speed={0.8}
        amplitude={energy * 0.8}
        animate={animate}
      />
    </Canvas>
  );
}

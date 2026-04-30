import { skeleton } from './Skeleton.css';

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  borderRadius?: string;
}

export function Skeleton({ width = '100%', height = 20, borderRadius }: SkeletonProps) {
  return (
    <div
      className={skeleton}
      style={{ width, height, borderRadius }}
      aria-hidden="true"
    />
  );
}

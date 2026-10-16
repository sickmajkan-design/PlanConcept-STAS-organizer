import { useEffect, useRef, useState } from 'react';

const hasFiles = (event: DragEvent) => Array.from(event.dataTransfer?.types ?? []).includes('Files');

/**
 * Makes the whole window a place files can be dropped on: true while a file is being dragged anywhere over
 * the page, and the files are handed over when they land. A drop that something inside the page already
 * took (a zone with its own handler) is left to it. Like the zone, it ignores drags that are not files.
 */
export function usePageDrop({
  onFiles,
  disabled = false,
}: {
  onFiles: (files: File[]) => void;
  disabled?: boolean;
}) {
  const [isOver, setIsOver] = useState(false);
  const handler = useRef(onFiles);

  handler.current = onFiles;

  useEffect(() => {
    if (disabled) {
      setIsOver(false);
      return;
    }

    let depth = 0;

    const enter = (event: DragEvent) => {
      if (!hasFiles(event)) return;

      depth += 1;
      setIsOver(true);
    };

    const over = (event: DragEvent) => {
      if (!hasFiles(event)) return;

      // Without this the browser refuses the drop and opens the file in the tab instead.
      event.preventDefault();
    };

    const leave = (event: DragEvent) => {
      if (!hasFiles(event)) return;

      depth = Math.max(0, depth - 1);

      if (depth === 0) setIsOver(false);
    };

    const drop = (event: DragEvent) => {
      if (!hasFiles(event)) return;

      depth = 0;
      setIsOver(false);

      // Something inside the page took it already.
      if (event.defaultPrevented) return;

      event.preventDefault();

      const files = Array.from(event.dataTransfer?.files ?? []);

      if (files.length > 0) handler.current(files);
    };

    window.addEventListener('dragenter', enter);
    window.addEventListener('dragover', over);
    window.addEventListener('dragleave', leave);
    window.addEventListener('drop', drop);

    return () => {
      window.removeEventListener('dragenter', enter);
      window.removeEventListener('dragover', over);
      window.removeEventListener('dragleave', leave);
      window.removeEventListener('drop', drop);
    };
  }, [disabled]);

  return { isOver };
}

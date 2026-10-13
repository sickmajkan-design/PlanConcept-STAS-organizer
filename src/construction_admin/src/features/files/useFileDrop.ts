import { useCallback, useRef, useState, type DragEvent } from 'react';

/** True when what is being dragged is (or includes) files, not text or a row of a list. */
export function carriesFiles(event: DragEvent): boolean {
  return Array.from(event.dataTransfer?.types ?? []).includes('Files');
}

/**
 * Turns an element into a place files can be dropped on.
 *
 * `dragenter` and `dragleave` fire again for every child the pointer crosses, so a plain flag flickers;
 * a counter of enters minus leaves says whether the pointer is still inside. Anything that is not a file
 * (a dragged table row, selected text) is ignored, so the element never claims a drag that is not for it.
 */
export function useFileDrop({
  onFiles,
  disabled = false,
}: {
  onFiles: (files: File[]) => void;
  disabled?: boolean;
}) {
  const [isOver, setIsOver] = useState(false);
  const depth = useRef(0);

  const reset = useCallback(() => {
    depth.current = 0;
    setIsOver(false);
  }, []);

  const bind = {
    onDragEnter: (event: DragEvent) => {
      if (disabled || !carriesFiles(event)) return;

      event.preventDefault();
      depth.current += 1;
      setIsOver(true);
    },
    onDragOver: (event: DragEvent) => {
      if (disabled || !carriesFiles(event)) return;

      // Without this the browser refuses the drop and opens the file in the tab instead.
      event.preventDefault();
      event.dataTransfer.dropEffect = 'copy';
    },
    onDragLeave: (event: DragEvent) => {
      if (disabled || !carriesFiles(event)) return;

      depth.current = Math.max(0, depth.current - 1);

      if (depth.current === 0) {
        setIsOver(false);
      }
    },
    onDrop: (event: DragEvent) => {
      if (disabled || !carriesFiles(event)) return;

      event.preventDefault();
      reset();

      const files = Array.from(event.dataTransfer.files ?? []);

      if (files.length > 0) {
        onFiles(files);
      }
    },
  };

  return { isOver, bind, reset };
}

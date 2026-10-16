/**
 * @vitest-environment jsdom
 */
import { act, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { usePageDrop } from './usePageDrop';

function Probe({ onFiles, disabled }: { onFiles: (files: File[]) => void; disabled?: boolean }) {
  const { isOver } = usePageDrop({ onFiles, disabled });

  return <div data-testid="state">{isOver ? 'over' : 'idle'}</div>;
}

const dragEvent = (type: string, files: File[] = [], kinds: string[] = ['Files']) => {
  const event = new Event(type, { bubbles: true, cancelable: true }) as Event & { dataTransfer: unknown };

  event.dataTransfer = { types: kinds, files };

  return event;
};

describe('usePageDrop', () => {
  it('shows it is over while a file is dragged anywhere and hands the files over on drop', () => {
    const onFiles = vi.fn();
    const file = new File(['x'], 'a.pdf', { type: 'application/pdf' });

    render(<Probe onFiles={onFiles} />);

    act(() => void window.dispatchEvent(dragEvent('dragenter', [file])));
    expect(screen.getByTestId('state')).toHaveProperty('textContent', 'over');

    act(() => void window.dispatchEvent(dragEvent('drop', [file])));
    expect(screen.getByTestId('state')).toHaveProperty('textContent', 'idle');
    expect(onFiles).toHaveBeenCalledWith([file]);
  });

  it('ignores a drag that is not carrying files', () => {
    const onFiles = vi.fn();

    render(<Probe onFiles={onFiles} />);

    act(() => void window.dispatchEvent(dragEvent('dragenter', [], ['text/plain'])));
    expect(screen.getByTestId('state')).toHaveProperty('textContent', 'idle');

    act(() => void window.dispatchEvent(dragEvent('drop', [], ['text/plain'])));
    expect(onFiles).not.toHaveBeenCalled();
  });

  it('leaves a drop that something inside the page already took', () => {
    const onFiles = vi.fn();
    const file = new File(['x'], 'a.pdf');

    render(<Probe onFiles={onFiles} />);

    const event = dragEvent('drop', [file]);

    event.preventDefault();
    act(() => void window.dispatchEvent(event));
    expect(onFiles).not.toHaveBeenCalled();
  });

  it('does nothing while disabled', () => {
    const onFiles = vi.fn();

    render(<Probe onFiles={onFiles} disabled />);

    act(() => void window.dispatchEvent(dragEvent('dragenter', [new File(['x'], 'a.pdf')])));
    expect(screen.getByTestId('state')).toHaveProperty('textContent', 'idle');
  });
});

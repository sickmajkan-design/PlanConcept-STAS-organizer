import { createContext, useContext } from 'react';

/**
 * How tall a widget's card is, as decided by whoever places it.
 *
 * On the free-form desktop board a card is either a fixed box of the grid's own height (`fit: false`: the person
 * resized it, or it is a chart that fills what it is given), or as tall as its content (`fit: true`), in which
 * case the card measures that content and reports it, and the grid gives it the rows it needs. On a phone the
 * widgets are a plain stack and there is no provider at all: every card is as tall as its content, and nobody
 * is told about it.
 */
export interface WidgetHeightControl {
  fit: boolean;
  /** The content's natural height in pixels, reported whenever it changes. Only asked of a fitted card. */
  report?: (pixels: number) => void;
  /** The tallest a fitted card may grow, in pixels; the body scrolls beyond it. */
  maxPixels?: number;
}

export const WidgetHeightContext = createContext<WidgetHeightControl | null>(null);

export function useWidgetHeight(): WidgetHeightControl {
  return useContext(WidgetHeightContext) ?? { fit: true };
}

/**
 * The help topic that explains the widget being drawn: what it is for and exactly how each of its numbers is counted.
 * Set by whoever places the widget (the board), read by its card, which offers a link to it.
 */
export const WidgetHelpContext = createContext<string | null>(null);

export function useWidgetHelp(): string | null {
  return useContext(WidgetHelpContext);
}

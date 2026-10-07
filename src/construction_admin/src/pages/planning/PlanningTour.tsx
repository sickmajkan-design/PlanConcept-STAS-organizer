import { Box, Button, Paper, Stack, Typography } from '@mui/material';
import { useCallback, useEffect, useLayoutEffect, useState } from 'react';

import type { MessageKey } from '../../i18n/en';
import { useT } from '../../i18n/useI18n';

export type TourView = 'day' | 'timeline' | 'need' | 'list';

interface TourStep {
  view: TourView;
  /** CSS selector of the part of the page to light up. Without one the card sits in the middle. */
  target?: string;
  key: string;
}

export const TOUR_STEPS: TourStep[] = [
  { view: 'timeline', key: 'welcome' },
  { view: 'timeline', target: '[data-tour="tabs"]', key: 'tabs' },
  { view: 'timeline', target: '[data-tour="controls"]', key: 'controls' },
  { view: 'timeline', target: '[data-tour="summary"]', key: 'summary' },
  { view: 'timeline', target: '[data-tour="timeline"]', key: 'timeline' },
  { view: 'timeline', target: '[data-tour="timeline"] tbody tr:first-child button', key: 'bar' },
  { view: 'need', target: '[data-tour="need-table"]', key: 'need' },
  { view: 'need', target: '[data-tour="need-table"] tbody tr:first-child td:first-child button:last-of-type', key: 'editNeeds' },
  { view: 'need', target: '[data-tour="skills"]', key: 'skills' },
  { view: 'need', target: '[data-tour="banner"]', key: 'cover' },
  { view: 'day', target: '[data-tour="day-sites"]', key: 'day' },
  { view: 'list', target: '[data-tour="list"]', key: 'list' },
  { view: 'timeline', key: 'done' },
];

const CARD_WIDTH = 400;
const CARD_HEIGHT = 260;

/**
 * A guided walk through the schedule for people seeing it for the first time. It lights up the real
 * parts of the page one at a time, switching views as it goes, so what is described is what is on screen.
 */
export function PlanningTour({
  open,
  onClose,
  setView,
}: {
  open: boolean;
  onClose: () => void;
  setView: (view: TourView) => void;
}) {
  const t = useT();
  const [index, setIndex] = useState(0);
  const [rect, setRect] = useState<DOMRect | null>(null);

  const step = TOUR_STEPS[index];
  const last = index === TOUR_STEPS.length - 1;

  useEffect(() => {
    if (open) setIndex(0);
  }, [open]);

  useEffect(() => {
    if (open) setView(step.view);
  }, [open, step.view, setView]);

  // Finds the target once its view has drawn, then follows it while the page scrolls or resizes.
  useLayoutEffect(() => {
    if (!open) return undefined;

    setRect(null);
    if (!step.target) return undefined;

    let element: Element | null = null;
    let timer: ReturnType<typeof setTimeout> | undefined;
    let attempts = 0;

    const update = () => {
      if (element) setRect(element.getBoundingClientRect());
    };

    const find = () => {
      element = document.querySelector(step.target!);

      if (element) {
        element.scrollIntoView?.({ block: 'center', inline: 'nearest' });
        update();
      } else if (attempts++ < 25) {
        timer = setTimeout(find, 100);
      }
    };

    find();
    window.addEventListener('scroll', update, true);
    window.addEventListener('resize', update);

    return () => {
      if (timer) clearTimeout(timer);
      window.removeEventListener('scroll', update, true);
      window.removeEventListener('resize', update);
    };
  }, [open, step.target, index]);

  const next = useCallback(() => (last ? onClose() : setIndex((i) => i + 1)), [last, onClose]);
  const back = useCallback(() => setIndex((i) => Math.max(0, i - 1)), []);

  useEffect(() => {
    if (!open) return undefined;

    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
      if (event.key === 'ArrowRight') next();
      if (event.key === 'ArrowLeft') back();
    };

    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, next, back, onClose]);

  if (!open) return null;

  const width = Math.min(CARD_WIDTH, window.innerWidth - 24);
  let card: { top?: number; bottom?: number; left: number } = {
    top: Math.max(12, window.innerHeight / 2 - CARD_HEIGHT / 2),
    left: window.innerWidth / 2 - width / 2,
  };

  if (rect) {
    const left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12));

    if (rect.bottom + 14 + CARD_HEIGHT < window.innerHeight) {
      card = { top: rect.bottom + 14, left };
    } else if (rect.top - 14 - CARD_HEIGHT > 0) {
      card = { top: rect.top - 14 - CARD_HEIGHT, left };
    } else {
      card = { bottom: 16, left };
    }
  }

  return (
    <>
      {/* Swallows clicks, so a step cannot be knocked off course by touching the page behind it. */}
      <Box sx={{ position: 'fixed', inset: 0, zIndex: 1500, bgcolor: rect ? 'transparent' : 'rgba(0,0,0,.5)' }} />

      {rect && (
        <Box
          sx={{
            position: 'fixed',
            top: rect.top - 6,
            left: rect.left - 6,
            width: rect.width + 12,
            height: rect.height + 12,
            borderRadius: 2,
            border: 2,
            borderColor: 'primary.main',
            boxShadow: '0 0 0 9999px rgba(0,0,0,.5)',
            pointerEvents: 'none',
            zIndex: 1501,
          }}
        />
      )}

      <Paper
        role="dialog"
        aria-modal="true"
        aria-label={t('planning.tour.button')}
        elevation={8}
        sx={{ position: 'fixed', zIndex: 1600, width, p: 2.5, borderRadius: 3, ...card }}
      >
        <Typography variant="caption" color="text.secondary">
          {t('planning.tour.step', { n: index + 1, total: TOUR_STEPS.length })}
        </Typography>
        <Typography variant="h6" sx={{ mt: 0.25, mb: 1 }}>
          {t(`planning.tour.${step.key}.title` as MessageKey)}
        </Typography>
        <Typography variant="body2" sx={{ mb: 2, whiteSpace: 'pre-line' }}>
          {t(`planning.tour.${step.key}.body` as MessageKey)}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <Button size="small" color="inherit" onClick={onClose}>
            {last ? t('planning.close') : t('planning.tour.skip')}
          </Button>
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" onClick={back} disabled={index === 0}>
              {t('planning.tour.back')}
            </Button>
            <Button size="small" variant="contained" onClick={next} autoFocus>
              {last ? t('planning.tour.finish') : t('planning.tour.next')}
            </Button>
          </Stack>
        </Stack>
      </Paper>
    </>
  );
}

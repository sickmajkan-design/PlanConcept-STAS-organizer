/** @vitest-environment jsdom */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { beforeEach, describe, expect, it } from 'vitest';

import { I18nProvider } from '../i18n/I18nProvider';
import { ReminderDaysField } from './ReminderDaysField';

function Harness({ initial = [] as number[] }) {
  const [value, setValue] = useState(initial);

  return (
    <I18nProvider>
      <ReminderDaysField value={value} onChange={setValue} />
      <output data-testid="value">{value.join(',')}</output>
    </I18nProvider>
  );
}

beforeEach(() => {
  window.localStorage.setItem('construction.locale', 'en');
});

describe('ReminderDaysField', () => {
  it('adds lead times longest first and drops duplicates', async () => {
    render(<Harness />);

    await userEvent.click(screen.getByText('+ 30'));
    await userEvent.click(screen.getByText('+ 90'));

    expect(screen.getByTestId('value').textContent).toBe('90,30');
    expect(screen.queryByText('+ 90')).toBeNull();
  });

  it('takes a custom number of days', async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText('Other (days)'), '45{Enter}');

    expect(screen.getByTestId('value').textContent).toBe('45');
  });

  it('refuses a lead time the API would refuse', async () => {
    render(<Harness />);

    await userEvent.type(screen.getByLabelText('Other (days)'), '400{Enter}');

    expect(screen.getByTestId('value').textContent).toBe('');
  });

  it('stops at five reminders', async () => {
    render(<Harness initial={[90, 60, 30, 14, 7]} />);

    expect(screen.getByLabelText('Other (days)')).toHaveProperty('disabled', true);
  });

  it('removes one', async () => {
    render(<Harness initial={[90, 30]} />);

    await userEvent.click(screen.getAllByTestId('CancelIcon')[0]!);

    expect(screen.getByTestId('value').textContent).toBe('30');
  });
});

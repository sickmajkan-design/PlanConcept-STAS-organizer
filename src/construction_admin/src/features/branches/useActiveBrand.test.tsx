/**
 * @vitest-environment jsdom
 */
import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { storageScope, writeScoped } from '../../hooks/userScopedStorage';
import { installFakeNetwork, renderScreen, signedIn, type FakeNetwork } from '../../test/renderScreen';

/**
 * The rail and the top bar speak for the unit that is chosen; with all units chosen, for the main one, and for the
 * company while no main unit has a logo.
 */
let network: FakeNetwork;

const SCREEN_TIMEOUT = 60_000;

beforeEach(() => {
  window.localStorage.clear();
  network = installFakeNetwork();
});

function unit(id: string, name: string, extra: Record<string, unknown> = {}) {
  return {
    id,
    name,
    color: '#3457D5',
    isActive: true,
    kind: 'LegalEntity',
    projectCount: 0,
    employeeCount: 0,
    parentBranchId: null,
    headEmployeeId: null,
    headEmployeeName: null,
    hasLogo: false,
    hasDarkLogo: false,
    logoVersion: null,
    isMain: false,
    address: 'Zmaja od Bosne 12',
    city: 'Sarajevo',
    ...extra,
  };
}

async function renderBrand(branches: unknown[], chosen?: string) {
  const user = signedIn('Admin');

  if (chosen) writeScoped(storageScope(user), 'branchFilter', chosen);

  network.reply('/branches', 200, branches);
  network.reply('/company-settings/branding', 200, { name: 'Darijo d.o.o.', hasLogo: true, timeZone: 'Europe/Sarajevo' });
  network.reply('/company-settings', 200, { address: 'Firmina 1' });

  const { BranchFilterProvider } = await import('./BranchContext');
  const { useActiveBrand, useBrandPicture } = await import('./useActiveBrand');

  function Probe() {
    const brand = useActiveBrand();
    const picture = useBrandPicture(brand);

    return (
      <div>
        <span data-testid="name">{brand.name}</span>
        <span data-testid="address">{brand.address ?? '-'}</span>
        <span data-testid="kind">{brand.kind}</span>
        <span data-testid="all">{String(brand.allUnits)}</span>
        <span data-testid="missing">{String(brand.missingLogo)}</span>
        <span data-testid="src">{picture?.src ?? '-'}</span>
      </div>
    );
  }

  renderScreen(
    <BranchFilterProvider>
      <Probe />
    </BranchFilterProvider>,
    { user },
  );
}

describe('useActiveBrand', () => {
  it('speaks for the chosen unit, with its address and its own logo', async () => {
    await renderBrand([unit('b1', 'Bio-Teo Sarajevo', { hasLogo: true, logoVersion: 7 }), unit('b2', 'Banja Luka')], 'b1');

    await screen.findByText('Bio-Teo Sarajevo');
    expect(screen.getByTestId('address').textContent).toBe('Zmaja od Bosne 12, Sarajevo');
    expect(screen.getByTestId('kind').textContent).toBe('unit');
    expect(screen.getByTestId('src').textContent).toMatch(/\/branches\/b1\/logo\?v=7$/);
  }, SCREEN_TIMEOUT);

  it('says the logo is missing for a chosen unit without one, instead of showing something generic', async () => {
    await renderBrand([unit('b1', 'Pogon Mostar')], 'b1');

    await screen.findByText('Pogon Mostar');
    expect(screen.getByTestId('missing').textContent).toBe('true');
    expect(screen.getByTestId('src').textContent).toBe('-');
  }, SCREEN_TIMEOUT);

  it('shows the main unit while all units are chosen', async () => {
    await renderBrand([unit('b1', 'Bio-Teo Sarajevo', { hasLogo: true, isMain: true }), unit('b2', 'Banja Luka', { hasLogo: true })]);

    await screen.findByText('Bio-Teo Sarajevo');
    expect(screen.getByTestId('all').textContent).toBe('true');
    expect(screen.getByTestId('src').textContent).toMatch(/\/branches\/b1\/logo/);
  }, SCREEN_TIMEOUT);

  it('falls back to the company while no main unit with a logo is set', async () => {
    await renderBrand([unit('b1', 'Bio-Teo Sarajevo', { isMain: true }), unit('b2', 'Banja Luka', { hasLogo: true })]);

    await screen.findByText('Darijo d.o.o.');
    expect(screen.getByTestId('kind').textContent).toBe('company');
    expect(screen.getByTestId('src').textContent).toMatch(/company-settings\/logo/);
  }, SCREEN_TIMEOUT);
});

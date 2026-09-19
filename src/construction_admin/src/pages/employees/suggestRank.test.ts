import { describe, expect, it } from 'vitest';

import { suggestRank } from './suggestRank';

describe('suggestRank', () => {
  it.each([
    ['Vlasnik', 'Owner'],
    ['Izvršni direktor', 'ExecutiveDirector'],
    ['Direktor', 'Director'],
    ['Zamenik direktora', 'DeputyDirector'],
    ['Zamjenik direktora', 'DeputyDirector'],
    ['Rukovodilac finansija', 'FinanceManager'],
    ['Referent nabavke', 'ProcurementManager'],
    ['Rukovodilac logistike', 'LogisticsManager'],
    ['Rukovodilac kadrovske', 'HrManager'],
    ['Voditelj projekta', 'ProjectManager'],
    ['Project Manager', 'ProjectManager'],
    ['Poslovođa', 'Foreman'],
    ['poslovodja', 'Foreman'],
    ['Predradnik', 'Foreman'],
  ] as const)('reads %s as %s', (position, rank) => {
    expect(suggestRank(position)).toBe(rank);
  });

  it('reads a diacritic title the same as its plain spelling', () => {
    expect(suggestRank('POSLOVOĐA')).toBe(suggestRank('poslovodja'));
  });

  it('prefers the specific title over the general one it contains', () => {
    // Both contain "direktor"; neither is just a Director.
    expect(suggestRank('Izvršni direktor')).not.toBe('Director');
    expect(suggestRank('Zamenik direktora')).not.toBe('Director');
  });

  it('suggests nothing for a trade, so that person is left where they are', () => {
    for (const trade of ['Zidar', 'Električar', 'Armirač', 'Vozač', 'Magacioner', '']) {
      expect(suggestRank(trade)).toBeNull();
    }
  });
});

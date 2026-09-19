import type { OrganizationRank } from '../../api/types';

/**
 * A rank guessed from the free-text job title, for somebody who has not been
 * placed by hand.
 *
 * Only a suggestion, and only ever offered for confirmation — a title is typed
 * by whoever entered the employee, so "Direktor" might mean the director of the
 * company or of a two-person department. Nothing here writes a rank.
 *
 * Deliberately narrow. A title that names a management role maps to it; a
 * trade ("Zidar", "Električar") maps to nothing, and that person stays where
 * an unranked worker already sits. Guessing "Worker" for them would be noise.
 *
 * Order matters: the specific phrases go first, because "izvršni direktor" and
 * "zamenik direktora" both contain "direktor".
 */
const RULES: readonly [RegExp, OrganizationRank][] = [
  [/\b(vlasnik|owner)\b/, 'Owner'],
  [/izvrsn\w*\s+direktor|executive\s+director|\bceo\b/, 'ExecutiveDirector'],
  [/(zamenik|zamjenik|deputy)\s+direktor|deputy\s+director/, 'DeputyDirector'],
  [/direktor|director/, 'Director'],
  [/finansij|finance\s+(manager|director|head)/, 'FinanceManager'],
  [/nabavk|procurement|purchasing/, 'ProcurementManager'],
  [/logistik|logistic/, 'LogisticsManager'],
  [/kadrov|human\s+resources|\bhr\b/, 'HrManager'],
  [/(voditelj|rukovodilac|menadzer)\s+projekt|project\s+manager/, 'ProjectManager'],
  [/poslovodj|poslovod|foreman|predradnik|site\s+manager|sef\s+gradilist/, 'Foreman'],
];

/** Lower-case, without diacritics, so "Poslovođa" and "poslovodja" are one title. */
function normalise(text: string): string {
  return text
    .toLowerCase()
    .replace(/đ/g, 'dj')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '');
}

export function suggestRank(position: string): OrganizationRank | null {
  const title = normalise(position);

  for (const [pattern, rank] of RULES) {
    if (pattern.test(title)) return rank;
  }

  return null;
}

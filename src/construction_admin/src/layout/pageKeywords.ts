import { paths } from '../routes/paths';

/**
 * Other words people type when they look for a page, so Ctrl+K finds it by what it does and not only by what
 * the menu calls it ("upload" for the documents page, "advance" for payouts). Matched in both languages.
 */
const KEYWORDS: Record<string, string> = {
  [paths.expiringDocuments]:
    'dokumenti prilozi datoteke otpremi upload pdf viza ljekarski ugovor istek rok važi do podsjetnik oznaka tag documents files attachments expiry visa contract',
  [paths.documentRequests]:
    'zahtjev dokument traži novi radnik paket za provjeru prihvati vrati pasoš request document new worker package check accept return passport',
  [paths.audit]: 'revizija trag izmjena ko je otvorio osjetljivo audit trail history who opened sensitive',
  [paths.organization]: 'firma poslovne jedinice filijala predstavništvo organizacija company branch business units',
  [paths.schedule]: 'raspored planiranje smjena zamjena odsutni ekipa potreba schedule planning shift replacement crew',
  [paths.bookOverview]: 'knjiga evidencija novac dobit prihod trošak ulaz izlaz book money profit income cost',
  [paths.bookEntries]: 'zapisi unos trošak prihod faktura gorivo izvoz zip knjigovođa entries costs invoice export accountant',
  [paths.bookCategories]: 'kategorije pravila kategorija categories rules',
  [paths.bookReceivables]: 'potraživanja dugovanja ko duguje faktura rok kašnjenje receivables debts overdue invoice',
  [paths.bookPayouts]: 'isplate plate akontacija radnici konačna isplata payouts wages advance pay',
  [paths.bookRecurring]: 'ponavljajući troškovi kirija zakup osiguranje šablon recurring rent lease insurance template',
  [paths.bookSettlement]: 'obračun mjeseca zaključaj paušal satnica marža settlement lock month flat rate hourly margin',
  [paths.bookImport]: 'uvoz excel xlsx tabela stari mjeseci import spreadsheet',
  [paths.bookGuide]: 'kako radi uputstvo vodič evidencija how it works guide',
  [paths.help]: 'uputstva pomoć vodič kako se koristi help guide how to manual',
  [paths.users]: 'nalozi korisnici lozinka uloge mfa drugi faktor accounts users password roles',
  [paths.ledgers]: 'evidencija mjesečne tabele ledger monthly sheets',
};

export function pageKeywords(path: string): string {
  return KEYWORDS[path] ?? '';
}

import type { User } from '../../api/types';
import {
  canAdministerAccounts,
  canViewBook,
  canViewDirectory,
  canViewFinanceStatistics,
} from '../../auth/authHelpers';
import { paths } from '../../routes/paths';

export type HelpLocale = 'sr' | 'en';
export type HelpGroup = 'documents' | 'money' | 'work' | 'account';

export interface HelpContent {
  title: string;
  /** One or two sentences: what it is for. */
  summary: string;
  /** In order, each one a thing to do. */
  steps: string[];
  /** A worked case from daily practice. */
  example?: string;
  /** Things that are easy to miss. */
  tips: string[];
  /** Other words people may type when they look for it (the palette matches these too). */
  keywords: string[];
}

export interface HelpTopic {
  id: string;
  group: HelpGroup;
  /** The screen the topic is about, so the page can offer to go there. */
  path: string;
  /** Who gets the topic; a topic about a screen someone cannot open is not shown to them. */
  audience: (user: User | null | undefined) => boolean;
  content: Record<HelpLocale, HelpContent>;
}

const everyone = () => true;
const office = (user: User | null | undefined) => canAdministerAccounts(user);
const staff = (user: User | null | undefined) => canViewDirectory(user);
/** The money widgets are for whoever may see the company's money, as amounts or as percentages. */
const moneyView = (user: User | null | undefined) => canViewFinanceStatistics(user);

export const helpGroups: { id: HelpGroup; sr: string; en: string }[] = [
  { id: 'documents', sr: 'Dokumenti', en: 'Documents' },
  { id: 'money', sr: 'Novac', en: 'Money' },
  { id: 'work', sr: 'Rad i organizacija', en: 'Work and organisation' },
  { id: 'account', sr: 'Nalog i sigurnost', en: 'Account and security' },
];

export const helpTopics: HelpTopic[] = [
  {
    id: 'dokumenti-dodavanje',
    group: 'documents',
    path: paths.expiringDocuments,
    audience: staff,
    content: {
      sr: {
        title: 'Dodavanje dokumenata',
        summary:
          'Dokument se vezuje za zapis kojem pripada: zaposlenog, vozilo, alat, gradilište ili smještaj. Može se dodati na kartici zapisa ili sa stranice Dokumenti.',
        steps: [
          'Otvorite zapis (npr. zaposlenog) i na kartici Dokumenti prevucite datoteke u okvir, ili kliknite na okvir i izaberite ih. Na računaru možete zalijepiti i sliku iz međuspremnika (Ctrl+V).',
          'Pri pravljenju novog zaposlenog, vozila, alata, gradilišta ili smještaja obrazac ima odjeljak Dokumenti: dodajte ih odmah, pa se spremaju uz zapis čim se napravi. U brzom unosu Evidencije dugme Dokument vezuje račun uz red koji ukucate.',
          'Ili idite na Dokumenti (Dokumenti na lijevoj traci), kliknite Otpremi i prvo izaberite zapis. Na toj stranici datoteku možete ispustiti bilo gdje; dodavanje se otvara s datotekom već unutra.',
          'Dozvoljeno je do 25 MB po datoteci: PDF, slike (JPG, PNG, WEBP, HEIC), Word, Excel, tekst i CSV. Server provjerava da sadržaj odgovara nazivu i svaku datoteku skenira na viruse prije spremanja.',
          'Više datoteka može odjednom. Svaka ima svoj napredak; ona koja zakaže može se poslati ponovo, a ostale ostaju. Uz svaku datoteku možete izabrati njenu vlastitu vrstu, a ostale prate opštu vrstu iznad.',
          'Naslov (samo za jednu datoteku), napomena i oznake pomažu da dokument kasnije nađete. Datume (Važi do, Čuvati do, podsjetnici) otvorite i popunite samo ako vrijede.',
          'Pritisnite Otpremi. Kad sve prođe, prozor se sam zatvara.',
        ],
        example:
          'Dobili ste skenirani ugovor i skeniranu vizu za radnika Marka. Otvorite Markov karton i prevucite ugovor: vrsta Ugovor, bez datuma. Zatim prevucite vizu: izaberite vrstu Viza ili dozvola boravka, otvorite Datumi i podsjetnici, upišite Važi do 30.06.2027. i podsjetnike 90 i 30 dana. Dokumente s različitim rokovima šaljite odvojeno, jer datumi iz prozora vrijede za sve datoteke koje se šalju zajedno.',
        tips: [
          'Fotografija ne ističe, pa za nju datum isteka nije moguć.',
          'Datoteka koja ne odgovara svom nazivu (npr. program preimenovan u .pdf) se odbija.',
          'Isti dokument se ne dodaje dvaput u istom prozoru; ponovljena datoteka se preskače uz poruku.',
          'Na telefonu umjesto okvira stoje dugmad Izaberi i Slikaj.',
        ],
        keywords: ['upload', 'otpremi', 'prilog', 'datoteka', 'prevuci', 'drag', 'ispusti', 'pdf', 'skeniraj', 'slika', 'prilozi'],
      },
      en: {
        title: 'Adding documents',
        summary:
          'A document belongs to the record it is about: an employee, a vehicle, a tool, a site or an accommodation. Add it on the record\'s card or from the Documents page.',
        steps: [
          'Open the record (an employee, say) and on the Documents card drag files into the box, or click it and choose them. On a computer you can also paste a picture (Ctrl+V).',
          'When making a new employee, vehicle, tool, site or accommodation, the form has a Documents section: add them straight away and they are filed with the record as soon as it is made. In the book\'s quick entry the Document button ties a receipt to the line you type.',
          'Or go to Documents (Documents on the left rail), click Upload and choose the record first. On that page you can drop a file anywhere; the upload opens with the file already in it.',
          'Up to 25 MB per file: PDF, images (JPG, PNG, WEBP, HEIC), Word, Excel, text and CSV. The server checks that the content fits the name and scans every file for viruses before it is stored.',
          'Several files at once. Each has its own progress; one that fails can be sent again and the others stay. Beside each file you can pick its own type; the rest follow the general type above.',
          'The title (one file only), a note and labels help you find it later. Open the dates (Valid until, Keep until, reminders) only if they apply.',
          'Press Upload. When everything has gone through, the window closes on its own.',
        ],
        example:
          'You received a scanned contract and a scanned visa for the worker Marko. Open his record and drag the contract: type Contract, no dates. Then drag the visa: choose the type Visa or permit, open Dates and reminders, enter Valid until 30.06.2027 with reminders at 90 and 30 days. Send documents with different deadlines separately, because the dates in the window apply to all files sent together.',
        tips: [
          'A photograph does not expire, so it cannot have an expiry date.',
          'A file that does not match its name (a program renamed to .pdf) is refused.',
          'The same file is not added twice in one window; a repeat is skipped with a message.',
          'On a phone the box is replaced by the Choose and Take a photo buttons.',
        ],
        keywords: ['upload', 'attach', 'file', 'drag', 'drop', 'pdf', 'scan', 'photo'],
      },
    },
  },
  {
    id: 'dokumenti-trazenje',
    group: 'documents',
    path: paths.expiringDocuments,
    audience: staff,
    content: {
      sr: {
        title: 'Pronalaženje, pregled i preuzimanje dokumenata',
        summary:
          'Stranica Dokumenti pokazuje dokumente cijele firme. Počinje s onima kojima uskoro ističe rok, a pretraga i filteri nalaze sve ostalo.',
        steps: [
          'Otvorite Dokumenti na lijevoj traci. Zadano se vide dokumenti koji ističu u sljedećih 30 dana; promijenite period ili izaberite Sve.',
          'Uključite Prikaži i dokumente bez roka isteka da vidite sve, ne samo one koji ističu.',
          'Upišite riječ u pretragu: traže se naslov, ime datoteke, vlasnik, napomena i oznake. Kad upišete više riječi, sve moraju da se podudaraju.',
          'Sužavajte filterima Zapis (vrsta vlasnika), Vrsta i Oznaka.',
          'Dvoklik na red otvara pregled bez preuzimanja: slike, PDF (stranicu po stranicu u samom panelu), tabele i tekst. Strelica preuzima datoteku.',
          'Dugme ZIP preuzima tačno ono što je trenutno prikazano, razvrstano po vlasniku i vrsti.',
        ],
        example:
          'Tražite sve ljekarske preglede koji ističu do kraja godine: izaberite period Sve, vrstu Ljekarski pregled i sortirajte po Važi do. Zatim kliknite ZIP da cijeli spisak pošaljete ljekaru.',
        tips: [
          'Pretraga radi nad učitanim dokumentima, pa za potpunu pretragu izaberite Sve i uključite dokumente bez roka.',
          'Vrlo dugački PDF prikazuje prvih 40 stranica; preuzimanjem dobijate cijeli.',
          'Ctrl+K pronalazi dokument i po naslovu ili oznaci i odmah ga otvara.',
        ],
        keywords: ['pretraga', 'traži', 'pregled', 'preuzmi', 'zip', 'filter', 'oznaka', 'tag', 'nađi dokument', 'ističe', 'istek'],
      },
      en: {
        title: 'Finding, previewing and downloading documents',
        summary:
          'The Documents page shows the whole company\'s documents. It starts with those expiring soon; search and filters find the rest.',
        steps: [
          'Open Documents on the left rail. By default you see documents expiring in the next 30 days; change the period or choose All.',
          'Switch on Also show documents with no expiry to see everything, not only those that lapse.',
          'Type a word in the search box: the title, file name, owner, note and labels are searched. With several words, all must match.',
          'Narrow with the Record (owner kind), Type and Label filters.',
          'Double-click a row to preview without downloading: images, PDF (page by page inside the panel), tables and text. The arrow downloads the file.',
          'The ZIP button downloads exactly what is shown, filed by owner and type.',
        ],
        example:
          'You want every medical check expiring before the end of the year: choose period All, type Medical check and sort by Valid until. Then click ZIP to send the whole list to the doctor.',
        tips: [
          'Search works on the documents loaded, so for a full search choose All and include documents with no expiry.',
          'A very long PDF shows the first 40 pages; the download is complete.',
          'Ctrl+K finds a document by title or label and opens it straight away.',
        ],
        keywords: ['search', 'find', 'preview', 'download', 'zip', 'filter', 'label', 'tag', 'expiring'],
      },
    },
  },
  {
    id: 'dokumenti-rokovi',
    group: 'documents',
    path: paths.expiringDocuments,
    audience: staff,
    content: {
      sr: {
        title: 'Rokovi važenja i podsjetnici',
        summary:
          'Dokument može imati datum do kojeg važi, rok do kojeg se mora čuvati i vlastite podsjetnike. Tako ništa ne ističe nezapaženo.',
        steps: [
          'Pri dodavanju ili izmjeni dokumenta otvorite Datumi i upišite Važi do.',
          'Ispod toga se pojave Podsjetnici prije isteka: dodajte ponuđene rokove (90, 60, 30, 14 dana) ili upišite svoj broj dana (1 do 365). Najviše pet po dokumentu.',
          'Po potrebi upišite Čuvati do: dokument se ne može obrisati prije tog datuma (npr. zakonski rok čuvanja).',
          'Kad rok stigne, svi administratori dobiju obavijest Dokument ističe. Onaj ko je dokument dodao vidi dodatak da ga je dodao on.',
          'Kad obnovite dokument (nova viza), promijenite datum Važi do; podsjetnici kreću ispočetka za novi datum.',
        ],
        example:
          'Viza ističe 30.06.2027. Postavite podsjetnike 90 i 30 dana: 1. aprila i 31. maja administratori dobiju obavijest. Ljekarski pregled istog radnika ima samo podsjetnik od 30 dana.',
        tips: [
          'Svaki rok javlja se jednom. Ako je više rokova prošlo dok niko nije gledao, stiže jedna obavijest, ne više.',
          'Dokument bez vlastitih podsjetnika ostaje na općem pravilu (zadano 30 dana).',
          'Lista Dokumenti sortirana po Važi do uvijek pokazuje šta je najhitnije; istekli su označeni crveno.',
        ],
        keywords: ['istek', 'ističe', 'rok', 'važi do', 'čuvati do', 'podsjetnik', 'viza', 'ljekarski', 'obnova', 'obavijest'],
      },
      en: {
        title: 'Expiry dates and reminders',
        summary:
          'A document can have a valid-until date, a date until which it must be kept, and its own reminders. That way nothing lapses unnoticed.',
        steps: [
          'When adding or editing a document, open Dates and enter Valid until.',
          'Below it Reminders before expiry appear: add the offered times (90, 60, 30, 14 days) or type your own number of days (1 to 365). Five at most per document.',
          'If needed enter Keep until: the document cannot be deleted before that date (a legal retention period, say).',
          'When a time arrives, all administrators get a Document expiring notice. Whoever added the document sees a note that they added it.',
          'When you renew a document (a new visa), change Valid until; the reminders start over for the new date.',
        ],
        example:
          'A visa expires on 30.06.2027. Set reminders at 90 and 30 days: on 1 April and 31 May the administrators are notified. The same worker\'s medical check has only a 30-day reminder.',
        tips: [
          'Each time is announced once. If several passed while nobody looked, one notice arrives, not several.',
          'A document with no reminders of its own stays on the general rule (30 days by default).',
          'The Documents list sorted by Valid until always shows what is most urgent; lapsed ones are marked red.',
        ],
        keywords: ['expiry', 'expires', 'deadline', 'valid until', 'keep until', 'reminder', 'visa', 'medical', 'renew', 'notification'],
      },
    },
  },
  {
    id: 'dokumenti-dijeljenje',
    group: 'documents',
    path: paths.employees,
    audience: office,
    content: {
      sr: {
        title: 'Dijeljenje dokumenata s radnikom',
        summary:
          'Radnik u aplikaciji vidi samo ono što uprava podijeli s njim. Zadano se ništa ne dijeli.',
        steps: [
          'Otvorite zaposlenog ili vozilo i na kartici Dokumenti kliknite ikonicu oka uz dokument koji želite podijeliti.',
          'Za više dokumenata odjednom izaberite Podijeli sve; isto tako se dijeljenje može povući.',
          'Radnik podijeljeni dokument vidi u aplikaciji, na ekranu Moji dokumenti (izbornik Više). Dokumente vozila koje mu je zaduženo vidi i vozač.',
          'Da biste povukli pristup, ponovo kliknite oko. Dokument nestaje iz aplikacije osobe.',
        ],
        example:
          'Markova radna dozvola je na njegovom kartonu. Kliknite oko uz nju i Marko je odmah vidi na telefonu, pa je ne mora tražiti od kancelarije. Njegov ljekarski nalaz ostaje nepodijeljen.',
        tips: [
          'Dijeliti se mogu samo dokumenti uz zaposlenog ili vozilo.',
          'Kad uprava prihvati dokument koji je osoba poslala na zahtjev, on se sam stavlja na karton i dijeli s njom.',
          'Kad neko osim same osobe otvori ljekarski pregled, lični dokument ili vizu, to ostaje upisano u reviziji.',
        ],
        keywords: ['dijeli', 'podijeli', 'oko', 'moji dokumenti', 'radnik vidi', 'aplikacija', 'vidljivo', 'privatnost'],
      },
      en: {
        title: 'Sharing documents with a worker',
        summary:
          'In the app a worker sees only what the office shares with them. By default nothing is shared.',
        steps: [
          'Open the employee or vehicle and on the Documents card click the eye icon beside the document you want to share.',
          'For several at once choose Share all; sharing can be taken back the same way.',
          'The worker sees a shared document in the app, on the My documents screen (More menu). The driver also sees the documents of the vehicle assigned to them.',
          'To take access back, click the eye again. The document disappears from the person\'s app.',
        ],
        example:
          'Marko\'s work permit is on his record. Click the eye beside it and Marko sees it on his phone at once, so he need not ask the office. His medical report stays unshared.',
        tips: [
          'Only documents on an employee or a vehicle can be shared.',
          'When the office accepts a document the person sent on request, it is filed on the record and shared with them automatically.',
          'When anyone other than the person opens a medical check, an ID or a visa, it stays recorded in the audit.',
        ],
        keywords: ['share', 'eye', 'my documents', 'worker sees', 'app', 'visible', 'privacy'],
      },
    },
  },
  {
    id: 'zahtjevi-dokumenti',
    group: 'documents',
    path: paths.documentRequests,
    audience: office,
    content: {
      sr: {
        title: 'Zahtjevi za dokumente',
        summary:
          'Uprava traži od ljudi dokument koji nedostaje; osoba ga pošalje s telefona, a uprava provjeri i prihvati ili vrati s razlogom.',
        steps: [
          'Otvorite Dokumenti na lijevoj traci, pa Zahtjevi za dokumente, i kliknite Zatraži dokument. Izaberite jednu ili više osoba i šta se traži; paket Novi radnik traži uobičajeni set odjednom. Po želji dodajte rok.',
          'Osobe su odmah obaviještene u aplikaciji. Tamo vide šta se traži i šalju fotografije ili PDF.',
          'Ono što stigne čeka pod Za provjeru. Otvorite zahtjev (Provjeri) i pogledajte poslane datoteke.',
          'Prihvatite: upišite Vrijedi do ako dokument ističe. Dokument se stavlja na karton osobe i dijeli s njom.',
          'Ili vratite s razlogom (najmanje pet znakova, ništa o zdravlju osobe): osoba dobija obavijest i šalje ponovo. Ponuđeni su gotovi razlozi, npr. slika je mutna.',
          'Zahtjev koji više ne treba povucite; povučen zahtjev se može i trajno obrisati.',
        ],
        example:
          'Za tri nova radnika zatražite paket Novi radnik s rokom za pet dana. Za svakoga stiže pet slika. Jednoj fotografiji pasoša vratite s razlogom Slika je mutna, a ostale prihvatite. Lista Samo kojima je rok istekao pokazuje ko kasni.',
        tips: [
          'Rok: osoba je podsjećena u aplikaciji dan prije i na dan roka, a vama stiže jedna obavijest ako rok prođe i ništa nije poslano. Zahtjev koji čeka na vašu provjeru se ne podsjeća.',
          'Isti zahtjev za istu osobu se ne pravi dvaput dok prvi čeka.',
          'Za svaki zahtjev se vidi broj pokušaja i cijela historija.',
          'Brisanje zahtjeva se upisuje u reviziju; poslane datoteke ostaju na kartonu, nepodijeljene.',
        ],
        keywords: ['zahtjev', 'traži dokument', 'novi radnik', 'paket', 'prihvati', 'vrati', 'provjera', 'za provjeru', 'kasni', 'rok', 'pasoš'],
      },
      en: {
        title: 'Requests for documents',
        summary:
          'The office asks people for a missing document; the person sends it from the phone, and the office checks it and accepts it or sends it back with a reason.',
        steps: [
          'Open Documents on the left rail, then Document requests, and click Request a document. Choose one or more people and what is needed; the New worker package asks for the usual set at once. Add a deadline if you like.',
          'The people are notified in the app straight away. There they see what is asked and send photos or a PDF.',
          'What arrives waits under To check. Open the request (Check) and look at the files sent.',
          'Accept: enter Valid until if the document expires. The document is filed on the person\'s record and shared with them.',
          'Or send it back with a reason (at least five characters, nothing about the person\'s health): the person is notified and sends again. Ready-made reasons are offered, such as the picture is blurred.',
          'Withdraw a request that is no longer needed; a withdrawn request can also be deleted for good.',
        ],
        example:
          'For three new workers request the New worker package with a deadline in five days. Five pictures arrive for each. Send one passport photo back with the reason The picture is blurred and accept the rest. The Only overdue filter shows who is late.',
        tips: [
          'Deadline: the person is reminded in the app the day before and on the day, and you get one notice if the deadline passes with nothing sent. A request waiting for your check is not reminded.',
          'The same request for the same person is not made twice while the first waits.',
          'Each request shows the number of attempts and the whole history.',
          'Deleting a request is recorded in the audit; the files sent stay on the record, unshared.',
        ],
        keywords: ['request', 'ask for document', 'new worker', 'package', 'accept', 'return', 'check', 'overdue', 'passport'],
      },
    },
  },
  {
    id: 'revizija-citanja',
    group: 'documents',
    path: paths.audit,
    audience: office,
    content: {
      sr: {
        title: 'Ko je otvarao osjetljive dokumente',
        summary:
          'Ljekarski pregled, lični dokument i viza su osjetljivi. Kad ih otvori ili izveze neko osim same osobe, to se upisuje u reviziju.',
        steps: [
          'Otvorite Trag izmjena u izborniku Administracija.',
          'Filtrirajte po radnji Otvoreno i vrsti zapisa Prilog (Attachment).',
          'Za svaki upis vidite ko je otvorio dokument, kada i odakle. Vlastiti dokument osobe se ne bilježi.',
        ],
        example:
          'Želite znati ko je pogledao Markov ljekarski nalaz u oktobru: Trag izmjena > radnja Otvoreno > vrsta Prilog > period oktobar.',
        tips: [
          'Aplikacija na telefonu ne dozvoljava snimak ekrana dok je takav dokument otvoren, a privremena kopija se briše kad osoba zatvori ekran ili se odjavi.',
          'Brisanje zahtjeva za dokument i drugi osjetljivi koraci takođe ostavljaju trag u reviziji.',
        ],
        keywords: ['revizija', 'audit', 'ko je otvorio', 'trag', 'osjetljivo', 'gdpr', 'privatnost', 'ljekarski'],
      },
      en: {
        title: 'Who opened sensitive documents',
        summary:
          'A medical check, an ID and a visa are sensitive. When anyone other than the person opens or exports one, it is recorded in the audit.',
        steps: [
          'Open Audit trail in the Administration menu.',
          'Filter by the action Viewed and the record type Attachment.',
          'For each entry you see who opened the document, when and from where. A person\'s own document is not recorded.',
        ],
        example:
          'To know who looked at Marko\'s medical report in October: Audit trail > action Viewed > type Attachment > period October.',
        tips: [
          'The phone app does not allow a screenshot while such a document is open, and its temporary copy is deleted when the person leaves the screen or signs out.',
          'Deleting a document request and other sensitive steps also leave a trace in the audit.',
        ],
        keywords: ['audit', 'who opened', 'trace', 'sensitive', 'gdpr', 'privacy', 'medical'],
      },
    },
  },
  {
    id: 'knjiga-pregled',
    group: 'money',
    path: paths.bookOverview,
    audience: canViewBook,
    content: {
      sr: {
        title: 'Knjiga troškova: kako teče novac',
        summary:
          'Svaki prihod, trošak, uplata i isplata upiše se samo jednom, kao jedan red u Knjizi. Zbirovi, dobit, ko nam duguje i obračun mjeseca računaju se sami iz tih redova.',
        steps: [
          'Počnite od Pregleda novca i dobiti: ulaz, izlaz i dobit za izabrani period, s grafikonom po mjesecima. Period biraju dugmad ili ručno Od i Do.',
          'Troškove upisujte u Knjizi zapisa, brzim unosom na vrhu: datum, vrsta, opis, gradilište, druga strana i iznos. Zbir se odmah promijeni.',
          'Gorivo (DKV izvod), smještaj i vozila nastaju sami; njih ne upisujete.',
          'Pogledajte Ko nam duguje, kome dugujemo: otvorene fakture, plate i računi sa rokovima. Označite plaćeno kad se uplata desi.',
          'Pred kraj mjeseca otvorite Obračun mjeseca, provjerite listu Za provjeru, zaključajte mjesec i izvezite za knjigovođu.',
        ],
        example:
          'Upišite Gorivo sedmica 41, 1.980 € za gradilište Hala B. Zbir troškova i dobit se odmah promijene, a obračun Hale B pokazuje novu maržu.',
        tips: [
          'Prihod (izdata faktura) i trošak mijenjaju dobit. Uplata i isplata samo mijenjaju ko kome šta duguje.',
          'Knjiga je zatvorena za sve osim Super Admina, dok joj Super Admin ne dodijeli pristup.',
          'Program ne vodi porez, PDV ni doprinose i nije zamjena za knjigovođu; knjigovođa dobija uredan izvoz.',
        ],
        keywords: ['knjiga', 'evidencija', 'troškovi', 'prihod', 'dobit', 'novac', 'zapis', 'unos', 'faktura', 'gorivo', 'dkv'],
      },
      en: {
        title: 'The book of money: how money flows',
        summary:
          'Every income, cost, payment and payout is entered once, as one row in the Book. Totals, profit, who owes us and the month\'s settlement are all worked out from those rows.',
        steps: [
          'Start from the Money and profit overview: money in, out and profit for the chosen period, with a chart by month. Choose the period with the buttons or From and To.',
          'Enter costs in the Book of entries with the quick entry on top: date, kind, description, site, other party and amount. The total changes at once.',
          'Fuel (the DKV statement), accommodation and vehicles arise on their own; you do not enter them.',
          'Look at Who owes us, whom we owe: open invoices, pay and bills with due dates. Mark paid when the payment happens.',
          'Near the end of the month open the Month settlement, check the To check list, lock the month and export for the accountant.',
        ],
        example:
          'Enter Fuel week 41, 1,980 € for the site Hall B. The cost total and profit change at once, and Hall B\'s settlement shows the new margin.',
        tips: [
          'Income (an issued invoice) and a cost change profit. A payment or payout only changes who owes whom.',
          'The Book is closed to everybody but the Super Admin until the Super Admin grants access.',
          'The program does not keep tax, VAT or contributions and does not replace the accountant; the accountant gets a tidy export.',
        ],
        keywords: ['book', 'ledger', 'costs', 'income', 'profit', 'money', 'entry', 'invoice', 'fuel', 'dkv'],
      },
    },
  },
  {
    id: 'knjiga-uvoz',
    group: 'money',
    path: paths.bookImport,
    audience: canViewBook,
    content: {
      sr: {
        title: 'Uvoz mjeseca iz Excela',
        summary:
          'Stare mjesece iz tabele možete prebaciti bez prekucavanja. Uvoz ide u četiri koraka, s provjerom svakog reda prije upisa.',
        steps: [
          'Otvorite Knjiga troškova > Uvoz iz Excela. Prevucite Excel datoteku u okvir ili kliknite Izaberi datoteku (isprobati možete i s Učitaj primjer).',
          'Izaberite list i mjesec na koji se odnosi.',
          'Program pogodi koja kolona je šta (sati, cijena, akontacija…). Ispravite gdje pogriješi, a nepotrebne kolone preskočite.',
          'Pregledajte provjeru: Upozorenje (žuto) se uvozi, ali ga treba pogledati; Greška (crveno) se ne uvozi.',
          'Potvrdite. Uvoz se može poništiti, osim ako se mjesec poslije ručno mijenjao ili je zaključan.',
        ],
        example:
          'Imate tabelu za septembar s gradilištima u redovima i sedmicama u kolonama. Učitajte je, označite kolone sedmica kao Sati (sedmica), ispravite Hala B ako je pogrešno prepoznata, pa potvrdite. Septembar se pojavi kao obračun s istim brojkama.',
        tips: [
          'Samo Super Admin uvozi cijele mjesece.',
          'Mjesec koji već ima zapise uvozi se samo u prazna polja, ili izaberite drugi mjesec.',
          'Datoteka se čita u pregledniku i ne čuva; upisuje se samo ono što potvrdite.',
        ],
        keywords: ['uvoz', 'import', 'excel', 'xlsx', 'tabela', 'stari mjeseci', 'prebaci'],
      },
      en: {
        title: 'Importing a month from Excel',
        summary:
          'Old months from a spreadsheet can be brought in without retyping. The import goes in four steps, with every row checked before it is written.',
        steps: [
          'Open Book of money > Import from Excel. Drag the Excel file into the box or click Choose file (you can also try Load an example).',
          'Choose the sheet and the month it is about.',
          'The program guesses which column is what (hours, price, advance…). Correct it where it is wrong and skip columns you do not need.',
          'Review the check: a Warning (yellow) is imported but should be looked at; an Error (red) is not imported.',
          'Confirm. The import can be undone unless the month was changed by hand afterwards or is locked.',
        ],
        example:
          'You have a September sheet with sites in rows and weeks in columns. Load it, mark the week columns as Hours (week), correct Hall B if it was misread, then confirm. September appears as a settlement with the same figures.',
        tips: [
          'Only the Super Admin imports whole months.',
          'A month that already has entries is imported only into its empty cells, or choose another month.',
          'The file is read in the browser and not kept; only what you confirm is written.',
        ],
        keywords: ['import', 'excel', 'xlsx', 'spreadsheet', 'old months', 'bring in'],
      },
    },
  },
  {
    id: 'knjiga-obracun',
    group: 'money',
    path: paths.bookSettlement,
    audience: canViewBook,
    content: {
      sr: {
        title: 'Obračun mjeseca i paket za knjigovođu',
        summary:
          'Obračun je tabela po gradilištima kao u Excelu, ali izračunata iz Knjige. Kad je sve provjereno, mjesec se zaključa i izveze.',
        steps: [
          'Otvorite Obračun mjeseca i izaberite mjesec. Sate upišite iz potpisanih satnica.',
          'Za svako gradilište izaberite način naplate: satnica (sati × cijena), paušal ili mjerenje (iznos izdatih faktura). Vidite i koliko bi bilo po satnici.',
          'Pregledajte listu Za provjeru: previše sati, paušal bez fakture, kašnjenje. Označite riješeno ono što jeste.',
          'U Isplatama radnicima dodajte akontacije (više puta u mjesecu, gotovinom ili prenosom); konačna isplata je zarada minus akontacije.',
          'Zaključajte mjesec. Brojke se više ne mijenjaju; ispravka traži otključavanje i upisuje ko je i kada.',
          'Na stranici Knjiga zapisa otvorite Izvoz i izaberite Izvoz s dokumentima (ZIP): tabela zapisa i dokumenti priloženi uz njih, jedna mapa po zapisu.',
        ],
        example:
          'Za Zgradu D (paušal) promijenite način na satnicu i odmah vidite razliku u marži. Zaključajte oktobar kad su sve kućice označene, pa knjigovođi pošaljite ZIP.',
        tips: [
          'Kratka napomena u ZIP-u navodi koliko troškova nije imalo priložen dokument.',
          'Obračun mjeseca upozorava na troškove proknjižene bez dokumenta; to je samo podsjetnik i ne sprječava zaključivanje.',
          'Zaključavanje zamrzava tabelu obračuna, ne i unose: red dodat poslije vidi se u Knjizi zapisa, ali ne mijenja zaključanu tabelu. Završite unose prije zaključavanja.',
          'Zaključan mjesec se ne može obrisati dok se ne otključa.',
        ],
        keywords: ['obračun', 'zaključaj', 'isplata', 'akontacija', 'knjigovođa', 'zip', 'izvoz', 'paušal', 'satnica', 'marža'],
      },
      en: {
        title: 'Month settlement and the accountant package',
        summary:
          'The settlement is a table by site like the Excel one, but worked out from the Book. When everything is checked the month is locked and exported.',
        steps: [
          'Open Month settlement and choose the month. Enter the hours from the signed timesheets.',
          'For each site choose the billing: hourly (hours × price), flat rate or measured (the amount of invoices issued). You also see what it would be hourly.',
          'Review the To check list: too many hours, a flat rate with no invoice, lateness. Mark as resolved what is.',
          'In Payouts add advances (several times a month, cash or transfer); the final payout is earnings minus advances.',
          'Lock the month. The figures no longer change; a correction needs unlocking and records who and when.',
          'On the Book of entries page open Export and choose Export with documents (ZIP): the table of entries and the documents attached to them, one folder per entry.',
        ],
        example:
          'For Building D (flat rate) switch the billing to hourly and see the difference in margin at once. Lock October when every box is ticked, then send the accountant the ZIP.',
        tips: [
          'A short note inside the ZIP states how many costs had no document attached.',
          'The settlement warns about costs booked with no document; it is only a reminder and does not prevent locking.',
          'Locking freezes the settlement table, not the entries: a line added afterwards shows in the book of entries but does not change the locked table. Finish the entries before locking.',
          'A locked month cannot be deleted until it is unlocked.',
        ],
        keywords: ['settlement', 'lock', 'payout', 'advance', 'accountant', 'zip', 'export', 'flat rate', 'hourly', 'margin'],
      },
    },
  },
  {
    id: 'sedmicni-izvjestaji',
    group: 'work',
    path: paths.weeklyReports,
    audience: office,
    content: {
      sr: {
        title: 'Sedmični izvještaji sa gradilišta',
        summary:
          'Poslovođa svake sedmice pošalje potpisane sate ili Aufmaß za gradilište; ured ih pregleda u programu i odobri ili vrati na korekciju.',
        steps: [
          'Otvorite Planiranje, pa Sedmični izvještaji. Čipovi iznad liste pokazuju šta čeka: Čeka pregled, Potrebna korekcija i Kasni.',
          'Pregledaj otvara izvještaj u programu, bez skidanja: lijevo su sve slike (listanje i uvećanje), a desno radnici i sati koje je poslovođa unio. Podaci su uredivi: promijenite sate, zamijenite radnika, dodajte ili obrišite red. Preuzmi snima sliku na računar.',
          'Odobri i upiši u Evidenciju stavlja sate svakog radnika u pravu sedmicu mjesečne tabele (Finansije > Evidencija). Uz svaki red piše šta je trenutno u tabeli; ako se razlikuje, birate Zamijeni ili Zadrži staro. Mjesec mora imati Evidenciju i ne smije biti zaključan. Upisane ćelije su zelene. Poslovođa dobija obavijest.',
          'Potrebna korekcija traži razlog (šta treba ispraviti). Poslovođa ga vidi u obavijesti i šalje izvještaj ponovo; novi izvještaj zamjenjuje stari, pa za gradilište i sedmicu uvijek postoji jedan.',
          'Izvještaj koji je stigao mailom, telefonom ili poštom unesite sami: Unesi izvještaj. Izaberite gradilište, sedmicu i poslovođu, upišite odakle je došao (npr. mail od kupca 20.10.), dodajte slike i radnike. Admin odmah dobije pregled za provjeru i odobravanje; Sačuvaj za kasnije ostavlja izvještaj u Čeka pregled. Voditelj projekta može unijeti izvještaj, a odobrava Admin. Poslovođa dobija obavijest da je uprava unijela izvještaj, a ako za tu sedmicu već postoji izvještaj, unos se dodaje u njega.',
          'Kasni pokazuje gradilišta koja su prošle sedmice imala raspoređene radnike, a do ponedjeljka u 12:00 nemaju izvještaj, s imenom poslovođe kojeg treba pitati.',
          'Sedmica koja prelazi u novi mjesec: poslovođa upisuje sate za oba mjeseca, a svaki ide u Evidenciju svog mjeseca. Iznad tabele piše koliko je sati odobreno iz izvještaja, a koliko upisano ručno; sate uvijek možete ručno ispraviti u tabeli (ćelija prestaje biti zelena).',
        ],
        example:
          'U utorak ujutro poslovođa Hale B dobije opomenu jer izvještaj za KW42 nije stigao. Pošalje ga u srijedu; ured dobija obavijest, otvara Pregledaj, vidi da nema potpisa kupca i vraća ga s razlogom. Poslovođa pošalje novi, ured ga odobri.',
        tips: [
          'Rok za slanje je ponedjeljak u 12:00; opomena poslovođi ide u utorak ujutro, jednom po gradilištu i sedmici.',
          'Ured dobija obavijest čim izvještaj stigne, a na početnoj stranici se može dodati widget Sedmični izvještaji.',
          'Izvještaj vraćen na korekciju se ne računa kao da nije poslat: gradilište se ne pojavljuje pod Kasni.',
        ],
        keywords: ['sedmični izvještaj', 'izvještaj', 'satnica', 'aufmaß', 'pregled', 'odobri', 'korekcija', 'kasni', 'poslovođa', 'kw'],
      },
      en: {
        title: 'Weekly site reports',
        summary:
          'Every week the foreman sends the signed hours or the Aufmaß for a site; the office reviews them in the program and approves them or sends them back for correction.',
        steps: [
          'Open Planning, then Weekly reports. The chips above the list show what waits: Waiting for review, Needs correction and Late.',
          'Preview opens the report in the program, without downloading: all the pictures are on the left (turn the pages, zoom), and on the right the workers and hours the foreman entered. The data is editable: change the hours, replace a worker, add or delete a row. Download saves the picture to your computer.',
          'Approve and enter in the Ledger puts each worker\'s hours in the right week of the monthly sheet (Finance > Ledger). Each row says what the sheet holds now; if it differs you choose Replace or Keep the old. The month must have a Ledger and not be closed. Entered cells are green. The foreman is notified.',
          'Needs correction asks for a reason (what to fix). The foreman sees it in the notice and sends the report again; the new report replaces the old one, so there is always one per site and week.',
          'Enter a report that came by e-mail, phone or post yourself: Enter a report. Choose the site, week and foreman, say where it came from (e.g. e-mail from the customer 20.10.), add the pictures and workers. An Admin gets the review straight away to check and approve; Save for later leaves the report under Waiting for review. A Project manager can enter a report, and an Admin approves it. The foreman is notified that the office entered a report, and if a report for that week already exists the entry is added to it.',
          'Late shows the sites that had workers posted to them last week and have no report by Monday 12:00, with the foreman to ask.',
          'A week that runs over a month\'s end: the foreman enters hours for both months, and each goes to the Ledger of its own month. Above the sheet it says how many hours were approved from reports and how many typed by hand; you can always correct the hours in the sheet by hand (the cell stops being green).',
        ],
        example:
          'On Tuesday morning the foreman of Hall B gets a reminder because the report for KW42 has not come in. He sends it on Wednesday; the office is notified, opens Preview, sees the customer\'s signature is missing and sends it back with a reason. The foreman sends a new one and the office approves it.',
        tips: [
          'The deadline is Monday 12:00; the foreman\'s reminder goes out on Tuesday morning, once per site and week.',
          'The office is notified as soon as a report arrives, and the Weekly reports widget can be added to the home page.',
          'A report sent back for correction does not count as missing: the site does not appear under Late.',
        ],
        keywords: ['weekly report', 'report', 'timesheet', 'aufmaß', 'review', 'approve', 'correction', 'late', 'foreman', 'kw'],
      },
    },
  },
  {
    id: 'raspored',
    group: 'work',
    path: paths.schedule,
    audience: staff,
    content: {
      sr: {
        title: 'Raspored i planiranje radne snage',
        summary:
          'Raspored pokazuje ko je gdje i kada, šta svakom gradilištu nedostaje i ko je odsutan, te upozorava prije nego što nastane problem.',
        steps: [
          'Otvorite Raspored. Izaberite prikaz (po danu, vremenska traka i dr.) i razdoblje.',
          'Kliknite radnika (ili traku u vremenskoj traci): u prozoru izaberite razdoblje Od/Do i gradilište na koje se raspoređuje, ili Slobodan. Izmjena postavlja raspored za to razdoblje.',
          'Ili ga povucite: u prikazu Po gradilištima na karticu gradilišta, na drugog radnika (zamjena) ili među Slobodne; u vremenskoj traci traku radnika na gradilište iznad tabele. Na telefonu radnika zadržite prstom. Nakon puštanja prozor piše radnika, gradilište i datume, a raspored se mijenja tek kad potvrdite.',
          'Na kartici gradilišta kliknite prazno mjesto: nudi se lista slobodnih u razdoblju i prebacivanje s drugog gradilišta; radnici prave vještine su na vrhu.',
          'Za odsutnog izaberite zamjenu: dvojica mogu razmijeniti raspored, ili popunite mjesto slobodnim radnikom s potrebnim uvjerenjima.',
          'Svaka izmjena prikazuje poruku s dugmetom Poništi. Pregledajte i upozorenja, Zahtijeva pažnju te Raspored na papiru ako ga treba odštampati.',
        ],
        example:
          'Ana ima godišnji odmor od 20. do 24. oktobra, a stoji na Hali B. Raspored označi prazno mjesto; kliknete ga i među slobodnima izaberete zidara s uvjerenjem za skele.',
        tips: [
          'Predradnik raspoređuje samo ljude i gradilišta svoje poslovne jedinice.',
          'Izmjene se pamte u istoriji promjena.',
          'Praznici i radni dani se uzimaju u obzir.',
        ],
        keywords: ['raspored', 'planiranje', 'smjena', 'gradilište', 'zamjena', 'odsutni', 'potreba', 'tim', 'ekipa', 'papir'],
      },
      en: {
        title: 'Schedule and workforce planning',
        summary:
          'The schedule shows who is where and when, what each site lacks and who is absent, and warns before a problem arises.',
        steps: [
          'Open Schedule. Choose a view (by day, timeline and so on) and the period.',
          'Click a worker (or a bar in the timeline): in the window choose the period From/To and the site to schedule on, or Free. The change sets the schedule for that period.',
          'Or drag them: in By site onto a site card, onto another worker (swap) or into Free; in the timeline the bar of a worker onto a site in the strip above the table. On a phone, hold the worker with a finger. After you let go the window names the worker, the site and the dates, and the schedule changes only when you confirm.',
          'On the card of a site click an empty place: you get the list of those free in the period and the option to move someone from another site; people with the right skill are on top.',
          'For an absent person choose a replacement: two people can swap their schedule, or fill the place with a free worker who holds the needed certificates.',
          'Every change shows a message with an Undo button. Also review the warnings, Needs attention and the paper schedule if you need to print it.',
        ],
        example:
          'Ana is on annual leave from 20 to 24 October and is on Hall B. The schedule marks the empty place; you click it and pick a free bricklayer with the scaffolding certificate.',
        tips: [
          'A foreman schedules only the people and sites of their own business unit.',
          'Changes are kept in the history.',
          'Holidays and working days are taken into account.',
        ],
        keywords: ['schedule', 'planning', 'shift', 'site', 'replace', 'absent', 'needs', 'team', 'crew', 'paper'],
      },
    },
  },
  {
    id: 'poslovne-jedinice',
    group: 'work',
    path: paths.organization,
    audience: office,
    content: {
      sr: {
        title: 'Poslovne jedinice',
        summary:
          'Poslovna jedinica je dio vaše organizacije: filijala, predstavništvo ili zasebno pravno lice. Nije firma klijenta. Mogu biti jedna ispod druge.',
        steps: [
          'Otvorite Administracija > Firma i poslovne jedinice. Na kartici Poslovne jedinice dodajte jedinicu: naziv, tip i boju.',
          'Dodijelite joj gradilišta i zaposlene (zaposlenje u jedinici ima datume).',
          'Po potrebi postavite jedinicu ispod druge i rukovodioca.',
          'U zaglavlju izaberite jedinicu: liste, troškovi, prihodi i početna stranica sužavaju se na nju.',
          'Za sate i plate izaberite računaju li se po gradilištu ili po poslodavcu.',
        ],
        example:
          'Firma ima Sarajevo i Banja Luku. Dodajte obje jedinice, prebacite Marka iz Sarajeva u Banja Luku od 1. novembra (zatvara prethodnu jedinicu dan ranije) i u zaglavlju izaberite Banja Luka da vidite samo njene brojke.',
        tips: [
          'Poreski i registracioni podaci vide se samo Super Adminu i računima kojima je to pravo dodijeljeno.',
          'Jedinica koja ima pod-jedinice ili projekte ne može se obrisati, samo isključiti.',
          'Jedinice se mogu gnijezditi do ograničenog broja nivoa.',
        ],
        keywords: ['poslovna jedinica', 'filijala', 'predstavništvo', 'organizacija', 'firma', 'branch', 'jedinica'],
      },
      en: {
        title: 'Business units',
        summary:
          'A business unit is a part of your own organisation: a branch, an office or a separate legal entity. It is not a client company. Units can be nested.',
        steps: [
          'Open Administration > Company and business units. On the Business units tab add a unit: name, kind and colour.',
          'Assign it sites and employees (employment in a unit has dates).',
          'If needed place the unit under another and set its head.',
          'Choose a unit in the header: lists, costs, income and the home page narrow to it.',
          'For hours and pay choose whether they are counted by site or by employer.',
        ],
        example:
          'The company has Sarajevo and Banja Luka. Add both units, move Marko from Sarajevo to Banja Luka from 1 November (this closes the previous unit a day earlier) and choose Banja Luka in the header to see only its figures.',
        tips: [
          'Tax and registration data are visible only to the Super Admin and accounts granted that right.',
          'A unit with sub-units or projects cannot be deleted, only switched off.',
          'Units can be nested to a limited number of levels.',
        ],
        keywords: ['business unit', 'branch', 'office', 'organisation', 'company', 'unit'],
      },
    },
  },
  {
    id: 'prijava-sigurnost',
    group: 'account',
    path: paths.changePassword,
    audience: everyone,
    content: {
      sr: {
        title: 'Prijava, lozinka i drugi faktor',
        summary:
          'Kako se prijaviti, promijeniti lozinku i, kad se uključi, zaštititi nalog dodatnim kodom iz aplikacije.',
        steps: [
          'Prijavite se e-poštom i lozinkom. Ako ste zaboravili lozinku, na stranici za prijavu izaberite Zaboravili ste lozinku.',
          'Lozinku promijenite iz izbornika naloga (Promijeni lozinku): najmanje 8 znakova, s velikim i malim slovom i cifrom.',
          'Kad je drugi faktor uključen za vašu ulogu, nakon lozinke upisujete šestocifreni kod iz aplikacije za provjeru (TOTP) ili jedan od kodova za oporavak.',
          'Prvo uključivanje: u izborniku naloga skenirajte QR kod aplikacijom, upišite kod i sačuvajte kodove za oporavak na sigurno mjesto.',
        ],
        example:
          'Izgubili ste telefon s aplikacijom za provjeru: prijavite se jednim od sačuvanih kodova za oporavak i uključite drugi faktor na novom telefonu. Ako nemate kodove, Super Admin može resetovati vaš drugi faktor.',
        tips: [
          'Nalog se privremeno zaključava nakon više pogrešnih pokušaja.',
          'Drugi faktor je na testnom serveru pripremljen, ali se uključuje tek kad Super Admin to odluči.',
          'Svaka promjena lozinke i uloge ostavlja trag u reviziji.',
        ],
        keywords: ['prijava', 'lozinka', 'šifra', 'password', 'mfa', 'totp', 'dvofaktorska', 'drugi faktor', 'kod', 'zaboravljena', 'nalog', 'sigurnost'],
      },
      en: {
        title: 'Signing in, password and the second factor',
        summary:
          'How to sign in, change your password and, once switched on, protect the account with an extra code from an app.',
        steps: [
          'Sign in with your e-mail and password. If you forgot the password, choose Forgot your password on the sign-in page.',
          'Change the password from the account menu (Change password): at least 8 characters, with an upper-case and a lower-case letter and a digit.',
          'When the second factor is switched on for your role, after the password you enter the six-digit code from the authenticator app (TOTP) or one of the recovery codes.',
          'First time: in the account menu scan the QR code with the app, enter the code and keep the recovery codes somewhere safe.',
        ],
        example:
          'You lost the phone with the authenticator app: sign in with one of the saved recovery codes and set up the second factor on the new phone. If you have no codes, a Super Admin can reset your second factor.',
        tips: [
          'The account is locked for a while after several wrong attempts.',
          'The second factor is prepared on the test server but is switched on only when the Super Admin decides.',
          'Every password and role change leaves a trace in the audit.',
        ],
        keywords: ['sign in', 'login', 'password', 'mfa', 'totp', 'two-step', 'second factor', 'code', 'forgot', 'account', 'security'],
      },
    },
  },
  {
    id: 'pretraga-precice',
    group: 'account',
    path: paths.home,
    audience: everyone,
    content: {
      sr: {
        title: 'Pretraga (Ctrl+K) i prečice',
        summary:
          'Jedno polje za sve: stranice, radnje, zapisi, dokumenti i ova uputstva. Otvara se prečicom Ctrl+K (na Macu Cmd+K).',
        steps: [
          'Pritisnite Ctrl+K bilo gdje u panelu.',
          'Upišite bar dva slova. Prikazuju se stranice menija, radnje (npr. Otpremi dokument), zapisi (zaposleni, projekti, vozila, dokumenti, zahtjevi…) i uputstva.',
          'Strelicama gore i dolje birajte, Enter otvara, Esc zatvara.',
          'Bez upisa vidite nedavno otvorene zapise, radnje i sve stranice koje možete otvoriti.',
          'Zvjezdicom uz stranicu je dodajete u favorite.',
          'Tipka ? (kad ne pišete u polje) otvara popis svih prečica.',
        ],
        example:
          'Upišite viza: odmah vidite dokumente s tom riječi u naslovu ili oznaci, uputstvo o rokovima važenja i radnju Otpremi dokument. Upišite hala: nalazite projekat Hala B i njegove zapise.',
        tips: [
          'Rezultati su samo ono što vaša uloga smije otvoriti.',
          'Pretraga dokumenata traži naslov, ime datoteke, vlasnika i oznake.',
          'Uputstva se traže i po srodnim riječima (npr. otpremi, rok, uvoz).',
          'Dugme Izgled u zaglavlju (na telefonu u meniju naloga) bira svijetao ili taman izgled, ili prati uređaj; izbor se pamti.',
        ],
        keywords: ['pretraga', 'ctrl+k', 'prečice', 'komanda', 'palette', 'traži', 'brzo', 'tipkovnica', 'prečica'],
      },
      en: {
        title: 'Search (Ctrl+K) and shortcuts',
        summary:
          'One box for everything: pages, actions, records, documents and these guides. It opens with Ctrl+K (Cmd+K on a Mac).',
        steps: [
          'Press Ctrl+K anywhere in the panel.',
          'Type at least two letters. Menu pages, actions (such as Upload document), records (employees, projects, vehicles, documents, requests…) and guides are shown.',
          'Use the up and down arrows to choose, Enter opens, Esc closes.',
          'With nothing typed you see recently opened records, actions and every page you can open.',
          'The star beside a page adds it to favourites.',
          'The ? key (when you are not typing in a field) opens the list of all shortcuts.',
        ],
        example:
          'Type visa: you immediately see documents with that word in the title or label, the guide about expiry dates and the Upload document action. Type hall: you find the project Hall B and its records.',
        tips: [
          'Results are only what your role may open.',
          'The document search covers the title, file name, owner and labels.',
          'Guides are also found by related words (such as upload, deadline, import).',
          'The Appearance button in the header (in the account menu on a phone) chooses light, dark or following the device; the choice is remembered.',
        ],
        keywords: ['search', 'ctrl+k', 'shortcuts', 'command', 'palette', 'find', 'quick', 'keyboard'],
      },
    },
  },
  {
    id: 'pocetna-od-jucer',
    group: 'work',
    path: paths.home,
    audience: staff,
    content: {
      sr: {
        title: 'Početna: šta se promijenilo od jučer',
        summary:
          'Uz brojeve na početnoj piše koliko su veći ili manji nego što ste ih vidjeli jučer, pa odmah znate da li ono što čeka raste ili se smanjuje.',
        steps: [
          'Otvorite Početnu. Uz broj u widgetu Zahtijeva pažnju, u kartici Danas na telefonu, uz Aktivne radnike, Otvorene radne naloge, Vozila i alate te Narudžbe pojavi se sitan natpis, npr. +2 od jučer.',
          'Boja govori šta promjena znači: zelena je bolje, crvena je lošije (više onoga što čeka odgovor, manje slobodnih vozila), siva je samo promjena (broj aktivnih radnika).',
          'Natpis nestaje kad je broj isti kao jučer. Klik na red ili pločicu vodi na listu gdje se to rješava.',
          'Ako izaberete poslovnu jedinicu u zaglavlju, brojevi se porede s jučerašnjim brojevima te iste jedinice.',
        ],
        example:
          'Jučer je 3 zahtjeva za odsustvo čekalo odgovor, danas ih je 5. Uz Zahtjevi za odsustvo piše crveno +2 od jučer: dvoje je novih, pa ih pogledajte prvo.',
        tips: [
          'Poredi se s onim što je vaš nalog zadnji put vidio jučer. Prvog dana korištenja, i dan poslije dana kad Početna nije otvarana, nema natpisa.',
          'Pamte se samo broj i dan, nikad sadržaj, i samo za vaš nalog; čuvaju se tri dana.',
          'Trošak i prihod već pokazuju promjenu u odnosu na prethodni mjesec ili razdoblje.',
        ],
        keywords: ['od jučer', 'promjena', 'početna', 'widget', 'poređenje', 'jučer', 'broj', 'raste', 'pada', 'trend'],
      },
      en: {
        title: 'Home: what changed since yesterday',
        summary:
          'Next to the figures on the home page it says how much larger or smaller they are than when you saw them yesterday, so you know at once whether what waits is growing or shrinking.',
        steps: [
          'Open Home. Next to a figure in the Needs attention widget, in the Today card on the phone, and next to Active employees, Open work items, Vehicles and tools, and Orders a small note appears, for example +2 since yesterday.',
          'The colour says what the change means: green is better, red is worse (more waiting for an answer, fewer free vehicles), grey is only a change (the number of active employees).',
          'The note disappears when the figure is the same as yesterday. A press on the row or the tile goes to the list where it is dealt with.',
          "If you choose a business unit in the header, the figures are compared with yesterday's figures of that same unit.",
        ],
        example:
          'Yesterday 3 leave requests were waiting for an answer, today there are 5. Leave requests shows a red +2 since yesterday: two are new, so look at them first.',
        tips: [
          'It is compared with what your account last saw yesterday. On the first day of use, and the day after a day when Home was not opened, there is no note.',
          'Only the number and the day are kept, never the content, and only for your account; they are kept for three days.',
          'Cost and income already show the change from the previous month or period.',
        ],
        keywords: ['since yesterday', 'change', 'home', 'widget', 'comparison', 'yesterday', 'figure', 'growing', 'falling', 'trend'],
      },
    },
  },
  {
    id: 'pocetna-widgeti',
    group: 'work',
    path: paths.home,
    audience: staff,
    content: {
      sr: {
        title: 'Početna: widgeti, Finansije i traka za prazno',
        summary:
          'Početna se sastavlja od widgeta koje birate. Novi nalog dobija šest najvažnijih; ostale dodajete, premještate i uklanjate sami.',
        steps: [
          'Pritisnite Dodaj widget i izaberite iz spiska. Widget uklanjate × na kartici, a premještate hvatanjem ručke s tačkicama.',
          'Finansije su jedna kartica s tri taba: Pregled (prihod, rashod, zarada u odnosu na prethodni period), Trend (po mjesecima ili u izabranom periodu) i Struktura (na šta odlazi novac). Dugme za postavke u Strukturi bira jedan projekat ili cijelu firmu. Period se bira na vrhu stranice.',
          'Widget kojem nema šta da pokaže postaje tanka traka s kvačicom i jednom rečenicom; kad ima sadržaja, sam se proširi.',
          'Na računaru je kartica visoka koliko njen sadržaj. Ako je razvučete po visini, ostaje kakvu ste je namjestili.',
          'Podešavanje sistema je mala značka desno iznad widgeta. Pritisak je otvara, a × je sakriva dok se ne pojavi nešto novo.',
        ],
        example:
          'Nemate nijednu poslovnu jedinicu, pa je Poslovne jedinice traka s „Još nema poslovnih jedinica” i vezom na strukturu firme. Kad dodate prvu, ista kartica pokaže jedinice i brojeve.',
        tips: [
          'Starih pet finansijskih widgeta zamijenila je jedna kartica Finansije; postojeći raspored je dobio jednu na mjestu prve.',
          'Nalog koji smije vidjeti samo postotke dobija postotke, bez tabova.',
          'Na telefonu se widgeti slažu u jedan stub; ispod kartice „Danas” widget „Još čeka” pokazuje ono što kartica ne nabraja.',
        ],
        keywords: ['početna', 'widget', 'raspored', 'finansije', 'trend', 'struktura', 'traka', 'prazno', 'značka', 'podešavanje'],
      },
      en: {
        title: 'Home: widgets, Finance and the strip for empty',
        summary:
          'The home page is built from widgets you choose. A new account gets the six most important; you add, move and remove the rest yourself.',
        steps: [
          'Press Add widget and pick from the list. Remove a widget with × on its card, and move it by grabbing the dotted handle.',
          'Finance is one card with three tabs: Overview (income, spending, profit against the previous period), Trend (by month or in the chosen period) and Structure (where the money goes). The settings button in Structure picks one project or the whole company. The period is chosen at the top of the page.',
          'A widget with nothing to show becomes a thin strip with a tick and one sentence; when there is something, it expands by itself.',
          'On the computer a card is as tall as its content. If you stretch it by height, it stays as you set it.',
          'System setup is a small badge above the widgets on the right. Pressing it opens it, and × hides it until something new comes up.',
        ],
        example:
          'You have no business unit yet, so Business units is a strip saying "No business units yet" with a link to the company structure. When you add the first, the same card shows the units and the figures.',
        tips: [
          'The five old finance widgets were replaced by the one Finance card; an existing layout got one in the place of the first.',
          'An account that may see only percentages gets percentages, without tabs.',
          'On the phone the widgets stack in one column; under the Today card the Still waiting widget shows what the card does not list.',
        ],
        keywords: ['home', 'widget', 'layout', 'finance', 'trend', 'structure', 'strip', 'empty', 'badge', 'setup'],
      },
    },
  },
  {
    id: 'widgeti-pregled',
    group: 'work',
    path: paths.home,
    audience: staff,
    content: {
      sr: {
        title: 'Widgeti: pregled i pažnja',
        summary:
          'Widgeti koji govore šta je danas važno: ključni brojevi, šta nekoga čeka, šta ističe, šta je novo i kako su podijeljene jedinice.',
        steps: [
          'Ključni pokazatelji. Pet brojeva o firmi na jednom mjestu, da za pola minute vidite stanje. Kako se računa: Trošak ovog mjeseca je ukupan trošak firme od 1. u mjesecu do danas (odobreni sati po stopi radnika, ručna plaćanja, izdat materijal s cijenom, opšti troškovi, smještaj, vozila i alati), uz promjenu u odnosu na ISTE dane prošlog mjeseca; vidi ga samo nalog s punim finansijskim pravom. Aktivnih radnika su radnici sa statusom Aktivan. Otvoreni radni nalozi su svi koji nisu Zatvoreni ni Otkazani (računaju se i Riješeni), a „u kašnjenju” su oni kojima je rok prije današnjeg dana. Vozila i alati dostupni su oni sa statusom Slobodno, od ukupnog broja. Sve se sužava na izabranu poslovnu jedinicu.',
          'Zahtijeva pažnju. Jedan spisak svega što nekoga čeka, tako da ništa ne propadne: svaka vrsta ima broj i vodi na stranicu gdje se rješava. Kako se računa: Pokazuje se samo ono što uloga može i riješiti. Vrste: zahtjevi za odsustvo na odobravanju; radni sati na pregledu (smjene u statusu Poslano); troškovi vozila na pregledu; narudžbe artikala na čekanju (Zatraženo); zahtjevi za povrat troškova (Zatraženo); zakasnjele fakture (izdate, rok prošao, nisu plaćene u cijelosti; samo uz pravo na Knjigu); zakasnjeli troškovi iz Evidencije (neplaćeni, rok prošao); DKV redovi koji traže pregled; rasporedi koje radnik nije potvrdio (počinju u narednih 7 dana ili traju, aktivan radnik); datumi vozila koji ističu (registracija, tehnički, osiguranje, servis u 30 dana ili prošli; jedan red po vozilu, vozila van upotrebe se ne računaju); uvjerenja radnika koja ističu (30 dana); dokumenti koji ističu (30 dana); zapisi za sređivanje; i, samo za administratora, servis za provjeru virusa koji ne odgovara. Uz svaki broj stoji „od jučer”. Sa izabranom poslovnom jedinicom broje se samo njene stavke; servis za viruse, zapisi za sređivanje i DKV redovi bez vozila (nepoznata kartica) pripadaju firmi u cjelini. Na telefonu je ovo „Još čeka” i izostavlja prve tri vrste, koje već nabraja kartica „Danas”.',
          'Dokumenti koji uskoro ističu. Da nijedan dokument (lična karta, dozvola, certifikat, osiguranje) ne istekne neprimijećen. Kako se računa: Dokumenti s datumom isteka u narednih 30 dana ili već istekli, najbliži prvo (do 8). Crveno je 7 dana ili manje, ili prošlo; dani se broje po kalendaru organizacije. Sa izabranom poslovnom jedinicom: dokumenti njenih radnika, gradilišta, vozila i alata. Kad nema ništa, kartica je jedan red.',
          'Obavještenja i oglasna tabla. Šta je novo za vas: obavještenja koja niste otvorili i zadnje objave firme. Kako se računa: Broj je Vaših nepročitanih obavještenja (lični je, ne zavisi od poslovne jedinice). Ispod su posljednjih 5 objava s oglasne ploče, koje su za cijelu firmu.',
          'Poslovne jedinice. Da vlasnik s više jedinica vidi koliko je koja velika. Kako se računa: Aktivne jedinice kao stablo: ko je šef i koliko radnika (trenutno zaposlenih u njoj) i gradilišta ima, računajući i jedinice ispod nje. Stubići su u odnosu na najveću. Bez jedinica je jedan red s vezom na strukturu firme.',
        ],
        example:
          'Dokumenti ističu za 12 dana, a tri zahtjeva za odsustvo čekaju: „Zahtijeva pažnju” ih nabroji, a pritisak na red otvara listu gdje se rješavaju.',
        tips: [
          'Brojevi se sužavaju na poslovnu jedinicu izabranu u zaglavlju; „od jučer” se poredi s onim što je Vaš nalog vidio jučer.',
          '„Danas”, „ovaj mjesec” i sati računaju se po vremenskoj zoni organizacije, koja se podešava u Podešavanjima firme.',
        ],
        keywords: ['overview', 'brojevi', 'pažnja', 'widget', 'dokumenti', 'ključni', 'pokazatelji', 'jedinice', 'oglasna'],
      },
      en: {
        title: 'Widgets: overview and attention',
        summary:
          'Widgets that say what matters today: the key figures, what is waiting for someone, what is running out, what is new and how the units divide.',
        steps: [
          'Key figures. Five figures about the company in one place, so you can see how things stand in half a minute. How it is counted: Cost this month is the company\'s whole cost from the 1st of the month to today (approved hours at each person\'s rate, manual pay, issued material with a price, general costs, housing, vehicles and tools), with the change against the SAME days of last month; only an account with the full finance right sees it. Active employees are those with the status Active. Open work items are all that are not Closed or Cancelled (Resolved ones count), and "overdue" are those whose deadline is before today. Vehicles and tools available are those with the status Available, out of the total. Everything narrows to the chosen business unit.',
          'Needs attention. One list of everything that is waiting for someone, so nothing is missed: each kind has a number and leads to the page where it is dealt with. How it is counted: Only what the role can act on is shown. Kinds: leave requests to answer; hours to review (shifts with the status Submitted); vehicle costs to review; article orders waiting (Requested); refund requests (Requested); late invoices (issued, past due, not paid in full; only with the right to the Book); late costs from the Book (unpaid, past due); DKV rows that need a look; postings the worker has not confirmed (starting within 7 days or running, active worker); vehicle dates running out (registration, inspection, insurance, service within 30 days or past; one row per vehicle, vehicles out of service are left out); workers\' certificates running out (30 days); documents running out (30 days); records to tidy; and, for an administrator only, the virus scanner not answering. Each number carries "since yesterday". With a business unit chosen only its items are counted; the virus scanner, the records to tidy and DKV rows with no vehicle (unknown card) belong to the company as a whole. On the phone this is "Still waiting" and leaves out the first three kinds, which the Today card already lists.',
          'Documents expiring soon. So that no document (ID, permit, certificate, insurance) runs out unnoticed. How it is counted: Documents with an expiry date within the next 30 days or already past, nearest first (up to 8). Red is 7 days or fewer, or past; days are counted on the organisation\'s calendar. With a business unit chosen: the documents of its people, sites, vehicles and tools. When there is nothing, the card is one line.',
          'Notifications and bulletin board. What is new for you: notifications you have not opened and the company\'s latest posts. How it is counted: The number is your own unread notifications (personal, not tied to a business unit). Below are the latest 5 posts of the bulletin board, which are for the whole company.',
          'Business units. So an owner with several units can see how large each one is. How it is counted: Active units as a tree: who heads each, and how many employees (currently employed in it) and sites it has, counting the units under it. The bars are against the largest. With no units it is one line with a link to the company structure.',
        ],
        example:
          'A document runs out in 12 days and three leave requests are waiting: "Needs attention" lists them, and a press on the row opens the list where they are dealt with.',
        tips: [
          'The numbers narrow to the business unit chosen in the header; "since yesterday" compares with what your account saw yesterday.',
          '"Today", "this month" and hours are counted in the organisation\'s time zone, which is set in the company settings.',
        ],
        keywords: ['overview', 'figures', 'attention', 'widget', 'documents', 'key', 'units', 'bulletin'],
      },
    },
  },
  {
    id: 'widgeti-ljudi',
    group: 'work',
    path: paths.home,
    audience: staff,
    content: {
      sr: {
        title: 'Widgeti: ljudi i rad',
        summary:
          'Widgeti o radnicima: ko je na terenu, koliko je odrađeno, ko je odsutan i šta traže.',
        steps: [
          'Prisutnost danas. Ko je sada na terenu i koliko je danas odrađeno. Kako se računa: Prsten i „na terenu” su radnici s otvorenom smjenom (prijavljeni, nisu odjavljeni), od aktivnih radnika; ako je prijavljenih više nego aktivnih, imenilac je broj prijavljenih. „Sati danas” su završene smjene koje se preklapaju s današnjim danom organizacije (trajanje minus pauza) plus koliko traju smjene koje još traju. „Čekaju odobrenje” su ti završeni sati u statusu Poslano. Lista su do 8 najranije prijavljenih. „Danas” je dan u vremenskoj zoni organizacije, od njene ponoći do sljedeće.',
          'Radna snaga. Raspoloživa radna snaga u jednom pogledu: ko radi, ko čeka odluku o odsustvu, ko je odsutan. Kako se računa: Veliki broj su radnici s otvorenom smjenom, „od N aktivnih” su radnici sa statusom Aktivan. Traka: Na gradilištu (prijavljeni), Zahtjevi na čekanju (zahtjevi za odsustvo u statusu Zahtjev) i Na odsustvu danas (odobrena odsustva čiji period obuhvata današnji dan organizacije). Trake po projektima su broj prijavljenih na svakom (šest najvećih). Ispod su prva najviše 5 zahtjeva za odsustvo na čekanju. Sa izabranom poslovnom jedinicom broji se samo ona.',
          'Zahtjevi za odsustvo. Odgovorite na zahtjeve za odsustvo bez odlaska na drugu stranicu. Kako se računa: Zahtjevi u statusu Zahtjev, s tipom, periodom i brojem dana; kvačica odobrava, krstić odbija (traži razlog). Odgovara samo administrator i nikad na vlastiti zahtjev. Isti odgovor odmah spušta brojeve u meniju i u „Zahtijeva pažnju”.',
          'Narudžbe. Zahtjevi radnika za artikle (odjeća, obuća, šljemovi, alat) koji još nisu u njihovim rukama. Kako se računa: Tri broja su brojevi koje daje server po koraku: Zatraženo, Naručeno, U dostavi (ne računa se Isporučeno, Odbijeno ni Otkazano). Ispod je 5 najnovijih otvorenih narudžbi sa sljedećim korakom na jedan pritisak. „Hitno” je oznaka koju je postavio onaj ko traži. Uz Zatraženo stoji „od jučer”. Sa izabranom poslovnom jedinicom: narudžbe njenih gradilišta.',
          'Sedmični izvještaji. Šta čeka ured: izvještaji za pregled, vraćeni na korekciju i gradilišta koja kasne. Kako se računa: Tri broja su brojevi koje daje server: Čeka pregled (status Poslato), Potrebna korekcija (vraćeni poslovođi, čeka se novi) i Kasni. Gradilište kasni ako je prošle sedmice (ponedjeljak do nedjelje) imalo raspoređene radnike, a do ponedjeljka u 12:00 po vremenu organizacije nema izvještaja; izvještaj vraćen na korekciju se računa kao poslat. Lista prvo pokazuje gradilišta koja kasne (s poslovođom), zatim najranije poslate izvještaje. Pregled i odobravanje su na stranici Sedmični izvještaji.',
        ],
        example:
          'Osam radnika je prijavljeno, a dva sata čekaju odobrenje: „Prisutnost danas” pokazuje prsten 8 od 16, „7h 30min danas” i „2 čekaju odobrenje”.',
        tips: [
          'Brojevi se sužavaju na poslovnu jedinicu izabranu u zaglavlju; „od jučer” se poredi s onim što je Vaš nalog vidio jučer.',
          '„Danas”, „ovaj mjesec” i sati računaju se po vremenskoj zoni organizacije, koja se podešava u Podešavanjima firme.',
        ],
        keywords: ['prisutnost', 'radna snaga', 'odsustvo', 'narudžbe', 'sati', 'radnici', 'smjena', 'zahtjevi'],
      },
      en: {
        title: 'Widgets: people and work',
        summary:
          'Widgets about the workers: who is on site, how much has been worked, who is away and what they are asking for.',
        steps: [
          'Attendance today. Who is on site now and how much has been worked today. How it is counted: The ring and "on site" are the workers with an open shift (clocked in, not out), out of the active workers; if more are clocked in than are active, the denominator is the number clocked in. "Hours today" are the finished shifts that overlap today on the organisation\'s calendar (duration less break) plus how long the shifts still running have run. "Waiting for approval" are those finished hours with the status Submitted. The list is up to 8 who clocked in earliest. "Today" is the day in the organisation\'s time zone, from its midnight to the next.',
          'Workforce. The available workforce in one look: who is working, who is waiting for a leave decision, who is away. How it is counted: The large number is the workers with an open shift, "of N active" the workers with the status Active. The bar: On site (clocked in), Requests waiting (leave requests with the status Requested) and On leave today (approved leave whose period covers the organisation\'s today). The bars per project are the number clocked in on each (the six largest). Below are the first 5 leave requests waiting. With a business unit chosen only it is counted.',
          'Leave requests. Answer leave requests without going to another page. How it is counted: Requests with the status Requested, with the type, period and number of days; the tick approves, the cross rejects (a reason is asked for). Only an administrator answers, and never their own request. The same answer drops the numbers in the menu and in "Needs attention" at once.',
          'Orders. Workers\' requests for articles (clothing, footwear, helmets, tools) that are not yet in their hands. How it is counted: The three numbers are the server\'s own counts per step: Requested, Ordered, In delivery (Delivered, Rejected and Cancelled are not counted). Below are the 5 newest open orders with the next step one press away. "Urgent" is a mark set by whoever asks. Requested carries "since yesterday". With a business unit chosen: the orders of its sites.',
          'Weekly reports. What waits for the office: reports to review, reports sent back and sites that are late. How it is counted: The three numbers are the server\'s own counts: Waiting for review (status Submitted), Needs correction (sent back to the foreman, a new one is awaited) and Late. A site is late if it had workers posted to it last week (Monday to Sunday) and has no report by Monday 12:00 organisation time; a report sent back for correction counts as sent. The list shows the late sites first (with the foreman), then the oldest reports sent. Review and approval are on the Weekly reports page.',
        ],
        example:
          'Eight workers are clocked in and two sets of hours are waiting for approval: "Attendance today" shows the ring 8 of 16, "7h 30m today" and "2 waiting for approval".',
        tips: [
          'The numbers narrow to the business unit chosen in the header; "since yesterday" compares with what your account saw yesterday.',
          '"Today", "this month" and hours are counted in the organisation\'s time zone, which is set in the company settings.',
        ],
        keywords: ['attendance', 'workforce', 'leave', 'orders', 'hours', 'workers', 'shift', 'requests'],
      },
    },
  },
  {
    id: 'widgeti-projekti',
    group: 'work',
    path: paths.home,
    audience: staff,
    content: {
      sr: {
        title: 'Widgeti: projekti i oprema',
        summary:
          'Widgeti o gradilištima i opremi: gdje se radi, koliko je naplaćeno, ko prelazi budžet i šta je slobodno.',
        steps: [
          'Vozila i alati. Šta je slobodno, šta izdato, a šta ne radi. Kako se računa: Dvije trake, za vozila i za alate: broj po statusu (vozila: Slobodno, Zaduženo, Na servisu, Van upotrebe, Iznajmljeno; alati: Slobodan, Zadužen, Na popravci, Izgubljen, Otpisan, Iznajmljen), kao brojevi koje daje server, pa flota veća od sto stavki ne staje na sto. Ispod su po imenu vozila Van upotrebe i alati Na popravci ili Izgubljeni. Sa izabranom poslovnom jedinicom: njena vozila i alati.',
          'Aktivni projekti. Gdje se radi i šta traži pogled: projekti bez ekipe i oni kojima ističe rok. Kako se računa: Oznake su broj projekata po fazi (Planiran, Aktivan, Na čekanju, Završen, Otkazan; prikazuju se faze koje imaju projekata), brojevi koje daje server. Lista su svi Aktivni projekti: prvo oni bez radnika („Bez ekipe”), zatim po roku; oznake „Završava za N dana” (14 dana ili manje) i „Rok je prošao” računaju se po kalendaru organizacije. Sa izabranom poslovnom jedinicom: njeni projekti.',
          'Projekti — realizacija. Koliko je od ugovorenog naplaćeno i na šta ove godine odlazi novac. Kako se računa: Do 5 projekata s ugovorenom vrijednošću, po udjelu realizacije: zbir uplata naručioca do danas podijeljen vrijednošću ugovora (traka stane na 100%, a veći udio je zelen i vidi se u broju). Ispod: sastav troška cijele firme od 1. januara (rad, ručna plaćanja, materijal, opšti troškovi, smještaj, vozila, alati) i realizovan prihod po mjesecima ove godine, zbir uplata po projektima. Samo uz puno finansijsko pravo.',
          'Projekti van budžeta. Upozorenje prije nego što projekat pređe budžet. Kako se računa: Projekti koji nisu Završeni ni Otkazani, a imaju budžet ili vrijednost ugovora (mjeri se prema onom što je izabrano na projektu, a zadano je budžet). Potrošeno je ukupan trošak projekta od početka do danas podijeljen granicom. Upozorenje je od praga koji je zadan na projektu (zadano 80%), a „prekoračen” od 100%. Ne prati period s vrha stranice. Kad nema projekata s granicom ili su svi ispod nje, kartica je jedan red.',
          'Projekt u fokusu. Jedan projekat pod povećalom. Kako se računa: Projekat birate u postavkama kartice. Za izabrani period i „od početka”: prihod (uplate), rashod (troškovi projekta), zarada i marža; udio budžeta koji je potrošen (rashod od početka / budžet) i udio ugovora koji je naplaćen (prihod od početka / vrijednost ugovora). Samo uz puno finansijsko pravo.',
          'Mapa uživo. Gdje su ljudi u ovom trenutku. Kako se računa: Posljednja poznata lokacija svakog aktivnog radnika koji prijavljuje lokaciju; broj je koliko ih je na mapi. Sa izabranom poslovnom jedinicom: radnici koje ona zapošljava. Potreban je Google Maps ključ.',
        ],
        example:
          'Projekat s budžetom 28.000 EUR potrošio je 25.200: u „Projektima van budžeta” je na 90% i žut, a na 100% postaje crven „prekoračen”.',
        tips: [
          'Brojevi se sužavaju na poslovnu jedinicu izabranu u zaglavlju; „od jučer” se poredi s onim što je Vaš nalog vidio jučer.',
          '„Danas”, „ovaj mjesec” i sati računaju se po vremenskoj zoni organizacije, koja se podešava u Podešavanjima firme.',
        ],
        keywords: ['projekti', 'realizacija', 'budžet', 'vozila', 'alati', 'mapa', 'gradilište', 'fokus'],
      },
      en: {
        title: 'Widgets: projects and equipment',
        summary:
          'Widgets about sites and equipment: where work is going on, how much has been collected, who is going over budget and what is free.',
        steps: [
          'Vehicles and tools. What is free, what is issued and what is not working. How it is counted: Two bars, for vehicles and for tools: the number per status (vehicles: Available, Assigned, In service, Out of service, Rented out; tools: Available, Assigned, Under repair, Lost, Retired, Rented out), as the server\'s own counts, so a fleet of more than a hundred is not cut at a hundred. Below are, by name, the vehicles Out of service and the tools Under repair or Lost. With a business unit chosen: its vehicles and tools.',
          'Active projects. Where work is going on and what needs a look: projects with nobody on them and those whose deadline is near. How it is counted: The tags are the number of projects per stage (Planned, Active, On hold, Completed, Cancelled; the stages that have projects are shown), the server\'s own counts. The list is all the Active projects: first those with no workers ("No crew"), then by deadline; the marks "Ends in N days" (14 days or fewer) and "Past its end date" are counted on the organisation\'s calendar. With a business unit chosen: its projects.',
          'Projects — realization. How much of what was contracted has been collected, and where the money goes this year. How it is counted: Up to 5 projects with a contract value, by share realized: the sum of the customer\'s payments to date divided by the contract value (the bar stops at 100%, a larger share is green and shows in the number). Below: the make-up of the whole company\'s cost since 1 January (labour, manual pay, material, general costs, housing, vehicles, tools) and the realized income per month this year, the sum of payments per project. Only with the full finance right.',
          'Projects over budget. A warning before a project goes over its budget. How it is counted: Projects that are not Completed or Cancelled and have a budget or a contract value (measured against what is chosen on the project; the default is the budget). Spent is the project\'s whole cost from its start to today divided by the limit. The warning starts at the threshold set on the project (default 80%), and "over" is from 100%. It does not follow the period at the top of the page. With no project with a limit, or all below it, the card is one line.',
          'Project in focus. One project under the magnifying glass. How it is counted: You choose the project in the card\'s settings. For the chosen period and "since the start": income (payments), spending (the project\'s costs), profit and margin; the share of the budget spent (spending since the start / budget) and the share of the contract collected (income since the start / contract value). Only with the full finance right.',
          'Live map. Where people are at this moment. How it is counted: The last known location of every active worker who reports one; the number is how many are on the map. With a business unit chosen: the workers it employs. A Google Maps key is needed.',
        ],
        example:
          'A project with a budget of 28,000 EUR has spent 25,200: in "Projects over budget" it is at 90% and amber, and at 100% it turns red "over".',
        tips: [
          'The numbers narrow to the business unit chosen in the header; "since yesterday" compares with what your account saw yesterday.',
          '"Today", "this month" and hours are counted in the organisation\'s time zone, which is set in the company settings.',
        ],
        keywords: ['projects', 'realization', 'budget', 'vehicles', 'tools', 'map', 'site', 'focus'],
      },
    },
  },
  {
    id: 'widgeti-novac',
    group: 'work',
    path: paths.home,
    audience: moneyView,
    content: {
      sr: {
        title: 'Widgeti: novac',
        summary:
          'Widgeti o novcu firme: prihod, rashod, zarada i njihova raspodjela. Vidi ih samo nalog s finansijskim pravom (nalog s pravom na statistiku vidi postotke).',
        steps: [
          'Finansije. Novac firme na jednom mjestu, u tri pogleda. Kako se računa: Pregled: prihod je zbir uplata po projektima i ostalih prihoda firme u periodu; rashod je ukupan trošak firme u periodu (isti trošak koji je u Ključnim pokazateljima); zarada je prihod minus rashod; marža je zarada kao dio prihoda. Uz svaki iznos je promjena u odnosu na isti broj dana neposredno prije perioda. Trend: „Po mjesecima” su zadnjih 6 mjeseci (broj je ovaj mjesec do danas, s promjenom prema istim danima prošlog mjeseca); „U periodu” su stubići po danima, sedmicama ili mjesecima prema dužini perioda, uz prosjek (ukupno podijeljeno brojem stubića) i najviši. Struktura: raspodjela rashoda po vrstama (rad, ručna plaćanja, materijal, opšti troškovi, smještaj, vozila, alati), za cijelu firmu ili jedan projekat (postavke). Nalog koji smije vidjeti samo postotke dobija promjene i udjele u postotku, bez iznosa. Period se bira na vrhu stranice.',
          'Prihod i rashod. Da vidite jesu li mjeseci, sedmice ili dani u plusu. Kako se računa: Par stubića po danu, sedmici ili mjesecu izabranog perioda: prihod (uplate po projektima i ostali prihodi) i rashod (ukupan trošak firme tog dana). Ispod je zarada za cijeli period, zbir stubića. Samo uz puno finansijsko pravo.',
          'Najveći troškovi po projektu. Gdje novac najviše odlazi. Kako se računa: Projekti koji su u izabranom periodu najviše koštali (prvih 5 ili 10, postavka kartice), najveći prvi; svaka traka je u odnosu na najveći. Prikazuju se projekti s troškom većim od nule. Samo uz puno finansijsko pravo.',
          'Zarada po projektu. Koji projekti zarađuju, a koji gube. Kako se računa: Tabela po projektu za izabrani period: prihod, rashod, zarada i marža, sortirana po zaradi od najvećeg gubitka; svaka kolona se može sortirati. Redovi „Nisu prikazani projekti” i „Bez projekta” (trošak i prihod koji ne pripadaju nijednom projektu) i ukupno firme sabiraju se tačno u zbir. Samo uz puno finansijsko pravo.',
        ],
        example:
          'Prihod 66.420,54, rashod 6.968,84: zarada je 59.451,70 i marža 89,5% (zarada kao dio prihoda). Isti rashod je u Ključnim pokazateljima i u „Prihodu i rashodu”.',
        tips: [
          'Brojevi se sužavaju na poslovnu jedinicu izabranu u zaglavlju; „od jučer” se poredi s onim što je Vaš nalog vidio jučer.',
          '„Danas”, „ovaj mjesec” i sati računaju se po vremenskoj zoni organizacije, koja se podešava u Podešavanjima firme.',
        ],
        keywords: ['finansije', 'prihod', 'rashod', 'zarada', 'marža', 'trend', 'struktura', 'novac'],
      },
      en: {
        title: 'Widgets: money',
        summary:
          'Widgets about the company\'s money: income, spending, profit and how they divide. Only an account with the finance right sees them (an account with the statistics right sees percentages).',
        steps: [
          'Finance. The company\'s money in one place, in three views. How it is counted: Overview: income is the sum of payments per project and the company\'s other income in the period; spending is the company\'s whole cost in the period (the same cost as in Key figures); profit is income less spending; margin is profit as a share of income. Each amount carries the change against the same number of days just before the period. Trend: "By month" is the last 6 months (the number is this month so far, with the change against the same days of last month); "In the period" is bars per day, week or month depending on the period\'s length, with the average (total divided by the number of bars) and the highest. Structure: how spending divides by kind (labour, manual pay, material, general costs, housing, vehicles, tools), for the whole company or one project (settings). An account that may see only percentages gets the changes and shares in percent, with no amounts. The period is chosen at the top of the page.',
          'Income vs spending. To see whether months, weeks or days are in the black. How it is counted: A pair of bars per day, week or month of the chosen period: income (payments per project and other income) and spending (the company\'s whole cost for that day). Below is the profit for the whole period, the sum of the bars. Only with the full finance right.',
          'Top projects by spending. Where the money goes the most. How it is counted: The projects that cost the most in the chosen period (the top 5 or 10, a setting of the card), largest first; each bar is against the largest. Projects with a cost above zero are shown. Only with the full finance right.',
          'Profit by project. Which projects earn and which lose. How it is counted: A table per project for the chosen period: income, spending, profit and margin, sorted by profit from the biggest loss; every column can be sorted. The rows "Projects not shown" and "No project" (cost and income that belong to no project) and the company\'s total add up exactly. Only with the full finance right.',
        ],
        example:
          'Income 66,420.54, spending 6,968.84: profit is 59,451.70 and the margin 89.5% (profit as a share of income). The same spending is in Key figures and in "Income vs spending".',
        tips: [
          'The numbers narrow to the business unit chosen in the header; "since yesterday" compares with what your account saw yesterday.',
          '"Today", "this month" and hours are counted in the organisation\'s time zone, which is set in the company settings.',
        ],
        keywords: ['finance', 'income', 'spending', 'profit', 'margin', 'trend', 'structure', 'money'],
      },
    },
  },
]

export function helpTopicsFor(user: User | null | undefined): HelpTopic[] {
  return helpTopics.filter((topic) => topic.audience(user))
}

export function helpLocale(locale: string): HelpLocale {
  return locale === 'en' ? 'en' : 'sr'
}

export function helpTopicById(id: string | undefined): HelpTopic | undefined {
  return helpTopics.find((topic) => topic.id === id)
}

/** Whether every word typed appears in the topic's title, summary, steps or keywords. */
export function helpMatches(content: HelpContent, query: string): boolean {
  const words = query
    .trim()
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean)

  if (words.length === 0) return true

  const text = [content.title, content.summary, ...content.steps, ...content.tips, ...content.keywords]
    .join(' ')
    .toLowerCase()

  return words.every((word) => text.includes(word))
}

import type { User } from '../../api/types';
import {
  canAdministerAccounts,
  canViewBook,
  canViewDirectory,
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
          'Ili idite na Dokumenti (Administracija), kliknite Otpremi i prvo izaberite zapis. Na toj stranici datoteku možete ispustiti bilo gdje; dodavanje se otvara s datotekom već unutra.',
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
          'Or go to Documents (Administration), click Upload and choose the record first. On that page you can drop a file anywhere; the upload opens with the file already in it.',
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
          'Otvorite Dokumenti u izborniku Administracija. Zadano se vide dokumenti koji ističu u sljedećih 30 dana; promijenite period ili izaberite Sve.',
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
          'Open Documents in the Administration menu. By default you see documents expiring in the next 30 days; change the period or choose All.',
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
          'Otvorite Zahtjevi za dokumente u izborniku Administracija i kliknite Zatraži dokument. Izaberite jednu ili više osoba i šta se traži; paket Novi radnik traži uobičajeni set odjednom. Po želji dodajte rok.',
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
          'Open Document requests in the Administration menu and click Request a document. Choose one or more people and what is needed; the New worker package asks for the usual set at once. Add a deadline if you like.',
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
          'A locked month cannot be deleted until it is unlocked.',
        ],
        keywords: ['settlement', 'lock', 'payout', 'advance', 'accountant', 'zip', 'export', 'flat rate', 'hourly', 'margin'],
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

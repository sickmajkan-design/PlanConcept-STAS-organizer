# Admin panel conventions

The rules the admin panel follows, so a new screen behaves like the existing
ones. If a screen has to break one, say why in the code.

## Navigation

- The menu shows one entry per job, not per table. Cost ledgers (vehicle, tool,
  general, stock, housing costs, finance) live under **Cost records** and move
  between each other with a tab strip. The housing register itself (places, who
  lives in them, contracts, import) sits in the **Directory** group; the housing
  costs tab only holds the charges. Pay rates, public holidays and the annual
  plan live under **Billing settings**.
- The URLs of the tabbed pages did not change. `/cost-records` and
  `/billing-settings` redirect to the first tab. A nav item can be marked
  `inTabs` (hidden from the drawer, still searchable and in the breadcrumb) and
  `alsoActiveOn` (keeps a hub highlighted on its tabs) in
  `layout/navConfig.tsx`.
- **Ctrl+K** searches pages and records, and offers "create" actions gated by
  the same role checks as the destination (`layout/paletteActions.tsx`). An
  action that opens a dialog links to `?new=1` (or `?compose=1`); the page
  handles it with `useOpenOnParam`.

## Things that wait on a decision

Anything a person has to review follows the same four steps. Time entries,
time off, vehicle costs and unassigned defects already do; add the same to
anything new.

1. The record starts **pending**.
2. The people who can decide get a **notification** (bell, push, mobile), never
   the person who caused it (`VehicleExpenseReviewNotifier` is the pattern).
3. The menu shows a **red count** of what is waiting, on the group, the hub and
   the tab (`layout/useNavBadgeCounts.ts`).
4. Approving needs a confirmation; rejecting needs a **reason**
   (`components/ReasonDialog.tsx`), and the recorder is notified with that
   reason. Rejecting or approving twice needs an explicit confirm.

Nobody approves their own entry, except a Super Admin. Changing a record after
a decision puts it back to pending and notifies the reviewers again.

A decision can be taken back: "Back to pending" on a decided vehicle cost
(`POST /vehicle-expenses/{id}/reopen`) returns it to the queue. It follows the
same who-may-act rules as reviewing and needs a confirmation, not a reason.
It is a real server operation, not a toast that pretends to undo. "Waiting on
me" for time off means undecided requests plus changes the employee proposed
to approved leave; the menu badge counts the same set (`waitingOnReviewer`).

## Lists

- No page numbers. A list shows "Showing N of M" and a **Show more** button
  that asks the API for a bigger page (20, 50, 100; the API accepts at most
  100). Past 100 rows the user narrows the search or filter.
- Every column is sortable on the server, and the sort picker on phone cards
  offers every sortable column.
- Below 600 px a list is drawn as cards from the same column definitions
  (`components/ResourceCardList.tsx`), so a column added to the table appears
  on the card. The first column is the card title, the `actions` column sits
  along the bottom.
- Lists that are naturally about status (time off, vehicle costs) also offer a
  **Board** view, one column per status (`components/StatusBoard.tsx`). The
  choice is remembered per person in the browser. A card is not dragged
  between columns: moving it on needs an approval or a reason, so it happens
  through the buttons on the card.

## Dialogs

- A confirm dialog whose action can fail stays open and shows why
  (`ConfirmDialog` awaits a promise-returning `onConfirm`). Never fire and
  forget a mutation from a dialog button.
- One dialog per intent: rejecting always uses `ReasonDialog`.

## Storage and sessions

- Anything remembered in the browser is scoped to the signed-in account
  (`hooks/userScopedStorage.ts`); a role change narrows what is shown rather
  than wiping it.
- The admin session ends when the browser closes and after 30 minutes without
  activity in any tab.

## Verifying a change

Type-check with `tsc -b` (a plain `tsc` checks nothing here). Check phone
layouts at 390 px and at the Android emulator's 320 px: Chrome on the emulator
can be driven by forwarding its DevTools socket
(`adb forward tcp:9222 localabstract:chrome_devtools_remote`) and connecting
Playwright with `connectOverCDP`.

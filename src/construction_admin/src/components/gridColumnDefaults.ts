/**
 * The columns a person came to a list for, by list. On a narrower screen only these are shown at first; the
 * rest are behind the "Columns" button, and on a wide screen (1500 px and up) everyone sees all of them.
 * A person's own choice, once made, wins over this.
 *
 * Change what counts as important here, in one place. A list that is not named here has no Columns button.
 */
export const ESSENTIAL_COLUMNS: Record<string, readonly string[]> = {
  employees: ['employeeNumber', 'fullName', 'position', 'status', 'currentProjectNames'],
  vehicles: ['brand', 'registrationNumber', 'registrationValidUntil', 'status', 'assignedEmployeeName'],
  invoices: ['number', 'projectName', 'companies', 'amount', 'remainingAmount', 'dueDate', 'status'],
};

/** From this viewport width on, every column is shown unless the person chose otherwise. */
export const ALL_COLUMNS_FROM_PX = 1500;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// What a computed cell holds, whether a person typed over an automatic figure
/// (<see cref="IsOverride"/>), and whether the value is a stored one at all
/// (<see cref="IsTyped"/>) — a typed value in a column with nothing automatic to
/// override is typed but not an override.
/// </summary>
public readonly record struct LedgerComputedValue(decimal Value, bool IsOverride, bool IsTyped = false);

/// <summary>
/// Works out every computed column of one row.
/// </summary>
/// <remarks>
/// <para>
/// Columns may refer to other computed columns (pay uses hours, margin uses pay),
/// so each is evaluated on demand and remembered. A reference that loops back
/// on itself counts as zero rather than hanging — the templates never build
/// one, and <see cref="HasCycle"/> lets a test prove that.
/// </para>
/// <para>
/// A value typed into a computed cell wins over the calculation and is reported
/// as an override, so it can be shown and counted instead of silently replacing
/// the formula — which is how a spreadsheet's hand-typed totals go unnoticed.
/// </para>
/// </remarks>
public sealed class LedgerCalculator
{
    private readonly IReadOnlyDictionary<Guid, LedgerFormula> _formulas;

    public LedgerCalculator(IEnumerable<(Guid ColumnId, LedgerFormula? Formula)> columns)
    {
        _formulas = columns
            .Where(c => c.Formula is not null)
            .ToDictionary(c => c.ColumnId, c => c.Formula!);
    }

    public bool HasFormulas => _formulas.Count > 0;

    public bool IsComputed(Guid columnId) => _formulas.ContainsKey(columnId);

    /// <summary>
    /// The computed columns of a row, given what is stored in its cells (typed
    /// values, sourced values and overrides alike), keyed by column id.
    /// </summary>
    /// <param name="stored">What the row's cells hold.</param>
    /// <param name="sourced">
    /// What the system knows for this row, by column. A column with a source that
    /// is missing from here has no automatic figure for this row — a subcontractor
    /// with no employee record, say — so a value typed into it is ordinary input,
    /// not an override of anything.
    /// </param>
    public IReadOnlyDictionary<Guid, LedgerComputedValue> ComputeRow(
        IReadOnlyDictionary<Guid, string?> stored,
        IReadOnlyDictionary<Guid, decimal>? sourced = null)
    {
        var memo = new Dictionary<Guid, LedgerComputedValue>();
        var inProgress = new HashSet<Guid>();

        LedgerComputedValue Resolve(Guid columnId)
        {
            if (memo.TryGetValue(columnId, out var known))
            {
                return known;
            }

            stored.TryGetValue(columnId, out var raw);

            if (!_formulas.TryGetValue(columnId, out var formula))
            {
                return new LedgerComputedValue(LedgerCellMath.ParseNumeric(raw), false);
            }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                // Typed over an automatic figure is an override; typed into a
                // sourced column the row has no automatic figure for is just a value.
                var overrides = formula.Source is null || (sourced?.ContainsKey(columnId) ?? false);

                return memo[columnId] = new LedgerComputedValue(LedgerCellMath.ParseNumeric(raw), overrides, IsTyped: true);
            }

            if (formula.Source is not null)
            {
                return memo[columnId] = new LedgerComputedValue(
                    sourced is not null && sourced.TryGetValue(columnId, out var auto) ? auto : 0m,
                    false);
            }

            if (!inProgress.Add(columnId))
            {
                return new LedgerComputedValue(0m, false);
            }

            var total = 0m;

            if (formula.Product.Count > 0)
            {
                total = formula.Product.Aggregate(1m, (acc, id) => acc * Resolve(id).Value);
            }

            foreach (var term in formula.Terms)
            {
                total += term.Sign * Resolve(term.ColumnId).Value;
            }

            inProgress.Remove(columnId);

            return memo[columnId] = new LedgerComputedValue(total, false);
        }

        return _formulas.Keys.ToDictionary(id => id, Resolve);
    }

    /// <summary>True when some computed column depends on itself, directly or through others.</summary>
    public bool HasCycle()
    {
        var state = new Dictionary<Guid, int>(); // 1 = visiting, 2 = done

        bool Visit(Guid id)
        {
            if (!_formulas.TryGetValue(id, out var formula))
            {
                return false;
            }

            if (state.TryGetValue(id, out var s))
            {
                return s == 1;
            }

            state[id] = 1;

            var cyclic = formula.ReferencedColumns.Any(Visit);

            state[id] = 2;

            return cyclic;
        }

        return _formulas.Keys.Any(Visit);
    }
}

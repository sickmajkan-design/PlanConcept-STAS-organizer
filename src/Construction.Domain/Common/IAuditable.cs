namespace Construction.Domain.Common;

/// <summary>
/// Marks an entity whose changes are recorded in the audit trail.
/// </summary>
/// <remarks>
/// <para>
/// Opt-in rather than opt-out, deliberately. Auditing everything would record
/// a row for every GPS ping — roughly a million a month for a hundred-person
/// crew — and another for every notification and outbox message, which would
/// more than double the write volume of the system to record things nobody
/// will ever ask about.
/// </para>
/// <para>
/// The test for whether an entity belongs here: would somebody, in a dispute
/// about pay, a workplace investigation, or an insurance claim, need to know
/// who changed this and when? That covers people, their postings, their hours
/// and absences, and anything with money attached. It does not cover machine
/// chatter.
/// </para>
/// </remarks>
public interface IAuditable;

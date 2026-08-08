namespace Construction.Domain.Common;

/// <summary>
/// Keeps a property's value out of the audit trail.
/// </summary>
/// <remarks>
/// <para>
/// The audit trail records the before and after of every changed property, so
/// on an audited entity it is a second place a secret can come to rest — one
/// that is deliberately long-lived, and readable by any administrator. A
/// password change would otherwise write both the old and the new hash into a
/// table that outlives the account.
/// </para>
/// <para>
/// The recorder also refuses, unconditionally, any property whose name
/// contains "password", "hash", "token" or "secret". That is the safety net,
/// not the mechanism: it catches a field added later by somebody who did not
/// read this file, and it means forgetting the attribute is not a silent
/// leak. Use the attribute anyway — it states the intent, and the safety net
/// cannot see a secret that happens to be called something else.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotAuditedAttribute : Attribute;

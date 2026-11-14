using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Where the Super Admins are told about one kind of server trouble, and when they last were. One row per kind
/// (<see cref="Key"/>), created the first time somebody changes it or a notice goes out; a kind with no row uses its default.
/// </summary>
public class ServerAlertSetting : BaseEntity
{
    /// <summary>The kind, e.g. <c>disk</c> or <c>backupOld</c> (see <c>ServerAlertKeys</c>).</summary>
    public string Key { get; set; } = null!;

    public bool EmailEnabled { get; set; }

    /// <summary>The in-app notice and the push to the phone, which go together.</summary>
    public bool PushEnabled { get; set; }

    public DateTime? LastSentAt { get; set; }
}

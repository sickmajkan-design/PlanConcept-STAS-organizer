using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Entities;

namespace Construction.UnitTests.Security;

/// <summary>
/// Every <see cref="AttachmentOwnerType"/> touches four independent switches
/// spread across the codebase — <see cref="AttachmentOwner"/>'s own two, the
/// DTO projection, the list query, and the upload existence check — because
/// two of those have to stay LINQ expressions EF Core can translate to SQL,
/// which rules out routing them all through one shared method.
///
/// <see cref="AttachmentOwner"/>'s switches are exhaustive by construction
/// (every branch is written by hand against the enum), so this file uses them
/// as the reference and checks that <see cref="AttachmentMapping"/> agrees —
/// exactly the switch that went stale and threw "Nullable object must have a
/// value" for three real owner types before anyone noticed. This is a fast,
/// no-database net for that one regression; the list query and the upload
/// check still need the database and are covered in
/// <c>AttachmentTests.A_receipt_can_be_filed_against_a_vehicle_rental_rate</c>
/// and its three siblings in the integration suite.
/// </summary>
public class AttachmentOwnerTests
{
    public static IEnumerable<object[]> EveryOwnerType() =>
        Enum.GetValues<AttachmentOwnerType>().Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(EveryOwnerType))]
    public void Every_owner_type_has_a_real_path_segment(AttachmentOwnerType type)
    {
        // "other" is PathSegment's fallback for a value nothing recognises —
        // seeing it back means this type fell through the switch.
        Assert.NotEqual("other", AttachmentOwner.PathSegment(type));
    }

    [Theory]
    [MemberData(nameof(EveryOwnerType))]
    public void Setting_an_owner_and_reading_it_back_roundtrips(AttachmentOwnerType type)
    {
        var id = Guid.NewGuid();
        var attachment = new Attachment();

        AttachmentOwner.Apply(attachment, type, id);
        var (readType, readId) = AttachmentOwner.Of(attachment);

        Assert.Equal(type, readType);
        Assert.Equal(id, readId);
    }

    [Theory]
    [MemberData(nameof(EveryOwnerType))]
    public void The_dto_projection_agrees_with_the_owner_that_was_set(AttachmentOwnerType type)
    {
        // The exact bug this guards: OwnerId's chain used to end in a hard
        // `.Value` on a specific FK, so any type stored past the last branch
        // the chain knew about threw InvalidOperationException here instead
        // of returning a DTO.
        var id = Guid.NewGuid();
        var attachment = new Attachment
        {
            FileName = "file.pdf",
            ContentType = "application/pdf",
            StorageKey = "k",
        };

        AttachmentOwner.Apply(attachment, type, id);
        var dto = AttachmentMapping.ToDto(attachment);

        Assert.Equal(type, dto.OwnerType);
        Assert.Equal(id, dto.OwnerId);
    }
}

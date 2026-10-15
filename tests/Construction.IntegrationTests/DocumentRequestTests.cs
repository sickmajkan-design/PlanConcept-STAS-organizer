using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetMyDocuments;
using Construction.Application.Features.DocumentRequests;
using Construction.Application.Features.DocumentRequests.Commands.CreateDocumentRequests;
using Construction.Application.Features.DocumentRequests.Commands.ReviewDocumentRequest;
using Construction.Application.Features.DocumentRequests.Commands.SubmitDocumentRequest;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequest;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequests;
using Construction.Application.Features.DocumentRequests.Queries.GetMyDocumentRequests;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Asking a person for a document and getting it back: who may ask, who may answer, what the answer
/// is allowed to be, and what each side is told.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DocumentRequestTests : IntegrationTestBase
{
    public DocumentRequestTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private static SubmitDocumentRequestCommand Answer(Guid id, string fileName = "nalaz.pdf", string content = "nalaz")
    {
        var bytes = TestFiles.WithSignature(fileName, content);

        return new SubmitDocumentRequestCommand
        {
            Id = id,
            Files = [new SubmittedFile(fileName, bytes.Length, new MemoryStream(bytes))]
        };
    }

    private sealed record World(Employee Employee, User Worker, User Admin, Employee Other, User OtherWorker);

    private async Task<World> SeedAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));
        var otherWorker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, other.Id));

        return new World(employee, worker, admin, other, otherWorker);
    }

    private async Task<DocumentRequestDto> AskAsync(
        World w,
        AttachmentCategory category = AttachmentCategory.MedicalCheck,
        string title = "Ljekarski pregled")
    {
        var result = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CreateDocumentRequestsCommand
            {
                EmployeeIds = [w.Employee.Id],
                Items = [new DocumentRequestItem { Category = category, Title = title, Note = "do kraja mjeseca" }]
            });
        });

        return Assert.Single(result.Created);
    }

    private Task<DocumentRequestDto> SubmitAsync(World w, Guid id, string fileName = "nalaz.pdf") =>
        InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(Answer(id, fileName));
        });

    // ---- asking ----------------------------------------------------------

    [Fact]
    public async Task The_office_asks_for_a_package_and_each_document_is_its_own_request()
    {
        var w = await SeedAsync();

        var result = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CreateDocumentRequestsCommand
            {
                EmployeeIds = [w.Employee.Id, w.Other.Id],
                Items =
                [
                    new DocumentRequestItem { Category = AttachmentCategory.Identity, Title = "Licna karta" },
                    new DocumentRequestItem { Category = AttachmentCategory.Licence, Title = "Vozacka dozvola" },
                    new DocumentRequestItem { Category = AttachmentCategory.Diploma, Title = "Diploma" }
                ]
            });
        });

        Assert.Equal(6, result.Created.Count);
        Assert.All(result.Created, r => Assert.Equal(DocumentRequestStatus.Open, r.Status));
        Assert.Equal(0, result.Skipped);

        // The same ask again leaves what is already waiting alone.
        var again = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CreateDocumentRequestsCommand
            {
                EmployeeIds = [w.Employee.Id],
                Items = [new DocumentRequestItem { Category = AttachmentCategory.Identity, Title = "licna karta" }]
            });
        });

        Assert.Empty(again.Created);
        Assert.Equal(1, again.Skipped);
    }

    [Fact]
    public async Task The_person_is_told_without_being_told_what_for()
    {
        var w = await SeedAsync();

        await AskAsync(w, AttachmentCategory.MedicalCheck, "Ljekarski pregled medicine rada");

        var notices = await InScope(scope => scope.Db.Notifications.AsNoTracking()
            .Where(n => n.UserId == w.Worker.Id && n.Type == NotificationType.DocumentRequested)
            .ToListAsync());

        var notice = Assert.Single(notices);
        Assert.DoesNotContain("Ljekarski", notice.Title + notice.Body + notice.DataJson);
        Assert.DoesNotContain("pregled", notice.Title + notice.Body + notice.DataJson);
    }

    [Theory]
    [InlineData(UserRole.Worker)]
    [InlineData(UserRole.Foreman)]
    public async Task Only_the_office_may_ask(UserRole role)
    {
        var w = await SeedAsync();
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var user = await InScope(scope => TestData.SeedUserAsync(scope, role, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new CreateDocumentRequestsCommand
            {
                EmployeeIds = [w.Employee.Id],
                Items = [new DocumentRequestItem { Category = AttachmentCategory.Identity, Title = "Pasos" }]
            });
        }));
    }

    [Fact]
    public async Task A_person_cannot_be_flooded_with_requests()
    {
        var w = await SeedAsync();

        var items = Enumerable.Range(1, DocumentRequestRules.MaxItemsPerCall)
            .Select(i => new DocumentRequestItem { Category = AttachmentCategory.Other, Title = $"Dokument {i}" })
            .ToList();

        await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CreateDocumentRequestsCommand { EmployeeIds = [w.Employee.Id], Items = items });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CreateDocumentRequestsCommand
            {
                EmployeeIds = [w.Employee.Id],
                Items = Enumerable.Range(11, 6)
                    .Select(i => new DocumentRequestItem { Category = AttachmentCategory.Other, Title = $"Dokument {i}" })
                    .ToList()
            });
        }));
    }

    // ---- seeing ----------------------------------------------------------

    [Fact]
    public async Task A_person_sees_only_their_own_requests_and_not_who_asked()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        var mine = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetMyDocumentRequestsQuery());
        });

        var seen = Assert.Single(mine);
        Assert.Equal(asked.Id, seen.Id);
        Assert.Null(seen.RequestedByName);

        var theirs = await InScope(scope =>
        {
            ActAs(scope, w.OtherWorker, w.Other.Id);
            return scope.Send(new GetMyDocumentRequestsQuery());
        });

        Assert.Empty(theirs);

        // By id: not found, which does not confirm that the id is real.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, w.OtherWorker, w.Other.Id);
            return scope.Send(new GetDocumentRequestQuery(asked.Id));
        }));
    }

    [Fact]
    public async Task Only_the_office_lists_everyone_s_requests()
    {
        var w = await SeedAsync();
        await AskAsync(w);

        var page = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new GetDocumentRequestsQuery { EmployeeId = w.Employee.Id });
        });

        Assert.Single(page.Items);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetDocumentRequestsQuery());
        }));
    }

    // ---- answering -------------------------------------------------------

    [Fact]
    public async Task An_answer_waits_for_the_office_and_is_not_shared_yet()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        var sent = await SubmitAsync(w, asked.Id);

        Assert.Equal(DocumentRequestStatus.Submitted, sent.Status);
        Assert.Equal(1, sent.Attempts);
        var file = Assert.Single(sent.Files);

        // The person can open what they sent, but it is not among their shared documents.
        await InScope(async scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            var content = await scope.Send(new GetAttachmentContentQuery(file.Id));
            await content.Content.DisposeAsync();
        });

        var mine = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        Assert.Empty(mine.Documents);

        // A colleague cannot open it.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, w.OtherWorker, w.Other.Id);
            return scope.Send(new GetAttachmentContentQuery(file.Id));
        }));

        // The office was told, and not what it was about.
        var notices = await InScope(scope => scope.Db.Notifications.AsNoTracking()
            .Where(n => n.UserId == w.Admin.Id && n.Type == NotificationType.DocumentSubmitted)
            .ToListAsync());

        Assert.Single(notices);

        // Answered once; a second answer while it waits is refused.
        await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync(w, asked.Id));
    }

    [Fact]
    public async Task Only_the_person_it_was_asked_of_can_answer()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, w.OtherWorker, w.Other.Id);
            return scope.Send(Answer(asked.Id));
        }));

        // The office cannot answer on their behalf either: it is not their request.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(Answer(asked.Id));
        }));
    }

    [Theory]
    [InlineData("tablica.xlsx")]
    [InlineData("skripta.exe")]
    [InlineData("nalaz.txt")]
    public async Task Only_photographs_and_pdfs_are_accepted_from_the_phone(string fileName)
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(Answer(asked.Id, fileName));
        }));

        var after = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new GetDocumentRequestQuery(asked.Id));
        });

        Assert.Equal(DocumentRequestStatus.Open, after.Status);
    }

    [Fact]
    public async Task A_file_that_is_not_what_its_name_says_is_refused()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new SubmitDocumentRequestCommand
            {
                Id = asked.Id,
                Files = [new SubmittedFile("nalaz.pdf", 20, new MemoryStream("ovo nije pdf, samo tekst"u8.ToArray()))]
            });
        }));
    }

    // ---- deciding --------------------------------------------------------

    [Fact]
    public async Task A_returned_document_comes_back_with_its_reason_and_can_be_sent_again()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);
        await SubmitAsync(w, asked.Id);

        var returned = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new ReturnDocumentRequestCommand { Id = asked.Id, Reason = "Slika je mutna, slikaj ponovo" });
        });

        Assert.Equal(DocumentRequestStatus.Returned, returned.Status);

        // The person is told, but the reason is only inside the app.
        var notices = await InScope(scope => scope.Db.Notifications.AsNoTracking()
            .Where(n => n.UserId == w.Worker.Id && n.Type == NotificationType.DocumentReturned)
            .ToListAsync());
        var notice = Assert.Single(notices);
        Assert.DoesNotContain("mutna", notice.Title + notice.Body + notice.DataJson);

        var asSeenByPerson = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetDocumentRequestQuery(asked.Id));
        });

        Assert.Equal("Slika je mutna, slikaj ponovo", asSeenByPerson.ReturnReason);
        Assert.Contains(asSeenByPerson.Events, e => e.Type == DocumentRequestEventType.Returned && !e.ByEmployee);
        Assert.All(asSeenByPerson.Events, e => Assert.Null(e.ActorName));

        var second = await SubmitAsync(w, asked.Id);

        Assert.Equal(DocumentRequestStatus.Submitted, second.Status);
        Assert.Equal(2, second.Attempts);
        Assert.Null(second.ReturnReason);
    }

    [Fact]
    public async Task A_reason_is_required_to_send_a_document_back()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);
        await SubmitAsync(w, asked.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new ReturnDocumentRequestCommand { Id = asked.Id, Reason = " " });
        }));
    }

    [Fact]
    public async Task Accepting_puts_the_document_on_the_record_and_shares_it_with_the_person()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w, AttachmentCategory.Licence, "Vozacka dozvola");
        var sent = await SubmitAsync(w, asked.Id);

        // A document that lapses needs the date before it is accepted.
        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new AcceptDocumentRequestCommand { Id = asked.Id });
        }));

        var accepted = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new AcceptDocumentRequestCommand
            {
                Id = asked.Id,
                ExpiresAt = new DateOnly(2030, 5, 1)
            });
        });

        Assert.Equal(DocumentRequestStatus.Accepted, accepted.Status);

        var mine = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        var document = Assert.Single(mine.Documents);
        Assert.Equal(sent.Files.Single().Id, document.Id);
        Assert.Equal(AttachmentCategory.Licence, document.Category);
        Assert.Equal(new DateOnly(2030, 5, 1), document.ExpiresAt);

        // Closed: it cannot be answered again, returned or withdrawn.
        await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync(w, asked.Id));
        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CancelDocumentRequestCommand(asked.Id));
        }));
    }

    [Fact]
    public async Task A_withdrawn_request_cannot_be_answered_and_the_person_still_sees_it()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new CancelDocumentRequestCommand(asked.Id));
        });

        await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync(w, asked.Id));

        var mine = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new GetMyDocumentRequestsQuery());
        });

        Assert.Equal(DocumentRequestStatus.Cancelled, Assert.Single(mine).Status);
    }

    [Fact]
    public async Task A_person_cannot_decide_on_their_own_request()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w, AttachmentCategory.Diploma, "Diploma");
        await SubmitAsync(w, asked.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new AcceptDocumentRequestCommand { Id = asked.Id });
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(new ReturnDocumentRequestCommand { Id = asked.Id, Reason = "Moj dokument je dobar" });
        }));
    }

    [Fact]
    public async Task Too_many_attempts_send_the_person_to_the_office()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        for (var attempt = 1; attempt <= DocumentRequestRules.MaxAttempts; attempt++)
        {
            await SubmitAsync(w, asked.Id);
            await InScope(scope =>
            {
                ActAs(scope, w.Admin);
                return scope.Send(new ReturnDocumentRequestCommand { Id = asked.Id, Reason = "Nije citljivo" });
            });
        }

        await Assert.ThrowsAsync<ConflictException>(() => SubmitAsync(w, asked.Id));
    }

    // ---- the driving licence warning -------------------------------------

    [Fact]
    public async Task Taking_a_vehicle_warns_when_the_driving_licence_is_missing_or_lapsed_but_never_refuses()
    {
        var w = await SeedAsync();

        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        // No licence on the record: a warning, and the vehicle is theirs anyway.
        var first = await InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            return scope.Send(
                new Construction.Application.Features.Vehicles.Commands.SelfCheckOutVehicle.SelfCheckOutVehicleCommand(vehicle.Id));
        });

        Assert.Equal("Missing", first.DriverLicenceWarning);
        Assert.Equal(w.Employee.Id, first.AssignedEmployeeId);

        // A licence that was only sent, not accepted, does not count.
        var asked = await AskAsync(w, AttachmentCategory.Licence, "Vozacka dozvola");
        await SubmitAsync(w, asked.Id);

        var second = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var pending = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(
                new Construction.Application.Features.Vehicles.Commands.AssignVehicle.AssignVehicleCommand(
                    second.Id, w.Employee.Id));
        });

        Assert.Equal("Missing", pending.DriverLicenceWarning);

        // Accepted with a date in the past: lapsed.
        await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new AcceptDocumentRequestCommand { Id = asked.Id, ExpiresAt = new DateOnly(2020, 1, 1) });
        });

        var third = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var lapsed = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(
                new Construction.Application.Features.Vehicles.Commands.AssignVehicle.AssignVehicleCommand(
                    third.Id, w.Employee.Id));
        });

        Assert.Equal("Expired", lapsed.DriverLicenceWarning);
    }

    // ---- the virus scan --------------------------------------------------

    [Fact]
    public async Task A_file_the_scan_recognises_is_refused_and_the_request_stays_open()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, w.Worker, w.Employee.Id);
            var bytes = TestFiles.WithSignature("nalaz.pdf", FakeVirusScanner.Eicar);

            return scope.Send(new SubmitDocumentRequestCommand
            {
                Id = asked.Id,
                Files = [new SubmittedFile("nalaz.pdf", bytes.Length, new MemoryStream(bytes))]
            });
        }));

        var after = await InScope(scope =>
        {
            ActAs(scope, w.Admin);
            return scope.Send(new GetDocumentRequestQuery(asked.Id));
        });

        Assert.Equal(DocumentRequestStatus.Open, after.Status);
        Assert.Equal(0, after.Attempts);

        // Nothing was stored for it.
        var stored = await InScope(scope => scope.Db.Attachments.AsNoTracking()
            .CountAsync(a => a.EmployeeId == w.Employee.Id));
        Assert.Equal(0, stored);
    }

    [Fact]
    public async Task When_the_scan_cannot_run_nothing_is_accepted_and_the_person_is_told_to_try_again()
    {
        var w = await SeedAsync();
        var asked = await AskAsync(w);

        var scanner = await InScope(scope => Task.FromResult(scope.VirusScanner));
        scanner.Down = true;

        try
        {
            await Assert.ThrowsAsync<ServiceUnavailableException>(() => SubmitAsync(w, asked.Id));
        }
        finally
        {
            scanner.Down = false;
        }

        // Once it is back, the same request goes through.
        var sent = await SubmitAsync(w, asked.Id);
        Assert.Equal(DocumentRequestStatus.Submitted, sent.Status);
    }
}

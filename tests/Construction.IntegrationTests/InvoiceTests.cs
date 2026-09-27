using Construction.Application.Common.Exceptions;
using Construction.Application.Features.CustomerCompanies;
using Construction.Application.Features.Invoices.Commands.ChangeInvoiceStatus;
using Construction.Application.Features.Invoices.Commands.CreateInvoice;
using Construction.Application.Features.Invoices.Models;
using Construction.Application.Features.Invoices.Queries.GetInvoices;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Invoices the firm issued to its clients, recorded on one or more of the client's companies:
/// the parts always add up to the whole, a number is issued once, and it is money, so only
/// management with the finance grant works with them.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class InvoiceTests : IntegrationTestBase
{
    public InvoiceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private sealed record Setup(User SuperAdmin, Customer Customer, Project Project, IReadOnlyList<CustomerCompanyDto> Companies);

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    /// <summary>A client with the given number of companies and a site of theirs.</summary>
    private async Task<Setup> SetupAsync(int companies)
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var customer = await InScope(scope => TestData.SeedCustomerAsync(scope));

        var project = await InScope(async scope =>
        {
            var p = await TestData.SeedProjectAsync(scope);
            p.CustomerId = customer.Id;
            await scope.Db.SaveChangesAsync();

            return p;
        });

        var created = new List<CustomerCompanyDto>();

        for (var i = 1; i <= companies; i++)
        {
            created.Add(await InScope(scope =>
            {
                ActAs(scope, superAdmin);
                return scope.Send(new CreateCustomerCompanyCommand { CustomerId = customer.Id, Name = $"Firma {i} {Unique()}" });
            }));
        }

        return new Setup(superAdmin, customer, project, created);
    }

    private Task<InvoiceDto> CreateAsync(Setup setup, Func<CreateInvoiceCommand, CreateInvoiceCommand>? tweak = null, User? as_ = null)
    {
        var command = new CreateInvoiceCommand
        {
            ProjectId = setup.Project.Id,
            Number = $"R-{Unique()}",
            IssueDate = new DateOnly(2026, 9, 20),
            Amount = 1000m,
        };

        if (tweak is not null)
        {
            command = tweak(command);
        }

        return InScope(scope =>
        {
            ActAs(scope, as_ ?? setup.SuperAdmin);
            return scope.Send(command);
        });
    }

    [Fact]
    public async Task A_client_with_no_companies_is_invoiced_as_a_whole()
    {
        var setup = await SetupAsync(companies: 0);

        var invoice = await CreateAsync(setup);

        var share = Assert.Single(invoice.Shares);
        Assert.Null(share.CustomerCompanyId);
        Assert.Equal(1000m, share.Amount);
        Assert.Equal(2026, invoice.PayrollYear);
        Assert.Equal(9, invoice.PayrollMonth);          // defaults to the month it was issued
        Assert.Equal("Issued", invoice.Status);
    }

    [Fact]
    public async Task An_invoice_can_go_to_one_company()
    {
        var setup = await SetupAsync(companies: 2);

        var invoice = await CreateAsync(setup, c => c with { CompanyIds = [setup.Companies[1].Id] });

        var share = Assert.Single(invoice.Shares);
        Assert.Equal(setup.Companies[1].Id, share.CustomerCompanyId);
        Assert.Equal(1000m, share.Amount);
    }

    [Fact]
    public async Task An_invoice_split_evenly_adds_up_to_the_cent()
    {
        var setup = await SetupAsync(companies: 3);

        var invoice = await CreateAsync(setup, c => c with
        {
            Amount = 100m,
            CompanyIds = setup.Companies.Select(x => x.Id).ToList(),
        });

        Assert.Equal(3, invoice.Shares.Count);
        Assert.Equal(100m, invoice.Shares.Sum(s => s.Amount));
        Assert.Equal([33.33m, 33.33m, 33.34m], invoice.Shares.Select(s => s.Amount).Order().ToArray());
    }

    [Fact]
    public async Task Explicit_parts_must_add_up_to_the_invoice()
    {
        var setup = await SetupAsync(companies: 2);

        var wrong = await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(setup, c => c with
        {
            Amount = 1000m,
            Shares =
            [
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[0].Id, Amount = 600m },
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[1].Id, Amount = 300m },
            ],
        }));
        Assert.Contains("100.00", wrong.Message);            // the difference is stated

        var right = await CreateAsync(setup, c => c with
        {
            Amount = 1000m,
            Shares =
            [
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[0].Id, Amount = 600m },
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[1].Id, Amount = 400m },
            ],
        });
        Assert.Equal(2, right.Shares.Count);
    }

    [Fact]
    public async Task A_client_with_companies_needs_the_invoice_to_name_them_and_only_its_own_and_active_ones()
    {
        var setup = await SetupAsync(companies: 2);
        var stranger = await SetupAsync(companies: 1);

        // Names none.
        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(setup));

        // Names somebody else's company.
        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(setup, c => c with { CompanyIds = [stranger.Companies[0].Id] }));

        // The same company twice.
        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(setup, c => c with
        {
            Shares =
            [
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[0].Id, Amount = 500m },
                new InvoiceShareInput { CustomerCompanyId = setup.Companies[0].Id, Amount = 500m },
            ],
        }));

        // A company switched off is no longer offered for new invoices.
        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new UpdateCustomerCompanyCommand
            {
                Id = setup.Companies[0].Id, Name = setup.Companies[0].Name, IsActive = false,
            });
        });
        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(setup, c => c with { CompanyIds = [setup.Companies[0].Id] }));
    }

    [Fact]
    public async Task A_client_without_companies_cannot_be_given_one_from_elsewhere()
    {
        var setup = await SetupAsync(companies: 0);
        var other = await SetupAsync(companies: 1);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(setup, c => c with { CompanyIds = [other.Companies[0].Id] }));
    }

    [Fact]
    public async Task A_number_is_issued_once_even_when_the_first_was_cancelled()
    {
        var setup = await SetupAsync(companies: 0);
        var first = await CreateAsync(setup);

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(setup, c => c with { Number = first.Number }));

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CancelInvoiceCommand { Id = first.Id, Reason = "Pogresan iznos" });
        });

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(setup, c => c with { Number = first.Number }));
    }

    [Fact]
    public async Task A_credit_note_is_an_invoice_with_a_negative_amount_and_zero_is_refused()
    {
        var setup = await SetupAsync(companies: 0);

        var credit = await CreateAsync(setup, c => c with { Amount = -250m });
        Assert.Equal(-250m, credit.Amount);
        Assert.Equal(-250m, Assert.Single(credit.Shares).Amount);

        await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(setup, c => c with { Amount = 0m }));
    }

    [Fact]
    public async Task The_payroll_month_may_differ_from_the_month_it_was_issued()
    {
        var setup = await SetupAsync(companies: 0);

        var invoice = await CreateAsync(setup, c => c with { PayrollYear = 2026, PayrollMonth = 8 });

        Assert.Equal(8, invoice.PayrollMonth);
        Assert.Equal(new DateOnly(2026, 9, 20), invoice.IssueDate);
    }

    [Fact]
    public async Task An_issued_invoice_can_be_marked_paid_and_a_paid_one_is_not_cancelled()
    {
        var setup = await SetupAsync(companies: 0);
        var invoice = await CreateAsync(setup);

        var paid = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new MarkInvoicePaidCommand(invoice.Id));
        });
        Assert.Equal("Paid", paid.Status);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CancelInvoiceCommand { Id = invoice.Id, Reason = "Predomislio sam se" });
        }));
    }

    [Fact]
    public async Task Cancelling_needs_a_reason_and_keeps_the_invoice_on_record()
    {
        var setup = await SetupAsync(companies: 0);
        var invoice = await CreateAsync(setup);

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CancelInvoiceCommand { Id = invoice.Id, Reason = "" });
        }));

        var cancelled = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CancelInvoiceCommand { Id = invoice.Id, Reason = "Izdat pogresnom klijentu" });
        });

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Izdat pogresnom klijentu", cancelled.CancelReason);

        var listed = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetInvoicesQuery { ProjectId = setup.Project.Id, PageSize = 50 });
        });
        Assert.Contains(listed.Items, i => i.Id == invoice.Id && i.Status == "Cancelled");
    }

    [Fact]
    public async Task The_list_filters_by_company_payroll_month_and_status()
    {
        var setup = await SetupAsync(companies: 2);

        var forFirst = await CreateAsync(setup, c => c with { CompanyIds = [setup.Companies[0].Id], PayrollYear = 2026, PayrollMonth = 9 });
        var forSecond = await CreateAsync(setup, c => c with { CompanyIds = [setup.Companies[1].Id], PayrollYear = 2026, PayrollMonth = 10 });
        var forBoth = await CreateAsync(setup, c => c with
        {
            CompanyIds = setup.Companies.Select(x => x.Id).ToList(), PayrollYear = 2026, PayrollMonth = 9,
        });

        Task<IReadOnlyCollection<InvoiceDto>> List(GetInvoicesQuery query) => InScope(async scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return (await scope.Send(query with { ProjectId = setup.Project.Id, PageSize = 50 })).Items;
        });

        var ofFirst = await List(new GetInvoicesQuery { CustomerCompanyId = setup.Companies[0].Id });
        Assert.Equal(new[] { forFirst.Id, forBoth.Id }.Order(), ofFirst.Select(i => i.Id).Order());

        var inOctober = await List(new GetInvoicesQuery { PayrollYear = 2026, PayrollMonth = 10 });
        Assert.Equal(forSecond.Id, Assert.Single(inOctober).Id);

        var byNumber = await List(new GetInvoicesQuery { Search = forBoth.Number });
        Assert.Equal(forBoth.Id, Assert.Single(byNumber).Id);
    }

    [Fact]
    public async Task Money_needs_management_and_the_finance_grant_and_the_super_admin_always_has_it()
    {
        var setup = await SetupAsync(companies: 0);

        foreach (var role in new[] { UserRole.ProjectManager, UserRole.Foreman, UserRole.Worker })
        {
            var user = await InScope(scope => TestData.SeedUserAsync(scope, role));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => CreateAsync(setup, as_: user));
        }

        // An Admin without the grant still may not.
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => CreateAsync(setup, as_: admin));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetInvoicesQuery());
        }));

        // With the grant they may.
        await InScope(scope => scope.Db.Users
            .Where(u => u.Id == admin.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.FinanceAccess, FinanceAccess.Full)));

        Assert.Equal("Issued", (await CreateAsync(setup, as_: admin)).Status);
    }

    [Fact]
    public async Task A_company_with_invoices_reports_how_many_and_can_only_be_switched_off()
    {
        var setup = await SetupAsync(companies: 1);
        await CreateAsync(setup, c => c with { CompanyIds = [setup.Companies[0].Id] });

        var companies = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetCustomerCompaniesQuery(setup.Customer.Id));
        });

        Assert.Equal(1, Assert.Single(companies).InvoiceCount);

        // There is no delete for a company at all: it is switched off, so its invoices keep a name.
        var off = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new UpdateCustomerCompanyCommand
            {
                Id = setup.Companies[0].Id, Name = setup.Companies[0].Name, IsActive = false,
            });
        });
        Assert.False(off.IsActive);
        Assert.Equal(1, off.InvoiceCount);
    }

    [Fact]
    public async Task A_client_cannot_have_two_companies_of_the_same_name_and_only_management_manages_them()
    {
        var setup = await SetupAsync(companies: 1);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CreateCustomerCompanyCommand { CustomerId = setup.Customer.Id, Name = setup.Companies[0].Name });
        }));

        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new CreateCustomerCompanyCommand { CustomerId = setup.Customer.Id, Name = "Nova firma" });
        }));
    }
}

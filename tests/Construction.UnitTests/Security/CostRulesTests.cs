using Construction.Application.Features.Costs;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Security;

public class CostRulesTests
{
    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    public void Pricing_a_job_needs_to_see_what_labour_costs(UserRole role)
    {
        Assert.True(CostRules.CanSeeLabourCost(role));
    }

    [Fact]
    public void A_foreman_does_not_see_what_labour_costs()
    {
        // Deliberately tighter than the ForemanAndAbove hierarchy the rest of
        // the system uses: a rate is effectively somebody's pay, and this
        // would put every colleague's earnings on a site phone.
        Assert.False(CostRules.CanSeeLabourCost(UserRole.Foreman));
    }

    [Fact]
    public void A_worker_and_a_roleless_token_see_nothing_at_all()
    {
        foreach (UserRole? role in new UserRole?[] { UserRole.Worker, null })
        {
            Assert.False(CostRules.CanSeeLabourCost(role));
            Assert.False(CostRules.CanSeeSpending(role));
            Assert.False(CostRules.CanRecordSpending(role));
            Assert.False(CostRules.CanSetLabourRate(role));
            Assert.False(CostRules.CanDeleteSpending(role));
        }
    }

    [Fact]
    public void Setting_a_rate_is_narrower_than_reading_one()
    {
        Assert.False(CostRules.CanSetLabourRate(UserRole.ProjectManager));
        Assert.True(CostRules.CanSeeLabourCost(UserRole.ProjectManager));
    }

    [Fact]
    public void A_foreman_records_spending_and_sees_what_their_site_used()
    {
        // The wide one on purpose: the person who signed for the delivery is
        // the one who knows it arrived. Figures nobody records are worthless.
        Assert.True(CostRules.CanRecordSpending(UserRole.Foreman));
        Assert.True(CostRules.CanSeeSpending(UserRole.Foreman));
    }

    [Fact]
    public void Removing_a_recorded_amount_is_narrower_than_recording_one()
    {
        // Entering a wrong figure is an ordinary mistake; quietly removing one
        // is how a total stops matching the paperwork behind it.
        Assert.True(CostRules.CanRecordSpending(UserRole.Foreman));
        Assert.False(CostRules.CanDeleteSpending(UserRole.Foreman));
    }

    [Fact]
    public void Nobody_may_delete_who_may_not_record()
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (CostRules.CanDeleteSpending(role))
            {
                Assert.True(CostRules.CanRecordSpending(role));
            }
        }
    }

    [Fact]
    public void Seeing_a_rate_implies_seeing_spending()
    {
        // The report puts both halves on one screen. A role that could read
        // the labour column but not the material one would get a total it
        // could not account for.
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (CostRules.CanSeeLabourCost(role))
            {
                Assert.True(CostRules.CanSeeSpending(role));
            }
        }
    }
}

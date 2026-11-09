using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using AwesomeAssertions;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Api.Core.Errors;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Commands;
using OcharoMarketing.Domain.Core.Entities;
using OcharoMarketing.Infrastructure.Inquiries.CommandHandlers;
using OcharoMarketing.Persistence.Inquiries;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;
using Xunit;
namespace OcharoMarketing.Tests.Integration.Inquiries;
public sealed class InquiryFollowUpTests:IAsyncLifetime
{
    private readonly MarketingDatabase database=new();
    private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-09-29T12:00:00Z");
    public Task InitializeAsync()=>database.InitializeAsync();
    public Task DisposeAsync()=>database.DisposeAsync();
    private static InquiryService Service(IInquiryRepository repo)=>new(repo,new Products(),new FakeTimeProvider(Now));
    private static InquiryInput Input()=>
        new(
            Guid.NewGuid().ToString(),
            "Review reader",
            "email",
            "reader@example.test",
            "Sizing question",
            "/catalogue/",
            null,
            true);
    [Fact]
    public async Task Update_ShouldKeepTheFollowUpInUtcWithAppendOnlyActorHistory()
    {
        string id;
        await using(var db=database.NewContext())
        {
            var repo=new InquiryRepository(db);
            var service=Service(repo);
            id=(await service.SubmitAsync(Input(),default)).Id;
            var first=await service.UpdateAsync(
                id,
                new(
                    1,
                    "contacted",
                    "Measurements requested",
                    new("Atelier",DateTimeOffset.Parse("2026-09-30T17:00:00+05:00"),"Confirm sleeve length")),
                default,
                "member-123");
            first.FollowUpAt!.Value.Offset.Should().Be(TimeSpan.Zero);
            await service.UpdateAsync(
                id,
                new(2,"qualified","Measurements received",new("Atelier",Now.AddDays(2),"Prepare response")),
                default,
                "member-456");
        }
        await using(var db=database.NewContext())
        {
            var service=Service(new InquiryRepository(db));
            var record=((await service.ListAsync(null,null,null,0,20,default)).Items).Should().ContainSingle().Which;
            record.Revision.Should().Be(3);record.AssignedTo.Should().Be("Atelier");
            var history=await service.ActivityAsync(id,0,1,default);history.Total.Should().Be(2);
            history.Items.Should().ContainSingle().Which.Actor.Should().Be("member-456");
            var older=((await service.ActivityAsync(id,1,1,default)).Items).Should().ContainSingle().Which;
            older.Actor.Should().Be("member-123");
            older.Note.Should().Be("Measurements requested");
            older.Revision.Should().Be(2);
        }
    }
    [Fact]
    public async Task Update_ShouldKeepAnOmittedFollowUpAndClearAnExplicitlyEmptyOne()
    {
        await using var db=database.NewContext();
        var service=Service(new InquiryRepository(db));
        var id=(await service.SubmitAsync(Input(),default)).Id;
        await service.UpdateAsync(id,new(1,"new",null,new("Owner",Now.AddHours(1),"Reply")),default);
        var legacy=await service.UpdateAsync(id,new(2,"contacted","Called"),default);
        legacy.AssignedTo.Should().Be("Owner");legacy.FollowUpAt.Should().Be(Now.AddHours(1));
        var closed=await service.UpdateAsync(id,new(3,"closed","Done",new(null,null,null)),default);
        closed.FollowUpAt.Should().BeNull();closed.AssignedTo.Should().BeNull();closed.Status.Should().Be("closed");
    }
    [Fact]
    public async Task Update_ShouldKeepTheRevisionAndAddNoHistory_WhenNothingChangesOrItIsStale()
    {
        await using var db=database.NewContext();
        var service=Service(new InquiryRepository(db));
        var id=(await service.SubmitAsync(Input(),default)).Id;
        var unchanged=await service.UpdateAsync(id,new(1,"new",null),default);unchanged.Revision.Should().Be(1);
        ((await service.ActivityAsync(id,0,10,default)).Items).Should().BeEmpty();
        await service.UpdateAsync(id,new(1,"contacted","Called"),default);
        (
            ErrorStatus
                .Of(
                    (
                        await FluentActions
                            .Awaiting(()=>service.UpdateAsync(id,new(1,"closed","Stale"),default))
                            .Should()
                            .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(409);
        ((await service.ActivityAsync(id,0,10,default)).Items).Should().ContainSingle();
    }
    [Fact]
    public async Task Update_ShouldRollBackTheState_WhenTheActivityInsertFails()
    {
        await using var db=database.NewContext();
        var repo=new InquiryRepository(db);
        var service=Service(repo);
        var id=(await service.SubmitAsync(Input(),default)).Id;
        var collision=new MetadataRecord{Id=$"inquiry-activity:{id}:0000000002",Json="{}"};
        db.Metadata.Add(collision);
        await db.SaveChangesAsync();
        db.Entry(collision).State=EntityState.Detached;
        await FluentActions
            .Awaiting(()=>service.UpdateAsync(id,new(1,"contacted","Never saved"),default))
            .Should()
            .ThrowExactlyAsync<DbUpdateException>();
        var preserved=await repo.FindAsync(id,default);
        preserved!.Revision.Should().Be(1);
        preserved.Status.Should().Be("new");
        preserved.Note.Should().BeNull();
    }
    [Fact]
    public async Task List_ShouldExcludeClosedAndSortDueDatesWithoutAssumingASchedule()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));
        var overdue=(await service.SubmitAsync(Input() with{Name="Overdue"},default)).Id;
        var future=(await service.SubmitAsync(Input() with{Name="Future"},default)).Id;
        await service.SubmitAsync(Input() with{Name="Unassigned"},default);
        await service.UpdateAsync(overdue,new(1,"qualified",null,new("Person",Now.AddHours(-1),"Call")),default);
        await service.UpdateAsync(future,new(1,"new",null,new("Person",Now.AddHours(1),"Message")),default);
        var due=await service.ListAsync(null,null,"due",0,20,default);
        due.ObservedAt.Should().Be(Now);
        due.Items.Should().ContainSingle().Which.Id.Should().Be(overdue);
        ((await service.ListAsync(null,null,"scheduled",0,20,default)).Total).Should().Be(2);
        (((await service.ListAsync(null,null,"unassigned",0,20,default)).Items).Should().ContainSingle().Which.Name)
            .Should()
            .Be("Unassigned");
        var filtered=await service.ListAsync("qualified","PERSON",null,0,20,default);
        filtered.Items.Should().ContainSingle().Which.Id.Should().Be(overdue);
    }
    [Fact]
    public async Task List_ShouldPageStablySearchNotesAndSkipAuditRows()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));
        for(var i=0;i<3;i++)await service.SubmitAsync(Input() with{Name=$"Reader {i}"},default);
        var first=await service.ListAsync(null,null,null,0,2,default);
        var second=await service.ListAsync(null,null,null,2,2,default);
        first.Total.Should().Be(3);first.Items.Count.Should().Be(2);second.Items.Should().ContainSingle();
        first.Items.Select(x=>x.Id).Should().NotContain(second.Items[0].Id);
        await service.UpdateAsync(first.Items[0].Id,new(1,"new","Measuring tape"),default);
        (((await service.ListAsync(null,"MEASURING",null,0,20,default)).Items).Should().ContainSingle().Which.Id)
            .Should()
            .Be(first.Items[0].Id);
        ((await service.ListAsync(null,null,null,0,20,default)).Total).Should().Be(3);
    }
    [Theory]
    [InlineData("", "Reply", "new")]
    [InlineData("Owner", "", "new")]
    [InlineData("Owner", "Reply", "closed")]
    public async Task Update_ShouldRefuseAScheduledFollowUp_WhenOwnerNextStepOrOpenStatusIsMissing(
        string owner,
        string step,
        string status)
    {
        await using var db=database.NewContext();
        var service=Service(new InquiryRepository(db));
        var id=(await service.SubmitAsync(Input(),default)).Id;
        await FluentActions
            .Awaiting(()=>service.UpdateAsync(id,new(1,status,null,new(owner,Now,step)),default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        ((await service.ActivityAsync(id,0,10,default)).Items).Should().BeEmpty();
    }
    [Fact]
    public async Task List_ShouldDefaultNewFieldsWithoutInventedHistory_WhenARecordIsLegacy()
    {
        await using var db=database.NewContext();
        db.Metadata.Add(
            new MetadataRecord
            {
                Id="inquiry:legacy",
                Json=
                    """{"id":"legacy","fingerprint":"hash","name":"Reader","contactMethod":"email","contac"""
                        + """t":"reader@example.test","message":"Question","sourcePath":"/catalogue/","productId":"""
                        + """null,"consent":true,"createdAt":"2026-09-29T12:00:00Z","status":"new","revision":"""
                        + """1,"note":"Earlier note"}"""
            });
        await db.SaveChangesAsync();
        var service=Service(new InquiryRepository(db));
        var legacy=((await service.ListAsync(null,null,null,0,20,default)).Items).Should().ContainSingle().Which;
        legacy.AssignedTo.Should().BeNull();legacy.FollowUpAt.Should().BeNull();legacy.NextStep.Should().BeNull();
        ((await service.ActivityAsync("legacy",0,20,default)).Items).Should().BeEmpty();
        legacy.Note.Should().Be("Earlier note");
    }
    [Fact]
    public async Task UpdateHandler_ShouldRecordTheSignedInActorAndRefuseAnAnonymousCaller()
    {
        await using var db=database.NewContext();
        var service=Service(new InquiryRepository(db));
        var id=(await service.SubmitAsync(Input(),default)).Id;
        var handler=new InquiryUpdateCommandHandler(service);var http=new DefaultHttpContext();
        var refused=await handler.HandleAsync(
            new InquiryUpdateCommand{Id=id,Input=new(1,"contacted",null),Actor=MarketingCaller.UserId(http)},
            default);
        new MarketingErrorHttpStatusCodeMapper()
            .ToStatusCode(refused.Should().BeOfType<AppResult<InquiryRecord>.Failure>().Subject.Error)
            .Should()
            .Be(401);
        http.Items["Ocharo.UserId"]="trusted-member";
        (
            await handler
                .HandleAsync(
                    new InquiryUpdateCommand{Id=id,Input=new(1,"contacted",null),Actor=MarketingCaller.UserId(http)},
                    default))
                .Should()
                .BeOfType<AppResult<InquiryRecord>.Success>();
        (((await service.ActivityAsync(id,0,10,default)).Items).Should().ContainSingle().Which.Actor)
            .Should()
            .Be("trusted-member");
    }
    [Fact]
    public async Task Activity_ShouldKeepTheCursorStable_WhenAnotherOperatorAppendsBetweenPages()
    {
        await using var db=database.NewContext();
        var service=Service(new InquiryRepository(db));
        var id=(await service.SubmitAsync(Input(),default)).Id;
        for(var revision=1;revision<=5;revision++)
            await service.UpdateAsync(id,new(revision,"new",$"Note {revision}"),default);
        var first=await service.ActivityAsync(id,0,2,default);
        first.Items.Select(x=>x.Revision).Should().Equal(new[]{6,5});
        first.NextBeforeRevision.Should().Be(5);
        await service.UpdateAsync(id,new(6,"contacted","Concurrent new event"),default);
        var second=await service.ActivityAsync(id,0,2,default,first.NextBeforeRevision);
        second.Items.Select(x=>x.Revision).Should().Equal(new[]{4,3});
        second.NextBeforeRevision.Should().Be(3);
        var third=await service.ActivityAsync(id,0,2,default,second.NextBeforeRevision);
        third.Items.Should().ContainSingle().Which.Revision.Should().Be(2);
        third.NextBeforeRevision.Should().BeNull();
        var original=first.Items.Concat(second.Items).Concat(third.Items).Select(x=>x.Revision).ToArray();
        original.Should().Equal(new[]{6,5,4,3,2});
        ((await service.ActivityAsync(id,0,2,default)).Items[0].Revision).Should().Be(7);
        await FluentActions
            .Awaiting(()=>service.ActivityAsync(id,1,2,default,5))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }
    private sealed class Products:IPublishedProductVerifier
    {
        public Task<bool> ExistsAsync(string id,CancellationToken ct)=>Task.FromResult(false);
    }
}

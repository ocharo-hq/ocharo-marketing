using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OcharoMarketing.Api.Marketing.Controllers;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Domain.Marketing.Entities;
using OcharoMarketing.Persistence.Inquiries;
using Xunit;
namespace OcharoMarketing.Tests.Marketing;
public sealed class InquiryFollowUpTests:IAsyncLifetime
{
    private readonly MarketingDatabase database=new();
    private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-09-29T12:00:00Z");
    public Task InitializeAsync()=>database.InitializeAsync();
    public Task DisposeAsync()=>database.DisposeAsync();
    private static InquiryService Service(IInquiryRepository repo)=>new(repo,new Products(),new Clock());
    private static InquiryInput Input()=>new(Guid.NewGuid().ToString(),"Review reader","email","reader@example.test","Sizing question","/catalogue/",null,true);
    [Fact]
    public async Task Follow_up_survives_context_with_utc_and_append_only_actor_history()
    {
        string id;
        await using(var db=database.NewContext())
        {
            var repo=new InquiryRepository(db);var service=Service(repo);id=(await service.SubmitAsync(Input(),default)).Id;
            var first=await service.UpdateAsync(id,new(1,"contacted","Measurements requested",new("Atelier",DateTimeOffset.Parse("2026-09-30T17:00:00+05:00"),"Confirm sleeve length")),default,"member-123");
            Assert.Equal(TimeSpan.Zero,first.FollowUpAt!.Value.Offset);
            await service.UpdateAsync(id,new(2,"qualified","Measurements received",new("Atelier",Now.AddDays(2),"Prepare response")),default,"member-456");
        }
        await using(var db=database.NewContext())
        {
            var service=Service(new InquiryRepository(db));var record=Assert.Single((await service.ListAsync(null,null,null,0,20,default)).Items);
            Assert.Equal(3,record.Revision);Assert.Equal("Atelier",record.AssignedTo);
            var history=await service.ActivityAsync(id,0,1,default);Assert.Equal(2,history.Total);
            Assert.Equal("member-456",Assert.Single(history.Items).Actor);
            var older=Assert.Single((await service.ActivityAsync(id,1,1,default)).Items);
            Assert.Equal("member-123",older.Actor);Assert.Equal("Measurements requested",older.Note);Assert.Equal(2,older.Revision);
        }
    }
    [Fact]
    public async Task Old_clients_preserve_follow_up_and_explicit_empty_plan_clears_it()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));var id=(await service.SubmitAsync(Input(),default)).Id;
        await service.UpdateAsync(id,new(1,"new",null,new("Owner",Now.AddHours(1),"Reply")),default);
        var legacy=await service.UpdateAsync(id,new(2,"contacted","Called"),default);
        Assert.Equal("Owner",legacy.AssignedTo);Assert.Equal(Now.AddHours(1),legacy.FollowUpAt);
        var closed=await service.UpdateAsync(id,new(3,"closed","Done",new(null,null,null)),default);
        Assert.Null(closed.FollowUpAt);Assert.Null(closed.AssignedTo);Assert.Equal("closed",closed.Status);
    }
    [Fact]
    public async Task No_op_keeps_revision_and_stale_change_creates_no_history()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));var id=(await service.SubmitAsync(Input(),default)).Id;
        var unchanged=await service.UpdateAsync(id,new(1,"new",null),default);Assert.Equal(1,unchanged.Revision);
        Assert.Empty((await service.ActivityAsync(id,0,10,default)).Items);
        await service.UpdateAsync(id,new(1,"contacted","Called"),default);
        Assert.Equal(409,(await Assert.ThrowsAsync<MarketingException>(()=>service.UpdateAsync(id,new(1,"closed","Stale"),default))).StatusCode);
        Assert.Single((await service.ActivityAsync(id,0,10,default)).Items);
    }
    [Fact]
    public async Task Activity_insert_failure_rolls_back_current_state()
    {
        await using var db=database.NewContext();var repo=new InquiryRepository(db);var service=Service(repo);var id=(await service.SubmitAsync(Input(),default)).Id;
        var collision=new MetadataRecord{Id=$"inquiry-activity:{id}:0000000002",Json="{}"};db.Metadata.Add(collision);await db.SaveChangesAsync();db.Entry(collision).State=EntityState.Detached;
        await Assert.ThrowsAsync<DbUpdateException>(()=>service.UpdateAsync(id,new(1,"contacted","Never saved"),default));
        var preserved=await repo.FindAsync(id,default);Assert.Equal(1,preserved!.Revision);Assert.Equal("new",preserved.Status);Assert.Null(preserved.Note);
    }
    [Fact]
    public async Task Due_filters_exclude_closed_and_sort_dates_without_assuming_a_schedule()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));
        var overdue=(await service.SubmitAsync(Input() with{Name="Overdue"},default)).Id;
        var future=(await service.SubmitAsync(Input() with{Name="Future"},default)).Id;
        await service.SubmitAsync(Input() with{Name="Unassigned"},default);
        await service.UpdateAsync(overdue,new(1,"qualified",null,new("Person",Now.AddHours(-1),"Call")),default);
        await service.UpdateAsync(future,new(1,"new",null,new("Person",Now.AddHours(1),"Message")),default);
        var due=await service.ListAsync(null,null,"due",0,20,default);Assert.Equal(Now,due.ObservedAt);Assert.Equal(overdue,Assert.Single(due.Items).Id);
        Assert.Equal(2,(await service.ListAsync(null,null,"scheduled",0,20,default)).Total);
        Assert.Equal("Unassigned",Assert.Single((await service.ListAsync(null,null,"unassigned",0,20,default)).Items).Name);
        var filtered=await service.ListAsync("qualified","PERSON",null,0,20,default);Assert.Equal(overdue,Assert.Single(filtered.Items).Id);
    }
    [Fact]
    public async Task Pagination_is_stable_searches_notes_and_never_includes_audit_rows()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));
        for(var i=0;i<3;i++)await service.SubmitAsync(Input() with{Name=$"Reader {i}"},default);
        var first=await service.ListAsync(null,null,null,0,2,default);var second=await service.ListAsync(null,null,null,2,2,default);
        Assert.Equal(3,first.Total);Assert.Equal(2,first.Items.Count);Assert.Single(second.Items);
        Assert.DoesNotContain(second.Items[0].Id,first.Items.Select(x=>x.Id));
        await service.UpdateAsync(first.Items[0].Id,new(1,"new","Measuring tape"),default);
        Assert.Equal(first.Items[0].Id,Assert.Single((await service.ListAsync(null,"MEASURING",null,0,20,default)).Items).Id);
        Assert.Equal(3,(await service.ListAsync(null,null,null,0,20,default)).Total);
    }
    [Theory]
    [InlineData("", "Reply", "new")]
    [InlineData("Owner", "", "new")]
    [InlineData("Owner", "Reply", "closed")]
    public async Task Scheduled_follow_ups_require_explicit_owner_next_step_and_open_status(string owner,string step,string status)
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));var id=(await service.SubmitAsync(Input(),default)).Id;
        await Assert.ThrowsAsync<MarketingException>(()=>service.UpdateAsync(id,new(1,status,null,new(owner,Now,step)),default));
        Assert.Empty((await service.ActivityAsync(id,0,10,default)).Items);
    }
    [Fact]
    public async Task Legacy_serialization_defaults_new_fields_without_invented_history()
    {
        await using var db=database.NewContext();db.Metadata.Add(new MetadataRecord{Id="inquiry:legacy",Json="""{"id":"legacy","fingerprint":"hash","name":"Reader","contactMethod":"email","contact":"reader@example.test","message":"Question","sourcePath":"/catalogue/","productId":null,"consent":true,"createdAt":"2026-09-29T12:00:00Z","status":"new","revision":1,"note":"Earlier note"}"""});await db.SaveChangesAsync();
        var service=Service(new InquiryRepository(db));var legacy=Assert.Single((await service.ListAsync(null,null,null,0,20,default)).Items);
        Assert.Null(legacy.AssignedTo);Assert.Null(legacy.FollowUpAt);Assert.Null(legacy.NextStep);
        Assert.Empty((await service.ActivityAsync("legacy",0,20,default)).Items);Assert.Equal("Earlier note",legacy.Note);
    }
    [Fact]
    public async Task Controller_uses_authenticated_actor_and_rejects_missing_connected_identity()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));var id=(await service.SubmitAsync(Input(),default)).Id;
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["PlatformIntegration:Mode"]="connected"}).Build();
        var controller=new InquiriesController(service,config){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
        Assert.Equal(401,(await Assert.ThrowsAsync<MarketingException>(()=>controller.Update(id,new(1,"contacted",null),default))).StatusCode);
        controller.HttpContext.Items["Ocharo.UserId"]="trusted-member";
        Assert.IsType<OkObjectResult>(await controller.Update(id,new(1,"contacted",null),default));
        Assert.Equal("trusted-member",Assert.Single((await service.ActivityAsync(id,0,10,default)).Items).Actor);
    }
    [Fact]
    public async Task Revision_cursor_stays_stable_when_another_operator_appends_between_pages()
    {
        await using var db=database.NewContext();var service=Service(new InquiryRepository(db));var id=(await service.SubmitAsync(Input(),default)).Id;
        for(var revision=1;revision<=5;revision++)await service.UpdateAsync(id,new(revision,"new",$"Note {revision}"),default);
        var first=await service.ActivityAsync(id,0,2,default);Assert.Equal(new[]{6,5},first.Items.Select(x=>x.Revision));Assert.Equal(5,first.NextBeforeRevision);
        await service.UpdateAsync(id,new(6,"contacted","Concurrent new event"),default);
        var second=await service.ActivityAsync(id,0,2,default,first.NextBeforeRevision);Assert.Equal(new[]{4,3},second.Items.Select(x=>x.Revision));Assert.Equal(3,second.NextBeforeRevision);
        var third=await service.ActivityAsync(id,0,2,default,second.NextBeforeRevision);Assert.Equal(2,Assert.Single(third.Items).Revision);Assert.Null(third.NextBeforeRevision);
        var original=first.Items.Concat(second.Items).Concat(third.Items).Select(x=>x.Revision).ToArray();Assert.Equal(new[]{6,5,4,3,2},original);
        Assert.Equal(7,(await service.ActivityAsync(id,0,2,default)).Items[0].Revision);
        await Assert.ThrowsAsync<MarketingException>(()=>service.ActivityAsync(id,1,2,default,5));
    }
    private sealed class Clock:TimeProvider{public override DateTimeOffset GetUtcNow()=>Now;}
    private sealed class Products:IPublishedProductVerifier{public Task<bool> ExistsAsync(string id,CancellationToken ct)=>Task.FromResult(false);}
}

using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Persistence.Inquiries;
using Xunit;
namespace OcharoMarketing.Tests.Marketing;
public sealed class InquiryWorkflowTests:IAsyncLifetime
{
    private readonly MarketingDatabase database=new();
    public Task InitializeAsync()=>database.InitializeAsync();
    public Task DisposeAsync()=>database.DisposeAsync();
    [Fact]
    public async Task Consented_submission_survives_context_and_replays_once()
    {
        var input=new InquiryInput(Guid.NewGuid().ToString(),"Reader","email","reader@example.test","What are the measurements?","/catalogue/pieces/example/",null,true);
        string id;
        await using(var db=database.NewContext()) {var service=new InquiryService(new InquiryRepository(db),new Products(),TimeProvider.System);var created=await service.SubmitAsync(input,default);id=created.Id;Assert.False(created.Reused);Assert.True((await service.SubmitAsync(input,default)).Reused);}
        await using(var db=database.NewContext()) {var repo=new InquiryRepository(db);var service=new InquiryService(repo,new Products(),TimeProvider.System);Assert.Single(await repo.ListAsync(default));var changed=await service.UpdateAsync(id,new(1,"qualified","Ask for size"),default);Assert.Equal(2,changed.Revision);Assert.Equal(409,(await Assert.ThrowsAsync<MarketingException>(()=>service.UpdateAsync(id,new(1,"closed",null),default))).StatusCode);}
    }
    [Fact]
    public async Task Missing_consent_honeypot_unpublished_product_and_changed_replay_are_rejected()
    {
        await using var db=database.NewContext();var service=new InquiryService(new InquiryRepository(db),new Products(),TimeProvider.System);
        var input=new InquiryInput(Guid.NewGuid().ToString(),"Reader","email","reader@example.test","Measurements?","/catalogue/",null,true);
        await Assert.ThrowsAsync<MarketingException>(()=>service.SubmitAsync(input with{Consent=false},default));
        await Assert.ThrowsAsync<MarketingException>(()=>service.SubmitAsync(input with{Website="bot"},default));
        await Assert.ThrowsAsync<MarketingException>(()=>service.SubmitAsync(input with{ProductId="withdrawn"},default));
        await service.SubmitAsync(input,default);Assert.Equal(409,(await Assert.ThrowsAsync<MarketingException>(()=>service.SubmitAsync(input with{Message="Changed"},default))).StatusCode);
    }
    private sealed class Products:IPublishedProductVerifier{public Task<bool> ExistsAsync(string id,CancellationToken ct)=>Task.FromResult(false);}
}

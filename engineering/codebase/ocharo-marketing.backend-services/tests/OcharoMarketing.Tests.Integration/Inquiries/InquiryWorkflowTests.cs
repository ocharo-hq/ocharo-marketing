using Microsoft.EntityFrameworkCore;
using AwesomeAssertions;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using OcharoMarketing.Persistence.Inquiries;
using Xunit;
namespace OcharoMarketing.Tests.Integration.Inquiries;
public sealed class InquiryWorkflowTests:IAsyncLifetime
{
    private readonly MarketingDatabase database=new();
    public Task InitializeAsync()=>database.InitializeAsync();
    public Task DisposeAsync()=>database.DisposeAsync();
    [Fact]
    public async Task Submit_ShouldPersistAConsentedSubmissionAndReplayItOnce()
    {
        var input=new InquiryInput(
            Guid.NewGuid().ToString(),
            "Reader",
            "email",
            "reader@example.test",
            "What are the measurements?",
            "/catalogue/pieces/example/",
            null,
            true);
        string id;
        await using(var db=database.NewContext())
        {
            var service=new InquiryService(new InquiryRepository(db),new Products(),TimeProvider.System);
            var created=await service.SubmitAsync(input,default);
            id=created.Id;
            created.Reused.Should().BeFalse();
            ((await service.SubmitAsync(input,default)).Reused).Should().BeTrue();
        }
        await using(var db=database.NewContext())
        {
            var repo=new InquiryRepository(db);
            var service=new InquiryService(repo,new Products(),TimeProvider.System);
            (await repo.ListAsync(default)).Should().ContainSingle();
            var changed=await service.UpdateAsync(id,new(1,"qualified","Ask for size"),default);
            changed.Revision.Should().Be(2);
            (
                ErrorStatus
                    .Of(
                        (
                            await FluentActions
                                .Awaiting(()=>service.UpdateAsync(id,new(1,"closed",null),default))
                                .Should()
                                .ThrowExactlyAsync<AppException>()).Which))
                    .Should()
                    .Be(409);
        }
    }
    [Fact]
    public async Task Submit_ShouldRefuse_WhenConsentHoneypotProductOrReplayIsInvalid()
    {
        await using var db=database.NewContext();
        var service=new InquiryService(new InquiryRepository(db),new Products(),TimeProvider.System);
        var input=new InquiryInput(
            Guid.NewGuid().ToString(),
            "Reader",
            "email",
            "reader@example.test",
            "Measurements?",
            "/catalogue/",
            null,
            true);
        await FluentActions
            .Awaiting(()=>service.SubmitAsync(input with{Consent=false},default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        await FluentActions
            .Awaiting(()=>service.SubmitAsync(input with{Website="bot"},default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        await FluentActions
            .Awaiting(()=>service.SubmitAsync(input with{ProductId="withdrawn"},default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        await service.SubmitAsync(input,default);
        (
            ErrorStatus
                .Of(
                    (
                        await FluentActions
                            .Awaiting(()=>service.SubmitAsync(input with{Message="Changed"},default))
                            .Should()
                            .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(409);
    }
    private sealed class Products:IPublishedProductVerifier
    {
        public Task<bool> ExistsAsync(string id,CancellationToken ct)=>Task.FromResult(false);
    }
}

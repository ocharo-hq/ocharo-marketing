using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OcharoMarketing.Application.Marketing;
namespace OcharoMarketing.Application.Inquiries;

/// <summary>Consented, bounded inbound enquiries; no outgoing message is sent.</summary>
public sealed partial class InquiryService(IInquiryRepository repository,IPublishedProductVerifier products,TimeProvider time)
{
    public async Task<(string Id,bool Reused)> SubmitAsync(InquiryInput input,CancellationToken ct)
    {
        if(!Guid.TryParse(input.SubmissionId,out var key))throw new MarketingException("A valid submission reference is required.");
        if(!input.Consent)throw new MarketingException("Please consent to being contacted about this enquiry.");
        if(!string.IsNullOrEmpty(input.Website))throw new MarketingException("Invalid submission.");
        var name=Text(input.Name,100,"name");var contact=Text(input.Contact,180,"contact");var message=Text(input.Message,3000,"message");
        if(input.ContactMethod is not("email" or "telegram" or "phone"))throw new MarketingException("Choose email, Telegram or phone.");
        if(input.ContactMethod=="email"&&(!MailAddress.TryCreate(contact,out var mail)||mail.Address!=contact))throw new MarketingException("Enter a valid email address.");
        if(input.ContactMethod=="telegram"&&!Telegram().IsMatch(contact))throw new MarketingException("Enter a Telegram username, not a URL.");
        if(input.ContactMethod=="phone"&&!Phone().IsMatch(contact))throw new MarketingException("Enter a phone number with its country code.");
        var source=Text(input.SourcePath,500,"source");
        if(!new[]{"/catalogue/","/atlas/","/wear/","/journal/"}.Any(source.StartsWith)||source.Contains("..",StringComparison.Ordinal)||source.Contains('\n')||source.Contains('?'))throw new MarketingException("Invalid enquiry source.");
        var product=input.ProductId?.Trim();if(product is {Length:>120})throw new MarketingException("Invalid product reference.");
        var fingerprint=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{name,contact,message,source,product,input.ContactMethod}))));
        var id=key.ToString("N");var existing=await repository.FindAsync(id,ct);
        if(existing is not null){if(existing.Fingerprint!=fingerprint)throw new MarketingException("This submission reference was already used.",409);return(id,true);}
        if(!string.IsNullOrWhiteSpace(product)&&!await products.ExistsAsync(product,ct))throw new MarketingException("This piece is not currently published. Contact Ocharo through its verified channels.",409);
        var record=new InquiryRecord(id,fingerprint,name,input.ContactMethod,contact,message,source,product,true,time.GetUtcNow(),"new",1);
        try{await repository.InsertAsync(record,ct);}catch(MarketingException e)when(e.StatusCode==409){var replay=await repository.FindAsync(id,ct);if(replay?.Fingerprint!=fingerprint)throw;return(id,true);}
        return(id,false);
    }
    public async Task<InquiryPage> ListAsync(string? status,string? search,string? followUp,int offset,int limit,CancellationToken ct)
    {
        if(!string.IsNullOrEmpty(status))ValidateStatus(status);
        if(!string.IsNullOrEmpty(followUp)&&followUp is not("due" or "scheduled" or "unassigned"))throw new MarketingException("Choose a valid follow-up filter.");
        var term=Optional(search,200,"search");var now=time.GetUtcNow();
        var rows=(await repository.ListAsync(ct)).Where(x=>string.IsNullOrEmpty(status)||x.Status==status)
            .Where(x=>followUp switch
            {
                "due"=>x.Status!="closed"&&x.FollowUpAt is {} due&&due<=now,
                "scheduled"=>x.Status!="closed"&&x.FollowUpAt is not null,
                "unassigned"=>x.Status!="closed"&&string.IsNullOrWhiteSpace(x.AssignedTo),
                _=>true
            })
            .Where(x=>term is null||new[]{x.Name,x.Contact,x.Message,x.ProductId,x.Id,x.Note,x.AssignedTo,x.NextStep}
                .Any(v=>v?.Contains(term,StringComparison.OrdinalIgnoreCase)==true)).ToList();
        var ordered=followUp is "due" or "scheduled"?rows.OrderBy(x=>x.FollowUpAt).ThenBy(x=>x.Id):rows.OrderByDescending(x=>x.CreatedAt).ThenBy(x=>x.Id);
        var skip=Math.Max(0,offset);var take=Math.Clamp(limit,1,100);
        return new(rows.Count,skip,take,now,ordered.Skip(skip).Take(take).ToList());
    }
    public async Task<InquiryRecord> UpdateAsync(string id,InquiryUpdate input,CancellationToken ct,string? actor=null)
    {
        ValidateStatus(input.Status);
        var existing=await repository.FindAsync(id,ct)??throw new MarketingException("Enquiry not found.",404);
        if(existing.Revision!=input.ExpectedRevision)throw new MarketingException("The enquiry changed. Reload it first.",409);
        var note=Optional(input.Note,3000,"internal note");var owner=Optional(input.FollowUp is {} plan?plan.AssignedTo:existing.AssignedTo,100,"follow-up owner");
        var nextStep=Optional(input.FollowUp is {} step?step.NextStep:existing.NextStep,500,"next step");
        var due=(input.FollowUp is {} followUp?followUp.FollowUpAt:existing.FollowUpAt)?.ToUniversalTime();
        if(due is not null&&(owner is null||nextStep is null))throw new MarketingException("A scheduled follow-up needs an owner and next step.");
        if(input.Status=="closed"&&due is not null)throw new MarketingException("Clear the follow-up date before closing the enquiry.");
        if(due is {} date&&(date.Year<2000||date.Year>2100))throw new MarketingException("Choose a follow-up date between 2000 and 2100.");
        if(existing.Status==input.Status&&existing.Note==note&&existing.AssignedTo==owner&&existing.FollowUpAt==due&&existing.NextStep==nextStep)return existing;
        var now=time.GetUtcNow();
        var updated=existing with{Status=input.Status,Note=note,AssignedTo=owner,FollowUpAt=due,NextStep=nextStep,Revision=existing.Revision+1,UpdatedAt=now};
        var activity=new InquiryActivity(updated.Revision,now,string.IsNullOrWhiteSpace(actor)?"local-operator":actor,input.Status,note,owner,due,nextStep);
        await repository.UpdateAsync(updated,input.ExpectedRevision,activity,ct);return updated;
    }
    public async Task<InquiryActivityPage> ActivityAsync(string id,int offset,int limit,CancellationToken ct,int? beforeRevision=null)
    {
        if(await repository.FindAsync(id,ct)is null)throw new MarketingException("Enquiry not found.",404);
        if(beforeRevision is <1||(beforeRevision is not null&&offset!=0))throw new MarketingException("Use a valid activity revision cursor without an offset.");
        return await repository.ActivityAsync(id,Math.Max(0,offset),Math.Clamp(limit,1,100),ct,beforeRevision);
    }
    private static void ValidateStatus(string status)
    {if(status is not("new" or "contacted" or "qualified" or "closed"))throw new MarketingException("Invalid enquiry status.");}
    private static string? Optional(string? value,int max,string field)
    {
        var text=value?.Trim();if(string.IsNullOrEmpty(text))return null;
        if(text.Length>max||text.Contains('\0'))throw new MarketingException($"Keep {field} under {max} characters.");return text;
    }
    private static string Text(string? value,int max,string field){var s=value?.Trim();if(string.IsNullOrWhiteSpace(s)||s.Length>max||s.Contains('\0'))throw new MarketingException($"Provide a {field} under {max} characters.");return s;}
    [GeneratedRegex("^@?[A-Za-z][A-Za-z0-9_]{4,31}$")]private static partial Regex Telegram();
    [GeneratedRegex("^\\+?[0-9 ()-]{7,24}$")]private static partial Regex Phone();
}

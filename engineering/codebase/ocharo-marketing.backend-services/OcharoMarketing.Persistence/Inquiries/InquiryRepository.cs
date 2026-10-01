using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Domain.Marketing.Entities;
namespace OcharoMarketing.Persistence.Inquiries;

/// <summary>One durable metadata row per inquiry; existing SQLite schema, optimistic atomic updates.</summary>
public sealed class InquiryRepository(OcharoMarketingDbContext db):IInquiryRepository
{
    private const string Prefix="inquiry:";
    public async Task<IReadOnlyList<InquiryRecord>> ListAsync(CancellationToken ct)=>
        (await db.Metadata.AsNoTracking().Where(x=>x.Id.StartsWith(Prefix)).ToListAsync(ct)).Select(x=>MarketingJson.Read<InquiryRecord>(x.Json)).OrderByDescending(x=>x.CreatedAt).ToList();
    public async Task<InquiryRecord?> FindAsync(string id,CancellationToken ct)=>await db.Metadata.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==Prefix+id,ct)is{} record?MarketingJson.Read<InquiryRecord>(record.Json):null;
    public async Task InsertAsync(InquiryRecord record,CancellationToken ct)
    {
        var row=new MetadataRecord{Id=Prefix+record.Id,Json=MarketingJson.Write(record)};db.Metadata.Add(row);
        try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){db.Entry(row).State=EntityState.Detached;throw new MarketingException("Submission reference already exists.",409);}
    }
    public async Task UpdateAsync(InquiryRecord record,int expectedRevision,InquiryActivity activity,CancellationToken ct)
    {
        var row=await db.Metadata.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==Prefix+record.Id,ct)??throw new MarketingException("Enquiry not found.",404);
        if(MarketingJson.Read<InquiryRecord>(row.Json).Revision!=expectedRevision)throw new MarketingException("Enquiry changed. Reload it first.",409);
        await using var transaction=await db.Database.BeginTransactionAsync(ct);
        var changed=await db.Metadata.Where(x=>x.Id==row.Id&&x.Json==row.Json).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Json,MarketingJson.Write(record)),ct);
        if(changed!=1)throw new MarketingException("Enquiry changed. Reload it first.",409);
        var audit=new MetadataRecord{Id=$"inquiry-activity:{record.Id}:{activity.Revision:D10}",Json=MarketingJson.Write(activity)};
        db.Metadata.Add(audit);
        try{await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);}
        catch{db.Entry(audit).State=EntityState.Detached;throw;}
    }
    public async Task<InquiryActivityPage> ActivityAsync(string id,int offset,int limit,CancellationToken ct,int? beforeRevision=null)
    {
        var prefix=$"inquiry-activity:{id}:";
        var query=db.Metadata.AsNoTracking().Where(x=>x.Id.StartsWith(prefix));var total=await query.CountAsync(ct);
        if(beforeRevision is {} cursor)
        {
            var upper=$"{prefix}{cursor:D10}";
            query=query.Where(x=>string.Compare(x.Id,upper)<0);
        }
        var rows=await query.OrderByDescending(x=>x.Id).Skip(offset).Take(limit+1).ToListAsync(ct);
        var items=rows.Take(limit).Select(x=>MarketingJson.Read<InquiryActivity>(x.Json)).ToList();
        return new(total,offset,limit,items,rows.Count>limit?items[^1].Revision:null);
    }
}

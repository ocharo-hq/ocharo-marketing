using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Media.BackgroundRemoval;
using WoW.Two.Sdk.Beta.Be.Media.Images;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Preserves validated original bytes and creates upright alpha-preserving web renditions through the SDK.</summary>
public sealed class MediaStore(IBlogRepository repository, IBlobRepository blobs, IOptions<BlogOptions> options, IClock clock, IImageService images) : IMediaStore
{
    public Task<MediaBlobRecord> StoreAsync(byte[] bytes,string fileName,CancellationToken ct)=>StoreCoreAsync(bytes,fileName,null,ct);
    public Task<MediaBlobRecord> StoreEditedAsync(byte[] bytes,string fileName,MediaEditRecord edit,CancellationToken ct)=>StoreCoreAsync(bytes,fileName,edit,ct);

    private async Task<MediaBlobRecord> StoreCoreAsync(byte[] bytes,string fileName,MediaEditRecord? edit,CancellationToken ct)
    {
        var maximum=(int)Math.Clamp(options.Value.MaximumImageBytes,1,MediaEditingService.MaximumBytes);
        if(bytes.Length==0||bytes.LongLength>maximum)throw new BlogException("Choose a non-empty image up to 20 MiB.",413);
        RasterImageInfo info;
        try{info=RasterImageGuard.Validate(bytes,maximum);}
        catch(BackgroundRemovalException e){throw new BlogException(e.Message);}
        var sha=Convert.ToHexStringLower(SHA256.HashData(bytes));
        if(edit is not null&&edit.Sha256!=sha)throw new BlogException("Edited image does not match its receipt.");
        if(await repository.FindMediaAsync(sha,ct) is {} existing)
        {
            if(edit is not null)await repository.SaveMediaWithEditAsync(existing,edit,ct);
            return existing;
        }
        var path=$"media/{sha[..2]}/{sha}";
        var previewPath=$"previews/{sha[..2]}/{sha}.webp";
        var previewSize=Math.Clamp(options.Value.PreviewSize,1,1400);
        using var processingSource=new MemoryStream(bytes,false);
        var preview=await images.EditAsync(processingSource,new ImageEditSpec{
            Resize=new(){Width=previewSize,Height=previewSize,Fit=ImageFit.Max},Output=new(){Format=ImageFormat.Webp,Quality=84}},ct);
        await using(var input=new MemoryStream(bytes,false))await blobs.SaveAsync(path,input,info.ContentType,ct);
        await using(var rendition=new MemoryStream(preview.Content,false))await blobs.SaveAsync(previewPath,rendition,"image/webp",ct);
        var name=Path.GetFileName(fileName.Replace('\\','/'));
        var record=new MediaBlobRecord{Sha256=sha,FileName=name is {Length:>0 and <=150}&&!name.Any(char.IsControl)?name:"image",
            ContentType=info.ContentType,ByteLength=bytes.LongLength,BlobPath=path,PreviewPath=previewPath,Width=info.Width,Height=info.Height,StoredAt=clock.UtcNow};
        if(edit is null)await repository.SaveMediaAsync(record,ct);
        else await repository.SaveMediaWithEditAsync(record,edit,ct);
        return record;
    }

    public async Task<(MediaBlobRecord Record,Stream Content)?> OpenAsync(string sha256,bool preview,CancellationToken ct)
    {
        if(sha256.Length!=64||!sha256.All(char.IsAsciiHexDigitLower))return null;
        var record=await repository.FindMediaAsync(sha256,ct);if(record is null)return null;
        var stream=await blobs.OpenReadAsync(preview&&record.PreviewPath is not null?record.PreviewPath:record.BlobPath,ct);
        return stream is null?null:(record,stream);
    }
}

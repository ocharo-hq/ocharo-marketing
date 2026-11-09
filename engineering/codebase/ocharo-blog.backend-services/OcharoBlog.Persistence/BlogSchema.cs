namespace OcharoBlog.Persistence;

/// <summary>Names Editorial's PostgreSQL schema in the product database and the migration set that owns it.</summary>
/// <remarks>The product database (<c>ocharo_{env}</c>) also holds Desk's and Identity's sets, so every Editorial table,
/// its migration history and its owner marker live in <see cref="Name"/>, and the BFF's sessions in
/// <see cref="CacheSchema"/>.</remarks>
public static class BlogSchema
{
    /// <summary>The schema holding Editorial's tables, the value of <c>{{schema}}</c> in its SQL.</summary>
    public const string Name = "blog";

    /// <summary>The be sdk migration set over <see cref="Name"/>; its history is
    /// <c>blog_migration_history</c>.</summary>
    public const string MigrationSet = "blog";

    /// <summary>The kind of owner the set claims <see cref="Name"/> as.</summary>
    public const string Owner = "service";

    /// <summary>The Identity set whose organizations and accounts the image receipts key into.</summary>
    public const string IdentitySet = "identity";

    /// <summary>The level of <see cref="IdentitySet"/> that publishes its organizations and accounts.</summary>
    public const int IdentityLevel = 1;

    /// <summary>The schema of the distributed cache holding the BFF's sign-in sessions; never Desk's
    /// <c>cache</c>.</summary>
    public const string CacheSchema = "blog_cache";
}

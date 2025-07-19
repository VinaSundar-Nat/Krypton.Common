using System;

namespace Kr.Common.Infrastructure.Datastore;

public abstract class BaseEntity<TEntity> : IEquatable<TEntity>
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public uint VersionStamp { get; set; }

   public override bool Equals(object? other)
    {
        if (other is not TEntity _compare)
            return false;

        if (other == null) return this == null;

        return this.Equals(_compare);
    }

    public virtual bool Equals(TEntity? other)
    {
        if (other is not BaseEntity<TEntity> compare)
            return false;

        if (compare == null) return this == null;

        return compare.Id.Equals(this.Id);
    }

    public override int GetHashCode() =>
         HashCode.Combine(Id.GetHashCode());
}

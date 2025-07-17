namespace SddlReferral.Data
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;

    [ExcludeFromCodeCoverage]
    public class SddlReferralDbSet<TEntity> where TEntity : class
    {
        private DbSet<TEntity> entitySet;

        public SddlReferralDbSet(DbSet<TEntity> set)
        {
            this.entitySet = set;
        }

        public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await this.entitySet.FirstOrDefaultAsync(predicate).ConfigureAwait(false);
        }

        public virtual EntityEntry<TEntity> Add(TEntity entity)
        {
            return this.entitySet.Add(entity);
        }

        public virtual EntityEntry<TEntity> Update(TEntity entity)
        {
            return this.entitySet.Update(entity);
        }

        public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            return this.entitySet.Where(predicate);
        }
    }
}

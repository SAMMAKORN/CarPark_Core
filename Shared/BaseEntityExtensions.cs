namespace CarPark.Shared
{
    public static class BaseEntityExtensions
    {
        public static void SetCreated(this BaseEntity entity, Guid? userId)
        {
            entity.CreateBy = userId;
            entity.CreateAt = DateTime.UtcNow;
        }

        public static void SetUpdated(this BaseEntity entity, Guid? userId)
        {
            entity.UpdateBy = userId;
            entity.UpdateAt = DateTime.UtcNow;
        }

        public static void SetDeleted(this BaseEntity entity, Guid? userId)
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.DeletedBy = userId;
            entity.DeleteAt = DateTime.UtcNow;
            entity.UpdateBy = userId;
            entity.UpdateAt = DateTime.UtcNow;
        }
    }
}

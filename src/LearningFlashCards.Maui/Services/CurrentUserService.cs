namespace LearningFlashCards.Maui
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }

    public sealed class LocalCurrentUserService : ICurrentUserService
    {
        private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public Guid UserId => DefaultUserId;
    }
}

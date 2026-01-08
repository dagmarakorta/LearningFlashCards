using Microsoft.Maui.Storage;

namespace LearningFlashCards.Maui
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
        void SetUser(Guid userId);
        void Clear();
    }

    public sealed class LocalCurrentUserService : ICurrentUserService
    {
        private const string UserIdKey = "current_user_id";
        private Guid? _userId;

        public LocalCurrentUserService()
        {
            var stored = Preferences.Get(UserIdKey, string.Empty);
            if (Guid.TryParse(stored, out var userId))
            {
                _userId = userId;
            }
        }

        public Guid? UserId => _userId;
        public bool IsAuthenticated => _userId.HasValue;

        public void SetUser(Guid userId)
        {
            _userId = userId;
            Preferences.Set(UserIdKey, userId.ToString());
        }

        public void Clear()
        {
            _userId = null;
            Preferences.Remove(UserIdKey);
        }
    }
}

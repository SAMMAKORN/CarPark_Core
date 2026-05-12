using CarPark.Models;
using CarPark.Shared.Enums;
using Microsoft.Extensions.Configuration;

namespace CarPark.Services
{
    public sealed class CurrentUserContext
    {
        private readonly Role _defaultRole;
        private readonly Guid? _defaultUserId;
        private readonly string _defaultName;
        private readonly string _defaultUsername;
        private readonly bool _defaultMustChangePassword;

        private Role _currentRole;
        private Guid? _currentUserId;
        private string _currentName;
        private string _currentUsername;
        private bool _mustChangePassword;
        private ParkingLot? _currentParkingLot;

        public CurrentUserContext(IConfiguration configuration)
        {
            if (!Enum.TryParse(configuration["Session:Role"], true, out _defaultRole))
            {
                _defaultRole = Role.User;
            }

            if (Guid.TryParse(configuration["Session:UserId"], out var userId))
            {
                _defaultUserId = userId;
            }

            _defaultName = configuration["Session:Name"] ?? "Guest";
            _defaultUsername = configuration["Session:Username"] ?? "guest";
            _defaultMustChangePassword = bool.TryParse(configuration["Session:MustChangePassword"], out var mustChangePassword)
                && mustChangePassword;

            _currentRole = _defaultRole;
            _currentUserId = _defaultUserId;
            _currentName = _defaultName;
            _currentUsername = _defaultUsername;
            _mustChangePassword = _defaultMustChangePassword;
        }

        public Role CurrentRole => _currentRole;
        public Guid? CurrentUserId => _currentUserId;
        public string CurrentName => _currentName;
        public string CurrentUsername => _currentUsername;
        public bool MustChangePassword => _mustChangePassword;
        public bool IsAuthenticated => _currentUserId.HasValue;
        public bool IsAdmin => _currentRole == Role.Admin;
        public ParkingLot? CurrentParkingLot => _currentParkingLot;

        public void SignIn(User user)
        {
            _currentRole = user.Role;
            _currentUserId = user.Id;
            _currentName = user.Name;
            _currentUsername = user.Username;
            _mustChangePassword = user.MustChangePassword;
        }

        public void SelectParkingLot(ParkingLot lot)
        {
            _currentParkingLot = lot;
        }

        public void SetMustChangePassword(bool mustChangePassword)
        {
            _mustChangePassword = mustChangePassword;
        }

        public void SignOut()
        {
            _currentRole = _defaultRole;
            _currentUserId = _defaultUserId;
            _currentName = _defaultName;
            _currentUsername = _defaultUsername;
            _mustChangePassword = _defaultMustChangePassword;
            _currentParkingLot = null;
        }
    }
}

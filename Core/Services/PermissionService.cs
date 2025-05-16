using System;
using Microsoft.Extensions.Logging;
using SANJET.Core.Constants.Enums;
using SANJET.Core.Models;

namespace SANJET.Core.Services
{
    public class PermissionService
    {
        private readonly SqliteDataService _dbService;
        private readonly ILogger<PermissionService> _logger;
        private User? _currentUser;

        public event EventHandler? PermissionsChanged;

        public PermissionService(SqliteDataService dbService, ILogger<PermissionService> logger)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("PermissionService initialized.");
        }

        public bool IsLoggedIn => _currentUser != null;

        public User? CurrentUser => _currentUser;

        public bool Login(string username, string password)
        {
            _logger.LogInformation("Attempting to login with username: {Username}", username);
            try
            {
                var user = _dbService.GetUserWithPermissions(username, password);
                if (user != null)
                {
                    _currentUser = user;
                    _logger.LogInformation("Login successful for user: {Username} with permissions: {Permissions}",
                        username, string.Join(", ", _currentUser.Permissions));
                    PermissionsChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                _logger.LogWarning("Login failed for user: {Username} - No matching user found.", username);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {Username}", username);
                return false;
            }
        }

        public bool HasPermission(Permission permission)
        {
            if (_currentUser == null)
            {
                _logger.LogDebug("No user logged in, permission check for {Permission} failed.", permission);
                return false;
            }

            string permissionName = permission.ToString();
            bool hasPermission = _currentUser.Permissions.Contains(permissionName);
            _logger.LogDebug("Permission check for {Permission}: {HasPermission}", permissionName, hasPermission);
            return hasPermission;
        }

        public void Logout()
        {
            _currentUser = null;
            _logger.LogInformation("User logged out.");
            PermissionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
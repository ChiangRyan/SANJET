using System;
using System.Diagnostics;
using SANJET.Core.Enums;
using SANJET.SANJET.Core.Models;


namespace SANJET.SANJET.Core.Services
{
    public class PermissionService
    {
        private readonly SqliteDataService _dbService;
        private User _currentUser;

        public event EventHandler PermissionsChanged;

        public PermissionService(SqliteDataService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            Debug.WriteLine("PermissionService initialized.");
        }

        public bool IsLoggedIn => _currentUser != null;

        public bool Login(string username, string password)
        {
            Debug.WriteLine($"Attempting to login with username: {username}, password: {password}");
            try
            {
                var user = _dbService.GetUserWithPermissions(username, password);
                if (user != null)
                {
                    _currentUser = user;
                    Debug.WriteLine($"Login successful for user: {username} with permissions: {string.Join(", ", _currentUser.Permissions)}");
                    PermissionsChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                Debug.WriteLine($"Login failed for user: {username} - No matching user found.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login failed for user: {username} - Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public bool HasPermission(Permission permission)
        {
            if (_currentUser == null)
            {
                Debug.WriteLine("No user logged in, permission check failed.");
                return false;
            }

            string permissionName = permission.ToString();
            bool hasPermission = _currentUser.Permissions.Contains(permissionName);
            Debug.WriteLine($"Permission check for {permissionName}: {hasPermission}");
            return hasPermission;
        }

        public void Logout()
        {
            _currentUser = null;
            Debug.WriteLine("User logged out.");
            PermissionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public User CurrentUser => _currentUser;
    }
}

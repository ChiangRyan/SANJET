

namespace SANJET.Core.Interfaces
{
    public interface ILoginDialogService
    {
        (bool Success, string Username, string Password) ShowLoginDialog();
    }
}

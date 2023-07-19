using System.ComponentModel;
using System.Threading.Tasks;

namespace ConsoleApp1.SDK
{
    public interface IAuthenticationManager : INotifyPropertyChanged
    {
        public Task Initialize();

        public string GetUserName();

        public string GetUserEmail();

        public string GetUserGuid();

        public bool IsAuthenticated { get; }

        public bool Initialized { get; }

        public Task<bool> GetAuthenticationTokenWithCredentialAsync(string userName, string password);

        public Task<string> GetAccessToken();

        public void Logout();

    }
}
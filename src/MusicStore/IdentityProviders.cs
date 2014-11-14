using Microsoft.AspNet.Identity;
using MusicStore.Models;
using System.Threading.Tasks;
using System.Threading;

namespace MusicStore
{
    public class EmailMessageProvider : IUserMessageProvider<ApplicationUser>
    {
        public string Name
        {
            get
            {
                return "Email";
            }
        }

        public Task SendAsync(UserManager<ApplicationUser> manager, ApplicationUser user, IdentityMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Plug in your service
            return Task.FromResult(0);
        }
    }

    public class SmsMessageProvider : IUserMessageProvider<ApplicationUser>
    {
        public string Name
        {
            get
            {
                return "SMS";
            }
        }

        public Task SendAsync(UserManager<ApplicationUser> manager, ApplicationUser user, IdentityMessage message, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Plug in your service
            return Task.FromResult(0);
        }
    }
}
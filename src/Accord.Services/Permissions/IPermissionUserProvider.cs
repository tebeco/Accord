using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;

namespace Accord.Services.Permissions;

public interface IPermissionUserProvider
{
    Task<PermissionUser> GetPermissionUser(ulong discordUserId, CancellationToken cancellationToken);
}

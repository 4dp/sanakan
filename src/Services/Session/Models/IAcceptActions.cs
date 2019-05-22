#pragma warning disable 1591

using System.Threading.Tasks;

namespace Sanakan.Services.Session.Models
{
    public interface IAcceptActions
    {
        Task<bool> OnAccept(SessionContext context);
        Task<bool> OnDecline(SessionContext context);
    }
}

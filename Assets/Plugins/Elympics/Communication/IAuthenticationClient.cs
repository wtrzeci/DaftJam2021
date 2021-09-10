using System.Threading.Tasks;

namespace Elympics
{
	public interface IAuthenticationClient
	{
		Task<(bool Success, string UserId, string JwtToken)> AuthenticateWithAuthToken(string endpoint, string authToken);
	}
}

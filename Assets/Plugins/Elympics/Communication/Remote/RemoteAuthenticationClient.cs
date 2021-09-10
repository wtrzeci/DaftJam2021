using System.Threading.Tasks;
using LobbyPublicApiClients.User;
using LobbyPublicApiModels.User.User;

namespace Elympics
{
	public class RemoteAuthenticationClient : IAuthenticationClient
	{
		private readonly IUserApiClient _userApiClient;

		public RemoteAuthenticationClient(IUserApiClient userApiClient)
		{
			_userApiClient = userApiClient;
		}

		public async Task<(bool Success, string UserId, string JwtToken)> AuthenticateWithAuthToken(string endpoint, string authToken)
		{
			_userApiClient.SetServerUri(endpoint);
			_userApiClient.SetAuthToken(authToken);
			var response = await _userApiClient.AuthenticateUserIdAsync(new AuthenticateUserIdModel.Request());
			return !response.IsSuccess
				? (false, null, null)
				: (true, response.UserId, response.JwtToken);
		}
	}
}

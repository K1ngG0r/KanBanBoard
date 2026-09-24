using Domain.Models;

namespace Application.Interfaces;

public interface ITokenService
{
    string GetAccessToken(Account account);
    string GetRefreshToken();
}

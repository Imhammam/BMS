using BMS1.Models;

namespace BMS1.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}

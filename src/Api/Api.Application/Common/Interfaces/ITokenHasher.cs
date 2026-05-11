namespace Api.Application.Common.Interfaces;

public interface ITokenHasher
{
    string HashToken(string rawToken);
}

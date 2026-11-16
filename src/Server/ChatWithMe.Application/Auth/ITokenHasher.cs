namespace ChatWithMe.Application.Auth;

public interface ITokenHasher
{
    string Hash(string token);
}

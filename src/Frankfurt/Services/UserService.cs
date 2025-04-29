using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frankfurt.Services.Interfaces;

namespace Frankfurt.Services;

public class UserService : IUserService
{
    private readonly List<Tuple<string, string, string>> USERS = new List<Tuple<string, string, string>>{
        new Tuple<string, string, string>("admin", "123", "Admin,User"),
        new Tuple<string, string, string>("user", "123", "User"),
        new Tuple<string, string, string>("user2", "123", "User")
    };

    private readonly IJwtTokenService jwtTokenService;
    public UserService(IJwtTokenService jwtTokenService)
    {
        this.jwtTokenService = jwtTokenService;
    }

    public Tuple<string, string, string> Login(string username, string password)
    {
        var user = USERS.FirstOrDefault(u => u.Item1 == username && u.Item2 == password);
        if (user != null)
        {
            return user; // Return the role
        }
        return null; // Invalid credentials
    }

    public string GenerateToken(Tuple<string, string, string> tuple)
    {
        if (tuple == null)
        {
            throw new ArgumentNullException(nameof(tuple), "User info cannot be null");
        }
        return jwtTokenService.GenerateToken(tuple.Item1, tuple.Item3.Split(",").ToArray());
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Services.Interfaces;

public interface IUserService
{
    Tuple<string, string, string> Login(string username, string password);
    string GenerateToken(Tuple<string, string, string> tuple);
}
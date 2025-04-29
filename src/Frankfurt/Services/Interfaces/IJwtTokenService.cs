using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Services.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string[] roles);
}
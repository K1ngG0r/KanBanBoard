using System;
using System.Collections.Generic;
using System.Text;

namespace Api.Response;

public class LoginResult
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}

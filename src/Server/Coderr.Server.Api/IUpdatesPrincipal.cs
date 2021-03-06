﻿using System.Security.Claims;

namespace codeRR.Server.Api
{
    /// <summary>
    ///     The handler modifies the permissions for the current user.
    /// </summary>
    public interface IUpdatesPrincipal
    {
        /// <summary>
        ///     Current user
        /// </summary>
        ClaimsPrincipal Principal { set; }
    }
}
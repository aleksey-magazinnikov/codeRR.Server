﻿using System;
using System.Security.Principal;
using System.Threading;

namespace codeRR.Server.ReportAnalyzer
{
    /// <summary>
    ///     codeRR principal
    /// </summary>
    public class CoderrPrincipal : IPrincipal
    {
        /// <summary>
        ///     Creates a new instance of <see cref="CoderrPrincipal" />.
        /// </summary>
        /// <param name="userName">logged in user</param>
        /// <exception cref="ArgumentNullException">userName</exception>
        public CoderrPrincipal(string userName)
        {
            if (userName == null) throw new ArgumentNullException(nameof(userName));
            Identity = new GenericIdentity(userName);
        }

        /// <summary>
        ///     Current user, do not work in an async/task context.
        /// </summary>
        public static CoderrPrincipal Current
        {
            get { return (CoderrPrincipal) Thread.CurrentPrincipal; }
        }


        /// <summary>
        ///     Determines whether the current principal belongs to the specified role.
        /// </summary>
        /// <returns>
        ///     true if the current principal is a member of the specified role; otherwise, false.
        /// </returns>
        /// <param name="role">The name of the role for which to check membership. </param>
        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the identity of the current principal.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:System.Security.Principal.IIdentity" /> object associated with the current principal.
        /// </returns>
        public IIdentity Identity { get; }
    }
}
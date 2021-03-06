﻿using System.Threading.Tasks;
using codeRR.Server.Api.Core.Notifications;
using DotNetCqs;
using Griffin.Container;

namespace codeRR.Server.App.Core.Notifications.Commands
{
    /// <summary>
    /// Handler for <see cref="AddNotification"/>.
    /// </summary>
    [Component]
    public class AddNotificationHandler : ICommandHandler<AddNotification>
    {
        /// <summary>
        /// Not implemented yet.
        /// </summary>
        /// <param name="command">cmd</param>
        /// <returns>task</returns>
        public Task ExecuteAsync(AddNotification command)
        {
            //TODO: Implement
            return Task.FromResult<object>(null);
        }
    }
}

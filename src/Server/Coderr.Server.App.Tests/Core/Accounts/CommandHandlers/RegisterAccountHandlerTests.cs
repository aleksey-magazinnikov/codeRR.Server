﻿using System.Threading.Tasks;
using codeRR.Server.Api.Core.Accounts.Commands;
using codeRR.Server.Api.Core.Accounts.Events;
using codeRR.Server.Api.Core.Messaging.Commands;
using codeRR.Server.App.Core.Accounts;
using codeRR.Server.App.Core.Accounts.CommandHandlers;
using codeRR.Server.Infrastructure.Configuration;
using DotNetCqs;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace codeRR.Server.App.Tests.Core.Accounts.CommandHandlers
{
    public class RegisterAccountHandlerTests
    {
        [Fact]
        public async Task Should_create_a_new_account()
        {
            ConfigurationStore.Instance = new TestStore();
            var repos = Substitute.For<IAccountRepository>();
            var cmdBus = Substitute.For<ICommandBus>();
            var eventBus = Substitute.For<IEventBus>();
            var cmd = new RegisterAccount("rne", "yo", "some@Emal.com");
            repos.When(x => x.CreateAsync(Arg.Any<Account>()))
                .Do(x => x.Arg<Account>().SetId(3));


            var sut = new RegisterAccountHandler(repos, cmdBus, eventBus);
            await sut.ExecuteAsync(cmd);
            await repos.Received().CreateAsync(Arg.Any<Account>());
        }

        [Fact]
        public async Task Should_inform_the_rest_of_the_system_about_the_new_account()
        {
            ConfigurationStore.Instance = new TestStore();
            var repos = Substitute.For<IAccountRepository>();
            var cmdBus = Substitute.For<ICommandBus>();
            var eventBus = Substitute.For<IEventBus>();
            var cmd = new RegisterAccount("rne", "yo", "some@Emal.com");
            repos.When(x => x.CreateAsync(Arg.Any<Account>()))
                .Do(x => x.Arg<Account>().SetId(3));


            var sut = new RegisterAccountHandler(repos, cmdBus, eventBus);
            await sut.ExecuteAsync(cmd);

            await eventBus.Received().PublishAsync(Arg.Any<AccountRegistered>());
            AssertionExtensions.Should((int) eventBus.Method("PublishAsync").Arg<AccountRegistered>().AccountId).Be(3);
        }

        [Fact]
        public async Task Should_send_activation_email()
        {
            ConfigurationStore.Instance = new TestStore();
            var repos = Substitute.For<IAccountRepository>();
            var cmdBus = Substitute.For<ICommandBus>();
            var eventBus = Substitute.For<IEventBus>();
            var cmd = new RegisterAccount("rne", "yo", "some@Emal.com");
            repos.When(x => x.CreateAsync(Arg.Any<Account>()))
                .Do(x => x.Arg<Account>().SetId(3));


            var sut = new RegisterAccountHandler(repos, cmdBus, eventBus);
            await sut.ExecuteAsync(cmd);

            await cmdBus.Received().ExecuteAsync(Arg.Any<SendEmail>());
        }
    }
}
﻿#if NET452
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace codeRR.Server.Infrastructure
{
    /// <summary>
    ///     Generates SQL connections
    /// </summary>
    public class Net452ConnectionFactory : IConnectionFactory
    {
        private static Func<IDbConnection> _customFactory;

        /// <inheritdoc />
        public IDbConnection Open()
        {
            return Create("Db");
        }

        /// <inheritdoc />
        public IDbConnection Open(string connectionStringName)
        {
            return Create(connectionStringName);
        }

        /// <inheritdoc />
        public IDbConnection TryOpen(string connectionStringName)
        {
            return Create(connectionStringName, false);
        }


        /// <summary>
        ///     For integration tests etc.
        /// </summary>
        /// <param name="factoryMethod">factory method</param>
        /// <param name="caller"></param>
        public static void SetConnectionFactory(Func<IDbConnection> factoryMethod,
            [CallerMemberName] string caller = "")
        {
            _customFactory = factoryMethod;
        }

        /// <summary>
        ///     Creates a connection using the <c>web.config</c> connection string named <c>Db</c>.
        /// </summary>
        /// <returns>open connection</returns>
        private static IDbConnection Create(string connectionStringName, bool throwIfMissing = true)
        {
            if (_customFactory != null)
            {
                var con = _customFactory();
                return con;
            }

            var conStr = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (conStr == null)
            {
                if (throwIfMissing)
                    throw new ConfigurationErrorsException(
                        $"Expected a <connectionString> named '{connectionStringName}' in web.config");
                return null;
            }


            var provider = DbProviderFactories.GetFactory(conStr.ProviderName);
            if (provider == null)
                throw new ConfigurationErrorsException(
                    $"Sql provider '{conStr.ProviderName}' was not found/registered.");

            var connection = provider.CreateConnection();
            connection.ConnectionString = conStr.ConnectionString + ";connect timeout=22;";
            try
            {
                connection.Open();
            }
            catch (DataException ex)
            {
                throw new DataException(
                    $"Failed to connect to '{conStr.ConnectionString}'. See inner exception for the reason.", ex);
            }

            return connection;
        }
    }
}
#endif
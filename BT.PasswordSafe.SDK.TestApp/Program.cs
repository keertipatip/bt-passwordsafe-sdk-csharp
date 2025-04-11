using BT.PasswordSafe.SDK;
using BT.PasswordSafe.SDK.Extensions;
using BT.PasswordSafe.SDK.Interfaces;
using BT.PasswordSafe.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BT.PasswordSafe.SDK.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("BT.PasswordSafe.SDK Test Application");
            Console.WriteLine("====================================");

            // Set up configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add the PasswordSafe client
            services.AddPasswordSafeClient(options => configuration.GetSection("PasswordSafe").Bind(options));

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Get the client from the service provider
            var client = serviceProvider.GetRequiredService<IPasswordSafeClient>();

            try
            {
                // Get test settings
                var testSettings = configuration.GetSection("TestSettings");
                var systemId = testSettings["SystemId"];
                var accountId = testSettings["AccountId"];
                var accountName = testSettings["AccountName"];
                var systemName = testSettings["SystemName"];

                // Test authentication
                Console.WriteLine("\nTesting Authentication...");
                var authResult = await client.Authenticate();
                Console.WriteLine($"Authentication successful. Token type: {authResult.TokenType}");
                Console.WriteLine($"Token expires in: {authResult.ExpiresIn} seconds");

                // Test getting managed systems
                Console.WriteLine("\nTesting GetManagedSystems...");
                var systems = await client.GetManagedSystems();
                Console.WriteLine($"Found {systems.Count()} managed systems");

                // Test getting a specific managed system
                if (!string.IsNullOrEmpty(systemId))
                {
                    Console.WriteLine($"\nTesting GetManagedSystems with ID: {systemId}...");
                    var specificSystem = await client.GetManagedSystems(systemId);
                    var system = specificSystem.FirstOrDefault();
                    if (system != null)
                    {
                        Console.WriteLine($"Found system: {system.SystemName} (ID: {system.ManagedSystemId})");
                    }
                    else
                    {
                        Console.WriteLine($"System with ID {systemId} not found");
                    }
                }

                // Test getting managed accounts
                Console.WriteLine("\nTesting GetManagedAccounts...");
                var accounts = await client.GetManagedAccounts();
                Console.WriteLine($"Found {accounts.Count()} managed accounts");

                // Test getting a specific managed account by ID
                if (!string.IsNullOrEmpty(accountId))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting GetManagedAccountPasswordById with ID: {accountId}...");
                        var password = await client.GetManagedAccountPasswordById(accountId);
                        Console.WriteLine($"Retrieved password for account ID {accountId}");
                        Console.WriteLine($"Password: {password.Password.Substring(0, 1)}*****");
                        Console.WriteLine($"Request ID: {password.RequestId}");
                        Console.WriteLine($"Expires: {password.ExpirationDate}");

                        // Test checking in the password
                        Console.WriteLine($"\nTesting CheckInPassword for request ID: {password.RequestId}...");
                        var checkInResult = await client.CheckInPassword(password.RequestId, "Test completed");
                        Console.WriteLine($"Check-in result: {(checkInResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving password: {ex.Message}");
                    }
                }

                // Test getting a specific managed account by name
                if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(systemName))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting GetManagedAccountByName with name: {accountName} on system: {systemName}...");
                        var account = await client.GetManagedAccountByName(accountName, systemName);
                        Console.WriteLine($"Found account: {account.AccountName} (ID: {account.ManagedAccountId})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving account: {ex.Message}");
                    }
                }

                // Test getting accounts for a specific system
                if (!string.IsNullOrEmpty(systemId))
                {
                    Console.WriteLine($"\nTesting GetManagedAccounts for system ID: {systemId}...");
                    var systemAccounts = await client.GetManagedAccounts(systemId);
                    Console.WriteLine($"Found {systemAccounts.Count()} accounts for system ID {systemId}");
                }

                // Test getting a specific account by system ID and account name
                if (!string.IsNullOrEmpty(systemId) && !string.IsNullOrEmpty(accountName))
                {
                    Console.WriteLine($"\nTesting GetManagedAccounts with system ID: {systemId} and account name: {accountName}...");
                    var specificAccount = await client.GetManagedAccounts(systemId, accountName);
                    var account = specificAccount.FirstOrDefault();
                    if (account != null)
                    {
                        Console.WriteLine($"Found account: {account.AccountName} (ID: {account.ManagedAccountId})");
                    }
                    else
                    {
                        Console.WriteLine($"Account with name {accountName} not found on system {systemId}");
                    }
                }

                // Sign out
                Console.WriteLine("\nTesting SignOut...");
                var signOutResult = await client.SignOut();
                Console.WriteLine($"Sign-out result: {(signOutResult ? "Success" : "Failed")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

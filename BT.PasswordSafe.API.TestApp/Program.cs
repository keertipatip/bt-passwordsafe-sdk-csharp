using BT.PasswordSafe.API;
using BT.PasswordSafe.API.Extensions;
using BT.PasswordSafe.API.Interfaces;
using BT.PasswordSafe.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BT.PasswordSafe.API.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("BT.PasswordSafe.API Test Application");
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
                var requestId = testSettings["RequestId"];
                var reason = testSettings["Reason"];

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
                        var password = await client.GetManagedAccountPasswordById(accountId, reason);
                        Console.WriteLine($"Retrieved password for account ID {accountId}");
                        
                        // Add null check for password.Password
                        if (!string.IsNullOrEmpty(password.Password))
                        {
                            Console.WriteLine($"Password: {password.Password.Substring(0, 1)}*****");
                        }
                        else
                        {
                            Console.WriteLine("Password: [Empty]");
                        }
                        
                        Console.WriteLine($"Request ID: {password.RequestId}");
                        Console.WriteLine($"Expires: {password.ExpirationDate}");

                        // Test GetManagedAccountPasswordByRequestId
                        if (!string.IsNullOrEmpty(password.RequestId))
                        {
                            Console.WriteLine($"\nTesting GetManagedAccountPasswordByRequestId with request ID: {password.RequestId}...");
                            var passwordByRequestId = await client.GetManagedAccountPasswordByRequestId(password.RequestId, reason);
                            Console.WriteLine($"Retrieved password using request ID {password.RequestId}");
                            
                            if (!string.IsNullOrEmpty(passwordByRequestId.Password))
                            {
                                Console.WriteLine($"Password: {passwordByRequestId.Password.Substring(0, 1)}*****");
                            }
                            else
                            {
                                Console.WriteLine("Password: [Empty]");
                            }
                        }

                        // Test checking in the password
                        // Add null check for password.RequestId
                        if (!string.IsNullOrEmpty(password.RequestId))
                        {
                            Console.WriteLine($"\nTesting CheckInPassword for request ID: {password.RequestId}...");
                            var checkInResult = await client.CheckInPassword(password.RequestId, "Test completed");
                            Console.WriteLine($"Check-in result: {(checkInResult ? "Success" : "Failed")}");
                        }
                        else
                        {
                            Console.WriteLine("\nSkipping CheckInPassword - Request ID is null");
                        }
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

                // Test getting managed account password by name
                if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(systemName))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting GetManagedAccountPasswordByName with name: {accountName} on system: {systemName}...");
                        var passwordByName = await client.GetManagedAccountPasswordByName(accountName, systemName, null, false, reason);
                        Console.WriteLine($"Retrieved password for account '{accountName}' on system '{systemName}'");
                        if (!string.IsNullOrEmpty(passwordByName.Password))
                        {
                            Console.WriteLine($"Password: {passwordByName.Password.Substring(0, 1)}*****");
                        }
                        else
                        {
                            Console.WriteLine("Password: [Empty]");
                        }
                        Console.WriteLine($"Request ID: {passwordByName.RequestId}");
                        Console.WriteLine($"Expires: {passwordByName.ExpirationDate}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving password by name: {ex.Message}");
                    }
                }

                // Test getting managed account password by request ID from config
                if (!string.IsNullOrEmpty(requestId))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting GetManagedAccountPasswordByRequestId with request ID: {requestId}...");
                        var passwordByRequestId = await client.GetManagedAccountPasswordByRequestId(requestId, reason);
                        Console.WriteLine($"Retrieved password using request ID {requestId}");
                        if (!string.IsNullOrEmpty(passwordByRequestId.Password))
                        {
                            Console.WriteLine($"Password: {passwordByRequestId.Password.Substring(0, 1)}*****");
                        }
                        else
                        {
                            Console.WriteLine("Password: [Empty]");
                        }
                        Console.WriteLine($"Expires: {passwordByRequestId.ExpirationDate}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving password by request ID: {ex.Message}");
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

                // Test getting a secret by ID (example GUID, replace with a real one for your environment)
                var exampleSecretId = Guid.Empty;
                var secretIdSetting = testSettings["SecretId"];
                if (!string.IsNullOrEmpty(secretIdSetting) && Guid.TryParse(secretIdSetting, out var parsedSecretId))
                {
                    exampleSecretId = parsedSecretId;
                }
                if (exampleSecretId != Guid.Empty)
                {
                    Console.WriteLine($"\nTesting GetSecretById with ID: {exampleSecretId}...");
                    var secretById = await client.GetSecretById(exampleSecretId);
                    if (secretById != null)
                    {
                        Console.WriteLine($"Found secret: {secretById.Title} (ID: {secretById.Id})");
                    }
                    else
                    {
                        Console.WriteLine($"Secret with ID {exampleSecretId} not found");
                    }
                }

                // Test getting a secret by name (title)
                var secretName = testSettings["SecretName"];
                if (!string.IsNullOrEmpty(secretName))
                {
                    Console.WriteLine($"\nTesting GetSecretByName with name: {secretName}...");
                    var secretByName = await client.GetSecretByName(secretName);
                    if (secretByName != null)
                    {
                        Console.WriteLine($"Found secret: {secretByName.Title} (ID: {secretByName.Id}) (Password: {secretByName.Password})");
                    }
                    else
                    {
                        Console.WriteLine($"Secret with name '{secretName}' not found");
                    }
                }

                // Test TestCredentialByAccountName
                if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(systemName))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting TestCredentialByAccountName with name: {accountName} on system: {systemName}...");
                        var testCredentialByNameResult = await client.TestCredentialByAccountName(accountName, systemName);
                        Console.WriteLine($"Test credential by name result: {(testCredentialByNameResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error testing credentials by name: {ex.Message}");
                    }
                }

                // Test ChangeCredentialByAccountID
                if (!string.IsNullOrEmpty(accountId))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting ChangeCredentialByAccountID with ID: {accountId}...");
                        Console.WriteLine("This will queue the credential change to run in the background.");
                        await client.ChangeCredentialByAccountID(accountId, true);
                        Console.WriteLine("Credential change queued successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error changing credentials by ID: {ex.Message}");
                    }
                }

                // Test ChangeCredentialByAccountName
                if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(systemName))
                {
                    try
                    {
                        Console.WriteLine($"\nTesting ChangeCredentialByAccountName with name: {accountName} on system: {systemName}...");
                        Console.WriteLine("This will queue the credential change to run in the background.");
                        await client.ChangeCredentialByAccountName(accountName, systemName, null, false, true);
                        Console.WriteLine("Credential change queued successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error changing credentials by name: {ex.Message}");
                    }
                }

                
                // Sign out
                Console.WriteLine("\nTesting SignOut...");
                var signOutResult = await client.SignOut();
                Console.WriteLine($"Sign-out result: {(signOutResult ? "Success" : "Failed")}");

                // Test the new credential testing and changing methods
                if (!string.IsNullOrEmpty(accountId))
                {
                    try
                    {
                        // Test TestCredentialByAccountID
                        Console.WriteLine($"\nTesting TestCredentialByAccountID with ID: {accountId}...");
                        var testCredentialResult = await client.TestCredentialByAccountID(accountId);
                        Console.WriteLine($"Test credential result: {(testCredentialResult ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error testing credentials by ID: {ex.Message}");
                    }
                }
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

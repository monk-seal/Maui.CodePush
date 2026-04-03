using System.CommandLine;
using System.Diagnostics;
using Maui.CodePush.Cli.Services;

namespace Maui.CodePush.Cli.Commands;

public static class LoginCommand
{
    public static Command Create()
    {
        var serverOption = new Option<string?>("--server", "-s") { Description = "Server URL (uses built-in default if omitted)" };
        var emailOption = new Option<string>("--email", "-e") { Description = "Account email", Required = true };
        var passwordOption = new Option<string>("--password") { Description = "Account password", Required = true };

        var command = new Command("login", "Authenticate with a CodePush server and save credentials")
        {
            serverOption, emailOption, passwordOption
        };

        command.SetAction(async (parseResult, _) =>
        {
            var server = parseResult.GetValue(serverOption);
            var email = parseResult.GetValue(emailOption)!;
            var password = parseResult.GetValue(passwordOption)!;

            try
            {
                var configManager = new ConfigManager();
                var loaded = configManager.TryLoadConfig();

                server ??= loaded?.Config.ServerUrl ?? CliSettings.DefaultServerUrl;
                if (string.IsNullOrEmpty(server))
                {
                    ConsoleUI.Error("Server URL required. Use --server or set serverUrl in .codepush.json");
                    return;
                }

                var client = new ServerClient(server);

                var result = await ConsoleUI.SpinnerAsync("Logging in",
                    async () => await client.LoginAsync(email, password));

                var token = result.GetProperty("token").GetString();

                var authedClient = new ServerClient(server, token: token);
                var me = await authedClient.GetMeAsync();
                var apiKey = me.GetProperty("apiKey").GetString();

                var config = loaded?.Config ?? new Models.CodePushConfig();
                var dir = loaded?.ProjectDir ?? Directory.GetCurrentDirectory();

                config.ServerUrl = server;
                config.Token = token;
                config.ApiKey = apiKey;
                configManager.CreateConfig(dir, config);

                ConsoleUI.Blank();
                ConsoleUI.Success("Authenticated successfully");
                ConsoleUI.Detail("Server", server);
                ConsoleUI.Detail("Email", email);
                if (apiKey?.Length > 16)
                    ConsoleUI.Detail("API Key", $"{apiKey[..16]}...");
                ConsoleUI.Blank();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
            }
        });

        return command;
    }

    public static Command CreateRegisterCommand()
    {
        var command = new Command("register", "Create an account on the Monkseal website");

        command.SetAction((_, _) =>
        {
            const string url = "https://monkseal.dev/register";
            ConsoleUI.Info($"Opening {url}...");

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                ConsoleUI.Success("Browser opened. Complete registration on the website, then run: codepush login");
            }
            catch
            {
                ConsoleUI.Info($"Open this URL in your browser: {url}");
            }

            ConsoleUI.Blank();
            return Task.CompletedTask;
        });

        return command;
    }
}

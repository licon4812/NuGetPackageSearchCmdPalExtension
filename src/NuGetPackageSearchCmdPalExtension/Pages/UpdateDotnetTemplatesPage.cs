using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace NuGetPackageSearchCmdPalExtension.Pages
{
    internal sealed partial class UpdateDotnetTemplatesPage : ListPage
    {
        public UpdateDotnetTemplatesPage()
        {
            // Retrieve the app version
            var version = Package.Current.Id.Version;
            var appVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            Icon = new IconInfo("\uE773");
            Title = $"NuGet Package Search Extension - v{appVersion}";
            Name = "Update";
        }

        public override IListItem[] GetItems()
        {
            var items = new List<IListItem>();

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "new update --check-only",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Debug.WriteLine("dotnet new update --check-only output:");
                Debug.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.WriteLine("dotnet new update --check-only error:");
                    Debug.WriteLine(error);
                }

                // Parse the table section
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                int tableStart = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("Package", StringComparison.Ordinal) &&
                        lines[i].Contains("Current") && lines[i].Contains("Latest"))
                    {
                        tableStart = i + 2; // Skip header and separator
                        break;
                    }
                }

                if (tableStart != -1)
                {
                    for (int i = tableStart; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line) ||
                            line.StartsWith("To update the package use:", StringComparison.Ordinal) ||
                            line.StartsWith("To update all the packages use:", StringComparison.Ordinal))
                            break;

                        // Split by whitespace, but only for the first 3 columns
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var package = parts[0];
                            var current = parts[1];
                            var latest = parts[2];

                            items.Add(new ListItem
                            {
                                Title = package,
                                Subtitle = $"Current: {current} → Latest: {latest}",
                                Icon = new IconInfo("\uE7B8"),
                                Command = new AnonymousCommand(UpdateTemplate(package, latest))
                                {
                                    Name = "Update Template",
                                    Icon = new IconInfo("\uE896")
                                },
                                MoreCommands = [
                                    new CommandContextItem("Update All Templates",name:"Update All Templates", action: UpdateAllTemplates())
                                    {
                                        Icon = new IconInfo("\uE896")
                                    }
                                ]
                            });
                        }
                    }
                }

                if (items.Count == 0)
                {
                    items.Add(new ListItem
                    {
                        Title = "No template updates available.",
                        Icon = new IconInfo("\uE8FB"),
                        Command = new NoOpCommand()
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in GetItems: " + ex);

                items.Add(new ListItem
                {
                    Title = "Error checking for template updates",
                    Subtitle = ex.Message,
                    Icon = new IconInfo("\uEA39"),
                    Command = new NoOpCommand()
                });
            }

            return [.. items];
        }

        public static Action UpdateTemplate(string packageName, string version)
        {
            return () =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"new install {packageName}::{version} --force",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                process.WaitForExit();
            };
        }

        public static Action UpdateAllTemplates()
        {
            return () =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "new update",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                process.WaitForExit();
            };
        }
    }
}

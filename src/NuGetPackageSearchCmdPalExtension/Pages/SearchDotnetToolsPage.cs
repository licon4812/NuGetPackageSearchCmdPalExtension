using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Windows.ApplicationModel;

namespace NuGetPackageSearchCmdPalExtension.Pages
{
    internal sealed partial class SearchDotnetToolsPage : DynamicListPage, IDisposable
    {
        private bool _isError;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly BufferBlock<string> _searchTextBuffer = new();
        private IReadOnlyList<ListItem> _results = [];

        public SearchDotnetToolsPage()
        {
            // Retrieve the app version
            var version = Package.Current.Id.Version;
            var appVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            Icon = new IconInfo("\uE773");
            Title = $"NuGet Package Search Extension - v{appVersion}";
            Name = "Search";

            // Configure the search processing pipeline
            Task.Run(async () =>
            {
                while (await _searchTextBuffer.OutputAvailableAsync(_cancellationTokenSource.Token))
                {
                    var searchText = await _searchTextBuffer.ReceiveAsync(_cancellationTokenSource.Token);
                    IsLoading = true;
                    try
                    {
                        _results = await ProcessSearchAsync(searchText, _cancellationTokenSource.Token);
                        _isError = false;
                    }
                    catch
                    {
                        _isError = true;
                        _results = [];
                    }
                    finally
                    {
                        IsLoading = false;
                        RaiseItemsChanged(_results!.Count);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public override IListItem[] GetItems()
        {
            return [.. _results];
        }

        public override void UpdateSearchText(string oldSearch, string newSearch)
        {
            if (newSearch == oldSearch) return;
            _searchTextBuffer.Post(newSearch);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public override ICommandItem? EmptyContent
        {
            get
            {
                if (_isError)
                {
                    return new CommandItem
                    {
                        Title = "Error loading dotnet tools",
                        Icon = new IconInfo("\uea39"),
                        Subtitle = "An error occurred while fetching dotnet tools.",
                        Command = new NoOpCommand()
                    };
                }

                return new CommandItem
                {
                    Title = "Search for a dotnet tools",
                    Icon = new IconInfo("\uea39"),
                    Subtitle = "Search for a dotnet tools",
                    Command = new NoOpCommand()
                }; ;
            }
        }

        private static async Task<IReadOnlyList<ListItem>> ProcessSearchAsync(string searchText, CancellationToken cancellationToken)
        {
            await Task.Yield(); // Simulate asynchronous behavior

            var url = $"https://azuresearch-usnc.nuget.org/query?take=20&packageType=DotnetTool&q={Uri.EscapeDataString(searchText)}";
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var results = new List<ListItem>();

            if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind != System.Text.Json.JsonValueKind.Array) return results;

            foreach (var package in dataElement.EnumerateArray())
            {
                var id = package.GetProperty("id").GetString();
                var version = package.GetProperty("version").GetString();
                string? iconUrl = null;
                if (package.TryGetProperty("iconUrl", out var iconUrlProp) && iconUrlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    iconUrl = iconUrlProp.GetString();
                }
                // Fallback to a glyph if iconUrl is missing
                var icon = !string.IsNullOrEmpty(iconUrl) ? new IconInfo(iconUrl) : new IconInfo("\uE7B8");
                results.Add(new ListItem
                {
                    Title = id!,
                    Subtitle = version!,
                    Icon = icon,
                    Command = new CopyTextCommand($"dotnet tool install --global {id} --version {version}") { Name = "Copy global install Command" },
                    MoreCommands =
                    [
                        new CommandContextItem(new AnonymousCommand(() =>
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/k dotnet tool install --global {id} --version {version}", // Use /k to keep the shell open
                                UseShellExecute = true,
                                CreateNoWindow = false
                            };

                            using var process = new Process();
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                        }){Icon = new IconInfo("\uE896"), Name = "Install tool globally"}),
                        new CommandContextItem(new CopyTextCommand($"dotnet tool install --local {id} --version {version}"){Name = "Copy local install command"}),
                        new CommandContextItem(new CopyTextCommand($"#tool dotnet:?package={id}&version={version}"){Name = "Copy cake tool"}),
                        new CommandContextItem(new CopyTextCommand($"nuke :add-package {id} --version {version}"){Name = "Copy NUKE"})

                    ]
                });
            }

            return results;
        }
    }
}

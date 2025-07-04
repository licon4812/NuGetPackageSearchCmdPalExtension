// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Windows.ApplicationModel;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NuGetPackageSearchCmdPalExtension.Pages;

internal sealed partial class SearchNuGetPackagesPage : DynamicListPage, IDisposable
{
    private bool _isError;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly BufferBlock<string> _searchTextBuffer = new();
    private IReadOnlyList<ListItem> _results = [];

    public SearchNuGetPackagesPage()
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
                    Title = "Error loading nuget packages",
                    Icon = new IconInfo("\uea39"),
                    Subtitle = "An error occurred while fetching nuget packages.",
                    Command = new NoOpCommand()
                };
            }

            return new CommandItem
            {
                Title = "Search for a nuget package",
                Icon = new IconInfo("\uea39"),
                Subtitle = "Search for a nuget package",
                Command = new NoOpCommand()
            }; ;
        }
    }

    private static async Task<IReadOnlyList<ListItem>> ProcessSearchAsync(string searchText, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous behavior

        var url = $"https://azuresearch-usnc.nuget.org/query?take=20&q={Uri.EscapeDataString(searchText)}";
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
                Title = id,
                Subtitle = version,
                Icon = icon,
                Command = new CopyTextCommand(id ?? string.Empty) { Name = "Copy Package Name" },
                MoreCommands =
                [
                    new CommandContextItem(new CopyTextCommand($"<PackageReference Include=\"{id}\" Version=\"{version}\" />"){Name = "Copy Package Reference"}),
                    new CommandContextItem(new CopyTextCommand($"dotnet add package {id} --version {version}"){Name = "Copy .NET CLI command"}),
                    new CommandContextItem(new CopyTextCommand($"NuGet\\Install-Package {id} -Version {version}"){Name = "Copy Nuget Package Manager command"}),
                    new CommandContextItem(new CopyTextCommand($"#r \"nuget: {id}, {version}\""){Name = "Copy Script & Interactive"}),
                    new CommandContextItem(new CopyTextCommand($"#:package {id}@{version}"){Name = "Copy File-based Apps"}),
                    new CommandContextItem(new CopyTextCommand($"#addin nuget:?package={id}={version}"){Name = "Copy Cake Addin"}),
                    new CommandContextItem(new CopyTextCommand($"#tool nuget:?package={id}&version={version}"){Name = "Copy Cake Tool"})

                ]
            });
        }

        return results;
    }
}

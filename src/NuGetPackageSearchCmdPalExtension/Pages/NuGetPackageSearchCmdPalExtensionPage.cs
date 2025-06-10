// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.ApplicationModel;

namespace NuGetPackageSearchCmdPalExtension;

internal sealed partial class NuGetPackageSearchCmdPalExtensionPage : DynamicListPage, IDisposable
{
    public NuGetPackageSearchCmdPalExtensionPage()
    {
        // Retrieve the app version
        var version = Package.Current.Id.Version;
        var appVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

        Icon = new IconInfo("\uE773");
        Title = $"NuGet Package Search Extension - v{appVersion}";
        Name = "Search";
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new NoOpCommand()) { Title = "TODO: Implement your extension here" }
        ];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

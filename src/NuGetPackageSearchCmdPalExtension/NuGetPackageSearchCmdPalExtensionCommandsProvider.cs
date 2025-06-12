// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NuGetPackageSearchCmdPalExtension;

public partial class NuGetPackageSearchCmdPalExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public NuGetPackageSearchCmdPalExtensionCommandsProvider()
    {
        DisplayName = "NuGet Package Search Extension";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new Pages.SearchNuGetPackagesPage()) { Title = "Search NuGet Packages" },
            new CommandItem(new Pages.SearchDotnetTemplatesPage()) {Title = "Search Dotnet Templates"}
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}

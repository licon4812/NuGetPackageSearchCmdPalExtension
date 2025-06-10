// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace NuGetPackageSearchCmdPalExtension;

[Guid("1ee4b1ff-1f87-4c96-90af-0fb6f415d3b6")]
public sealed partial class NuGetPackageSearchCmdPalExtension(ManualResetEvent extensionDisposedEvent)
    : IExtension, IDisposable
{
    private readonly NuGetPackageSearchCmdPalExtensionCommandsProvider _provider = new();

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => extensionDisposedEvent.Set();
}

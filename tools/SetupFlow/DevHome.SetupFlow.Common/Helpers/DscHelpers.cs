﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Common.Helpers;

/// <summary>
/// Helper class for DSC related constants.
/// </summary>
public static class DscHelpers
{
    public const string GitCloneDscResource = "GitDsc/GitClone";

    public const string GitDscWinGetId = "Git.Git";

    public const string GitName = "Git";

    public const string DscSourceNameForWinGet = "winget";

    public const string WinGetDscResource = "Microsoft.WinGet.DSC/WinGetPackage";

    public const string WinGetConfigureVersion = "0.2.0";

    // Banner to be shown on top of the generated winget config file.
    public const string DevHomeHeaderBanner =
@"# yaml-language-server: $schema=https://aka.ms/configuration-dsc-schema/0.2
# Reference: https://github.com/microsoft/winget-create#building-the-client
# WinGet Configure file Generated By Dev Home.";
}

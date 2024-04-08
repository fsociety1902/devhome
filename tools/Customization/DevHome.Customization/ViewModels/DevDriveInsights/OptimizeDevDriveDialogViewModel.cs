// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Media.Protection;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.Customization.ViewModels.DevDriveInsights;

/// <summary>
/// View model for the card that represents a dev drive on the dev drive insights page.
/// </summary>
public partial class OptimizeDevDriveDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private List<string> _existingDevDriveLetters;

    [ObservableProperty]
    private string _exampleDevDriveLocation;

    [ObservableProperty]
    private string _chooseDirectoryPromptText;

    [ObservableProperty]
    private string _makeChangesText;

    [ObservableProperty]
    private string _existingCacheLocation;

    [ObservableProperty]
    private string _environmentVariableToBeSet;

    [ObservableProperty]
    private string _optimizeDevDriveDialogDescription;

    [ObservableProperty]
    private string _directoryPathTextBox;

    public OptimizeDevDriveDialogViewModel(
        string existingCacheLocation,
        string environmentVariableToBeSet,
        string exampleDevDriveLocation,
        List<string> existingDevDriveLetters)
    {
        DirectoryPathTextBox = string.Empty;
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        ExistingDevDriveLetters = existingDevDriveLetters;
        ExampleDevDriveLocation = stringResource.GetLocalized("ExampleText") + exampleDevDriveLocation;
        ChooseDirectoryPromptText = stringResource.GetLocalized("ChooseDirectoryPromptText");
        MakeChangesText = stringResource.GetLocalized("MakeChangesText");
        ExistingCacheLocation = existingCacheLocation;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
        OptimizeDevDriveDialogDescription = stringResource.GetLocalized("OptimizeDevDriveDialogDescription/Text", ExistingCacheLocation, EnvironmentVariableToBeSet);
    }

    [RelayCommand]
    private void DirectoryPathChanged(string text)
    {
        DirectoryPathTextBox = text;
    }

    [RelayCommand]
    private async Task BrowseButtonClick(object sender)
    {
        // Create a folder picker dialog
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List,
        };

        folderPicker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Microsoft.UI.Xaml.Application.Current.GetService<WindowEx>().GetWindowHandle());
        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null)
        {
            DirectoryPathTextBox = folder.Path;
        }
    }

    private int MoveDirectory(string sourceDirectory, string targetDirectory)
    {
        try
        {
            // Create the target directory if it doesn't exist
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Get all files and subdirectories in the source directory
            var files = Directory.GetFiles(sourceDirectory);
            var subdirectories = Directory.GetDirectories(sourceDirectory);

            // Move files
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var targetFilePath = Path.Combine(targetDirectory, fileName);
                File.Move(file, targetFilePath);
            }

            // Recursively move subdirectories
            foreach (var subdirectory in subdirectories)
            {
                var subdirectoryName = Path.GetFileName(subdirectory);
                var targetSubdirectoryPath = Path.Combine(targetDirectory, subdirectoryName);
                MoveDirectory(subdirectory, targetSubdirectoryPath);
            }

            // Delete the source directory
            Directory.Delete(sourceDirectory, true);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in MoveDirectory. Error: {ex}");
            TelemetryFactory.Get<ITelemetry>().LogError("DevDriveInsights_PackageCacheMove_Error", LogLevel.Critical, new ExceptionEvent(ex.HResult, sourceDirectory));
            return ex.HResult;
        }
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.User);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in SetEnvironmentVariable. Error: {ex}");
        }
    }

    private bool ChosenDirectoryInDevDrive(string directoryPath)
    {
        foreach (var devDriveLetter in ExistingDevDriveLetters)
        {
            if (directoryPath.StartsWith(devDriveLetter + ":", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private void DirectoryInputConfirmed()
    {
        var directoryPath = DirectoryPathTextBox;

        if (!string.IsNullOrEmpty(directoryPath))
        {
            // Handle the selected folder
            // TODO: If chosen folder not a dev drive location, currently we no-op and log the error. Instead we should display the error.
            if (ChosenDirectoryInDevDrive(directoryPath))
            {
                if (MoveDirectory(ExistingCacheLocation, directoryPath) == 0)
                {
                    SetEnvironmentVariable(EnvironmentVariableToBeSet, directoryPath);
                    Log.Debug($"Moved cache from {ExistingCacheLocation} to {directoryPath}");
                    TelemetryFactory.Get<ITelemetry>().Log("DevDriveInsights_PackageCacheMoved_Event", LogLevel.Critical, new ExceptionEvent(0, ExistingCacheLocation));
                }
            }
            else
            {
                Log.Error($"Chosen directory {directoryPath} not on a dev drive.");
            }
        }
    }
}

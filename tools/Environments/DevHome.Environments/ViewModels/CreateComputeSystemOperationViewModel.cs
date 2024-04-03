﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Environments.ViewModels;

public partial class CreateComputeSystemOperationViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemViewModel));

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly StringResource _stringResource;

    private readonly string _deletionUniCodeCharacter = "\uE74D";

    public string Name => Operation.EnvironmentName;

    private readonly Action _removalAction;

    public CreateComputeSystemOperation Operation { get; }

    // Dot button operations
    public ObservableCollection<OperationsViewModel> DotOperations { get; set; }

    [ObservableProperty]
    private ComputeSystemState _state;

    [ObservableProperty]
    private string _uiMessageToDisplay = string.Empty;

    [ObservableProperty]
    private bool _isCreationInProgress;

    [ObservableProperty]
    private CardStateColor _stateColor;

    public BitmapImage? HeaderImage { get; set; } = new();

    public BitmapImage? BodyImage { get; set; } = new();

    public string PackageFullName { get; set; } = string.Empty;

    public string ProviderDisplayName { get; set; } = string.Empty;

    public ComputeSystem? ComputeSystem { get; private set; }

    public CreateComputeSystemOperationViewModel(StringResource stringResource, Action removalAction, CreateComputeSystemOperation operation)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _removalAction = removalAction;
        _stringResource = stringResource;
        ProviderDisplayName = operation.ProviderDetails.ComputeSystemProvider.DisplayName;
        PackageFullName = operation.ProviderDetails.ExtensionWrapper.PackageFullName;
        Operation = operation;
        UpdateUiMessage(Operation.LastProgressMessage, Operation.LastProgressPercentage);
        IsCreationInProgress = true;
        Operation.Completed += OnOperationCompleted;
        Operation.Progress += OnOperationProgressChanged;
        State = ComputeSystemState.Creating;
        StateColor = CardStateColor.Caution;

        DotOperations = new ObservableCollection<OperationsViewModel>() { new(_stringResource.GetLocalized("DeleteButtonTextForCreateComputeSystem"), _deletionUniCodeCharacter, _removalAction) };
        HeaderImage = CardProperty.ConvertMsResourceToIcon(operation.ProviderDetails.ComputeSystemProvider.Icon, PackageFullName);

        // If the operation is already completed update the status
        if (operation.CreateComputeSystemResult != null)
        {
            UpdateStatusIfCompleted(operation.CreateComputeSystemResult);
        }
    }

    private void OnOperationCompleted(object sender, CreateComputeSystemResult createComputeSystemResult)
    {
        UpdateStatusIfCompleted(createComputeSystemResult);
    }

    private void UpdateStatusIfCompleted(CreateComputeSystemResult createComputeSystemResult)
    {
        _dispatcher.TryEnqueue(() =>
        {
            // Update the creation status
            IsCreationInProgress = false;
            if (createComputeSystemResult.Result.Status == ProviderOperationStatus.Success)
            {
                UpdateUiMessage(_stringResource.GetLocalized("SuccessMessageForCreateComputeSystem"), 0);
                ComputeSystem = new(createComputeSystemResult.ComputeSystem);
                State = ComputeSystemState.Created;
                StateColor = CardStateColor.Success;
            }
            else
            {
                UpdateUiMessage(_stringResource.GetLocalized("FailureMessageForCreateComputeSystem", createComputeSystemResult.Result.DisplayMessage), 0);
                State = ComputeSystemState.Unknown;
                StateColor = CardStateColor.Failure;
            }
        });
    }

    private void OnOperationProgressChanged(object sender, CreateComputeSystemProgressEventArgs args)
    {
        UpdateUiMessage(args.Status, args.PercentageCompleted);
    }

    public void RemoveEventHandlers()
    {
        Operation.Completed -= OnOperationCompleted;
        Operation.Progress -= OnOperationProgressChanged;
    }

    private void UpdateUiMessage(string operationStatus, uint percentage)
    {
        _dispatcher.TryEnqueue(() =>
        {
            var percentageString = percentage == 0 ? string.Empty : $"({percentage}%)";
            UiMessageToDisplay = $"{operationStatus} {percentageString}";
        });
    }
}
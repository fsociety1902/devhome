﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface for creating response to host request.
/// </summary>
public interface IHostResponse
{
    string RequestId { get; set; }

    string RequestType { get; set; }

    string ResponseId { get; set; }

    string ResponseType { get; set; }

    uint Status { get; set; }

    DateTime Timestamp { get; set; }

    IResponseMessage GetResponseMessage();
}

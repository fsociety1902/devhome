﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using SampleExtension.Providers;
using Serilog;

namespace SampleExtension;

public class SettingsProvider2 : ISettingsProvider2
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(SettingsProvider2)));

    private static readonly ILogger _log = _logger.Value;
    private readonly WebViewResult _webViewResult;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public DisplayType DisplayType => DisplayType.WebView2;

    public SettingsProvider2()
    {
        _webViewResult = new WebViewResult(new NotImplementedException(), "No WebView was provided for the SettingsProvider2");
    }

    public SettingsProvider2(WebViewResult webViewResult)
    {
        _webViewResult = webViewResult;
        if (!string.IsNullOrEmpty(webViewResult.Url))
        {
            _log.Information($"SettingsProvider2 URL: {webViewResult.Url}");
        }
        else
        {
            _log.Debug($"Web URL was null or empty");
            _webViewResult = new WebViewResult(new NotImplementedException(), "WebView URL was null or empty");
        }
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");

        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public WebViewResult GetWebView()
    {
        _log.Debug($"GetWebView");
        return _webViewResult;
    }
}

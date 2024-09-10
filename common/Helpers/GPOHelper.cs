// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace DevHome.Common.Helpers;

public class GPOHelper
{
    private enum GpoRuleConfigured
    {
        WrongValue = -3, // The policy is set to an unrecognized value
        Unavailable = -2, // Couldn't access registry
        NotConfigured = -1, // Policy is not configured
        Disabled = 0, // Policy is disabled
        Enabled = 1, // Policy is enabled
    }

    // Registry path where gpo policy values are stored.
    private const string PoliciesScopeMachine = "HKEY_LOCAL_MACHINE";
    private const string PoliciesPath = @"\SOFTWARE\Policies\DevHome";

    // Registry value names
    private const string PolicyConfigureEnabledDevHome = "ConfigureEnabledDevHome";
    private const string PolicyConfigureEnabledMachineConfiguration = "ConfigureEnabledMachineConfiguration";
    private const string PolicyConfigureEnabledEnvironments = "ConfigureEnabledEnvironments";
    private const string PolicyConfigureEnabledExperimentalFeatures = "ConfigureEnabledExperimentalFeatures";
    private const string PolicyConfigureHiddenDevHome = "ConfigureHiddenDevHome";

    private GpoRuleConfigured GetConfiguredValue(string registryValueName)
    {
        var value = Registry.GetValue(PoliciesScopeMachine + PoliciesPath, registryValueName, GpoRuleConfigured.NotConfigured);
        value ??= GpoRuleConfigured.NotConfigured;

        return (GpoRuleConfigured)value;
    }

    private bool EvaluateConfiguredValue(string registryValueName, GpoRuleConfigured defaultValue)
    {
        var configuredValue = GetConfiguredValue(registryValueName);
        if (configuredValue < 0)
        {
            configuredValue = defaultValue;
        }

        return configuredValue == GpoRuleConfigured.Enabled;
    }

    public bool GetConfiguredEnabledDevHomeValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledDevHome, defaultValue);
    }

    public bool GetConfiguredEnabledMachineConfigurationValue()
    {
        ////var defaultValue = GpoRuleConfigured.Enabled;
        ////return EvaluateConfiguredValue(PolicyConfigureEnabledMachineConfiguration, defaultValue);
        return false;
    }

    public bool GetConfiguredEnabledEnvironmentsValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledEnvironments, defaultValue);
    }

    public bool GetConfiguredEnabledExperimentalFeaturesValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledExperimentalFeatures, defaultValue);
    }

    public bool GetConfiguredHiddenDevHomeValue()
    {
        var defaultValue = GpoRuleConfigured.Disabled;
        return EvaluateConfiguredValue(PolicyConfigureHiddenDevHome, defaultValue);
    }
}

// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using HyperVExtension.DevSetupAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Windows.UI.Accessibility;

namespace DevSetupAgent.Test;

[TestClass]
public class DevSetupAgentIntegrationTest
{
    protected IHost TestHost
    {
        get; set;
    }

    public DevSetupAgentIntegrationTest()
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<DevAgentService>();
                services.AddSingleton<IRequestFactory, RequestFactory>();
                services.AddSingleton<IRegistryChannelSettings, TestRegistryChannelSettings>();
                services.AddSingleton<IHostChannel, HostRegistryChannel>();
                services.AddSingleton<IRequestManager, RequestManager>();
            }).Build();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"TEST", false);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        TestHost.GetService<DevAgentService>().StartAsync(CancellationToken.None);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        TestHost.GetService<DevAgentService>().StopAsync(CancellationToken.None).Wait();
    }

    [TestMethod]
    public void TestGetVersionRequest()
    {
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-100000000000}";
        inputkey.SetValue(messageId, $"{{\"RequestId\": \"{messageId}\", \"RequestType\": \"GetVersion\", \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"}}");

        Thread.Sleep(3000);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var responseMessage = (string?)outputKey.GetValue(messageId);
        Assert.IsNotNull(responseMessage);
        var json = JsonDocument.Parse(responseMessage).RootElement;
        Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

        // Check that the timestamp is within 5 second of the current
        var time = json.GetProperty("Timestamp").GetDateTime();
        var now = DateTime.UtcNow;
        Assert.IsTrue(now - time < TimeSpan.FromSeconds(5));

        var version = json.GetProperty("Version").GetString();
        Assert.AreEqual("0.0.1", version);

        // TODO: Check that the response message is deleted
    }

    [TestMethod]
    public void TestInvalidRequest()
    {
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-200000000000}";
        inputkey.SetValue(messageId, $"{{\"RequestId\": \"{messageId}\", \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"}}");

        Thread.Sleep(3000);

        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var responseMessage = (string?)outputKey.GetValue(messageId);
        Assert.IsNotNull(responseMessage);
        var json = JsonDocument.Parse(responseMessage).RootElement;
        Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

        // Check that the timestamp is within 5 second of the current
        var time = json.GetProperty("Timestamp").GetDateTime();
        var now = DateTime.UtcNow;
        Assert.IsTrue(now - time < TimeSpan.FromSeconds(5));

        var status = json.GetProperty("Status").GetUInt32();
        Assert.AreNotEqual(0, status);
    }

    /// <summary>
    /// Test that a simple Configure request can be sent to DevSetupEngine and that it responds with
    /// Progress and Completed results.
    /// Currently DevSetupEngine needs to be started manually from command line for this test.
    /// </summary>
    [TestMethod]
    public void TestConfigureRequest()
    {
        var yaml =
@"# yaml-language-server: $schema=https://aka.ms/configuration-dsc-schema/0.2
properties:
  assertions:
    - resource: Microsoft.Windows.Developer/OsVersion
      directives:
        description: Verify min OS version requirement
        allowPrerelease: true
      settings:
        MinVersion: '10.0.22000'
  resources:
    - resource: Microsoft.Windows.Developer/DeveloperMode
      directives:
        description: Enable Developer Mode
        allowPrerelease: true
      settings:
        Ensure: Present
  configurationVersion: 0.2.0";

        var noNewLinesYaml = yaml.Replace(System.Environment.NewLine, "\\n");

        yaml.ReplaceLineEndings("\r\n");
        var registryChannelSettings = TestHost.GetService<IRegistryChannelSettings>();
        var inputkey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.FromHostRegistryKeyPath);
        var messageId = "DevSetup{10000000-1000-1000-1000-100000000001}";
        var requestData =
            $"{{\"RequestId\": \"{messageId}\"," +
            $" \"RequestType\": \"Configure\", \"Timestamp\":\"2023-11-21T08:08:58.6287789Z\"," +
            $" \"Configure\": \"{noNewLinesYaml}\" }}";

        inputkey.SetValue(messageId, requestData);

        string? responseMessage = null;
        var outputKey = Registry.CurrentUser.CreateSubKey(registryChannelSettings.ToHostRegistryKeyPath);
        var waitTime = DateTime.Now + TimeSpan.FromMinutes(3);
        var foundCompletedMessage = false;
        while ((waitTime > DateTime.Now) && !foundCompletedMessage)
        {
            Thread.Sleep(1000);
            var valueNames = outputKey.GetValueNames();
            foreach (var valueName in valueNames)
            {
                System.Diagnostics.Trace.WriteLine($"Found response registry value '{valueName}'");

                responseMessage = (string?)outputKey.GetValue(valueName);
                if (responseMessage != null)
                {
                    var json = JsonDocument.Parse(responseMessage).RootElement;
                    Assert.AreEqual(messageId, json.GetProperty("RequestId").GetString());

                    var responseType = json.GetProperty("ResponseType").GetString();
                    if (responseType == "Completed")
                    {
                        var applyConfigurationResult = json.GetProperty("ApplyConfigurationResult").GetString();
                        Assert.IsNotNull(applyConfigurationResult);
                        System.Diagnostics.Trace.WriteLine(applyConfigurationResult);
                        foundCompletedMessage = true;
                    }
                    else if (responseType == "Progress")
                    {
                        var configurationSetChangeData = json.GetProperty("ConfigurationSetChangeData").GetString();
                        Assert.IsNotNull(configurationSetChangeData);
                        System.Diagnostics.Trace.WriteLine(configurationSetChangeData);
                    }
                    else
                    {
                        Assert.Fail($"Unexpected response type: {responseType}");
                    }
                }

                outputKey.DeleteValue(valueName);
            }
        }

        Assert.IsNotNull(responseMessage);
    }
}

// <copyright file="ChromeDriver.cs" company="WebDriver Committers">
// Licensed to the Software Freedom Conservancy (SFC) under one
// or more contributor license agreements. See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership. The SFC licenses this file
// to you under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Net;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;

namespace OpenQA.Selenium.Chromium
{
    public abstract class ChromiumDriver : RemoteWebDriver, ISupportsLogs
    {
        /// <summary>
        /// Accept untrusted SSL Certificates
        /// </summary>
        public static readonly bool AcceptUntrustedCertificates = true;

        private const string GetNetworkConditionsCommand = "getNetworkConditions";
        private const string SetNetworkConditionsCommand = "setNetworkConditions";
        private const string DeleteNetworkConditionsCommand = "deleteNetworkConditions";
        private const string SendChromeCommand = "sendChromeCommand";
        private const string SendChromeCommandWithResult = "sendChromeCommandWithResult";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumDriver"/> class using the specified
        /// <see cref="ChromiumDriverService"/> and options.
        /// </summary>
        /// <param name="service">The <see cref="ChromiumDriverService"/> to use.</param>
        /// <param name="options">The <see cref="ChromiumOptions"/> used to initialize the driver.</param>
        public ChromiumDriver(ChromiumDriverService service, ChromiumOptions options)
            : this(service, options, RemoteWebDriver.DefaultCommandTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumDriver"/> class using the specified <see cref="ChromiumDriverService"/>.
        /// </summary>
        /// <param name="service">The <see cref="ChromiumDriverService"/> to use.</param>
        /// <param name="options">The <see cref="ChromiumOptions"/> to be used with the Chrome driver.</param>
        /// <param name="commandTimeout">The maximum amount of time to wait for each command.</param>
        public ChromiumDriver(ChromiumDriverService service, ChromiumOptions options, TimeSpan commandTimeout)
            : base(new DriverServiceCommandExecutor(service, commandTimeout), ConvertOptionsToCapabilities(options))
        {
            // Add the custom commands unique to Chrome
            this.AddCustomChromeCommand(GetNetworkConditionsCommand, CommandInfo.GetCommand, "/session/{sessionId}/chromium/network_conditions");
            this.AddCustomChromeCommand(SetNetworkConditionsCommand, CommandInfo.PostCommand, "/session/{sessionId}/chromium/network_conditions");
            this.AddCustomChromeCommand(DeleteNetworkConditionsCommand, CommandInfo.DeleteCommand, "/session/{sessionId}/chromium/network_conditions");
            this.AddCustomChromeCommand(SendChromeCommand, CommandInfo.PostCommand, "/session/{sessionId}/chromium/send_command");
            this.AddCustomChromeCommand(SendChromeCommandWithResult, CommandInfo.PostCommand, "/session/{sessionId}/chromium/send_command_and_get_result");
        }

        /// <summary>
        /// Gets or sets the <see cref="IFileDetector"/> responsible for detecting
        /// sequences of keystrokes representing file paths and names.
        /// </summary>
        /// <remarks>The Chromium driver does not allow a file detector to be set,
        /// as the server component of the Chromium driver only
        /// allows uploads from the local computer environment. Attempting to set
        /// this property has no effect, but does not throw an exception. If you
        /// are attempting to run the Chromium driver remotely, use <see cref="RemoteWebDriver"/>
        /// in conjunction with a standalone WebDriver server.</remarks>
        public override IFileDetector FileDetector
        {
            get { return base.FileDetector; }
            set { }
        }

        /// <summary>
        /// Gets or sets the network condition emulation for Chromium.
        /// </summary>
        public ChromiumNetworkConditions NetworkConditions
        {
            get
            {
                Response response = this.Execute(GetNetworkConditionsCommand, null);
                return ChromiumNetworkConditions.FromDictionary(response.Value as Dictionary<string, object>);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "value must not be null");
                }

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters["network_conditions"] = value.ToDictionary();
                this.Execute(SetNetworkConditionsCommand, parameters);
            }
        }

        /// <summary>
        /// Executes a custom Chrome command.
        /// </summary>
        /// <param name="commandName">Name of the command to execute.</param>
        /// <param name="commandParameters">Parameters of the command to execute.</param>
        public void ExecuteChromeCommand(string commandName, Dictionary<string, object> commandParameters)
        {
            if (commandName == null)
            {
                throw new ArgumentNullException("commandName", "commandName must not be null");
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["cmd"] = commandName;
            parameters["params"] = commandParameters;
            this.Execute(SendChromeCommand, parameters);
        }

        public object ExecuteChromeCommandWithResult(string commandName, Dictionary<string, object> commandParameters)
        {
            if (commandName == null)
            {
                throw new ArgumentNullException("commandName", "commandName must not be null");
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["cmd"] = commandName;
            parameters["params"] = commandParameters;
            Response response = this.Execute(SendChromeCommandWithResult, parameters);
            return response.Value;
        }

        private static ICapabilities ConvertOptionsToCapabilities(ChromiumOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options", "options must not be null");
            }

            return options.ToCapabilities();
        }

        private void AddCustomChromeCommand(string commandName, string method, string resourcePath)
        {
            CommandInfo commandInfoToAdd = new CommandInfo(method, resourcePath);
            this.CommandExecutor.CommandInfoRepository.TryAddCommand(commandName, commandInfoToAdd);
        }
    }
}

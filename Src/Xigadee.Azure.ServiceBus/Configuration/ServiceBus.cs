﻿#region Copyright
// Copyright Hitachi Consulting
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.Azure.ServiceBus;

namespace Xigadee
{
    public static partial class AzureServiceBusExtensionMethods
    {
        /// <summary>
        /// The reserved keyword.
        /// </summary>
        public const string ServiceBus = "ServiceBus";

        /// <summary>
        /// The service bus connection configuration key
        /// </summary>
        [ConfigSettingKey(ServiceBus)]
        public const string KeyServiceBusConnection = "ServiceBusConnection";

        /// <summary>
        /// The service bus connection configuration value
        /// </summary>
        /// <param name="config">The configuration collection.</param>
        /// <returns>The defined connection value.</returns>
        [ConfigSetting(ServiceBus)]
        public static string ServiceBusConnection(this IEnvironmentConfiguration config) => config.PlatformOrConfigCache(KeyServiceBusConnection);

        /// <summary>
        /// This extension sets the fabric bridge Service Bus connection from the pipeline configuration.
        /// </summary>
        /// <typeparam name="P">The pipeline type.</typeparam>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="fabric">The fabric to configure from the pipeline.</param>
        /// <returns>Returns the pipeline</returns>
        public static P FabricConfigure<P>(this P pipeline, AzureServiceBusFabricBridge fabric) where P : IPipeline
        {
            var conn = pipeline.Configuration.ServiceBusConnection();

            fabric.Connection = new ServiceBusConnectionStringBuilder(conn);

            return pipeline;
        }
    }
}

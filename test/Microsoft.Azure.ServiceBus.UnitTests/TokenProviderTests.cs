﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus.UnitTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.ServiceBus.Primitives;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Xunit;

    public class TokenProviderTests : SenderReceiverClientTestBase
    {
        /// <summary>
        /// This test is for manual only purpose. Fill in the tenant-id, app-id and app-secret before running.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [DisplayTestMethodName]
        public async Task UseITokenProviderWithAad()
        {
            var tenantId = "";
            var aadAppId = "";
            var aadAppSecret = "";

            if (string.IsNullOrEmpty(tenantId))
            {
                TestUtility.Log($"Skipping test during scheduled runs.");
                return;
            }

            var authContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var cc = new ClientCredential(aadAppId, aadAppSecret);
            var tokenProvider = TokenProvider.CreateAadTokenProvider(authContext, cc);

            // Create new client with updated connection string.
            var csb = new ServiceBusConnectionStringBuilder(TestUtility.NamespaceConnectionString);
            var queueClient = new QueueClient(csb.Endpoint, csb.EntityPath, tokenProvider);

            // Send and receive messages.
            await this.PeekLockTestCase(queueClient.InnerSender, queueClient.InnerReceiver, 10);
        }

        [Fact]
        [DisplayTestMethodName]
        public async void SasTokenWithLargeExpiryTimeShouldBeAccepted()
        {
            var csb = new ServiceBusConnectionStringBuilder(TestUtility.NamespaceConnectionString);
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(csb.SasKeyName, csb.SasKey, TimeSpan.FromDays(100));
            var connection = new ServiceBusConnection(csb)
            {
                TokenProvider = tokenProvider
            };
            var receiver = new MessageReceiver(connection, TestConstants.NonPartitionedQueueName, ReceiveMode.PeekLock, RetryPolicy.Default);

            try
            {
                var msg = await receiver.ReceiveAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                await receiver.CloseAsync();
            }
        }
    }
}

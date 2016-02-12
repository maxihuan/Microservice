﻿#region using
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
#endregion
namespace Xigadee
{
    public static class BrokeredMessageHelper
    {
        public static void AssignMessageHelpers<C>(this AzureClientHolder<C,BrokeredMessage> client)
            where C : ClientEntity
        {
            client.MessagePack = Pack;
            client.MessageUnpack = Unpack;
            client.MessageSignal = MessageSignal;
        }

        #region MessagePack(ServiceMessage sMessage)
        /// <summary>
        /// This method packs the ServiceMessage in to the BrokeredMessage format
        /// for communication through the Azure Service Bus.
        /// </summary>
        /// <param name="sMessage">The ServiceMessage object to convert.</param>
        /// <returns>Returns a converted BrokeredMessage from transmission.</returns>
        public static BrokeredMessage Pack(TransmissionPayload payload)
        {
            ServiceMessage sMessage = payload.Message;
            BrokeredMessage bMessage;
            if (sMessage.Blob == null)
                bMessage = new BrokeredMessage();
            else
                bMessage = new BrokeredMessage(sMessage.Blob);
            
            bMessage.Properties.Add("OriginatorKey", sMessage.OriginatorKey);
            bMessage.Properties.Add("OriginatorServiceId", sMessage.OriginatorServiceId);
            bMessage.Properties.Add("OriginatorUTC", sMessage.OriginatorUTC);

            bMessage.Properties.Add("ResponseChannelId", sMessage.ResponseChannelId);
            bMessage.Properties.Add("ResponseChannelPriority", sMessage.ResponseChannelPriority.ToString());

            bMessage.Properties.Add("ChannelId", sMessage.ChannelId);
            bMessage.Properties.Add("MessageType", sMessage.MessageType);
            bMessage.Properties.Add("ActionType", sMessage.ActionType);

            bMessage.Properties.Add("IsNoop", sMessage.IsNoop ? "1" : "0");
            bMessage.Properties.Add("IsReplay", sMessage.IsReplay ? "1" : "0");

            bMessage.Properties.Add("CorrelationKey", sMessage.CorrelationKey);
            bMessage.CorrelationId = sMessage.CorrelationServiceId;
            bMessage.Properties.Add("CorrelationServiceId", sMessage.CorrelationServiceId);
            bMessage.Properties.Add("CorrelationUTC", sMessage.CorrelationUTC.HasValue ? sMessage.CorrelationUTC.Value.ToString("o") : null);

            bMessage.Properties.Add("DispatcherTransitCount", sMessage.DispatcherTransitCount);

            bMessage.Properties.Add("Status", sMessage.Status);
            bMessage.Properties.Add("StatusDescription", sMessage.StatusDescription);

            return bMessage;
        }
        #endregion
        #region MessageUnpack(BrokeredMessage bMessage)
        /// <summary>
        /// This method unpacks the azure service bus message in to a generic ServiceMessage object
        /// which can be processed by the service..
        /// /// </summary>
        /// <param name="bMessage">The Azure BrokeredMessage to convert.</param>
        /// <returns>Returns a generic ServiceMessage class for processing.</returns>
        public static ServiceMessage Unpack(BrokeredMessage bMessage)
        {
            var sMessage = new ServiceMessage();

            sMessage.EnqueuedTimeUTC = bMessage.EnqueuedTimeUtc;

            sMessage.OriginatorKey = bMessage.Properties["OriginatorKey"] as string;
            sMessage.OriginatorServiceId = bMessage.Properties["OriginatorServiceId"] as string;
            sMessage.OriginatorUTC = (DateTime)bMessage.Properties["OriginatorUTC"];

            sMessage.ResponseChannelId = bMessage.Properties["ResponseChannelId"] as string;

            if (bMessage.Properties.ContainsKey("ResponseChannelPriority"))
            {
                string value = bMessage.Properties["ResponseChannelPriority"] as string;
                int responsePriority;
                if (string.IsNullOrEmpty(value) || !int.TryParse(value, out responsePriority))
                    responsePriority = 0;
                sMessage.ResponseChannelPriority = responsePriority;
            }

            sMessage.ChannelId = bMessage.Properties["ChannelId"] as string;
            sMessage.MessageType = bMessage.Properties["MessageType"] as string;
            sMessage.ActionType = bMessage.Properties["ActionType"] as string;

            sMessage.IsNoop = bMessage.Properties["IsNoop"] as string == "1";
            sMessage.IsReplay = bMessage.Properties["IsReplay"] as string == "1";

            sMessage.CorrelationKey = bMessage.Properties["CorrelationKey"] as string;
            sMessage.CorrelationServiceId = bMessage.Properties["CorrelationServiceId"] as string;
            DateTime serviceUTC;
            if (bMessage.Properties.ContainsKey("CorrelationUTC") &&
                DateTime.TryParse(bMessage.Properties["CorrelationUTC"] as string, out serviceUTC))
                sMessage.CorrelationUTC = serviceUTC;

            sMessage.DispatcherTransitCount = (int)bMessage.Properties["DispatcherTransitCount"];

            sMessage.Status = bMessage.Properties["Status"] as string;
            sMessage.StatusDescription = bMessage.Properties["StatusDescription"] as string;

            sMessage.Blob = bMessage.GetBody<byte[]>();

            return sMessage;
        }
        #endregion 

        public static void MessageSignal(BrokeredMessage message, bool success)
        {
            if (message.State != MessageState.Active)
                return;

            if (success)
                message.Complete();
            else
                message.Abandon();
        }
    }
}
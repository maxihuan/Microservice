﻿using System;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;

namespace Xigadee
{
    /// <summary>
    /// This serializer is used to convert the object in to a simple JSON structure.
    /// </summary>
    /// <seealso cref="Xigadee.SerializerBase" />
    public class StatisticsSummaryLogUdpSerializer: SerializerBase
    {
        public StatisticsSummaryLogUdpSerializer():base("application/summarylog","The summary log serializer")
        {

        }
        /// <summary>
        /// This method is not supported.
        /// </summary>
        /// <param name="holder">The holder.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void Deserialize(ServiceHandlerContext holder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializes the summary JSON object in the holder and sets the byte array.
        /// </summary>
        /// <param name="holder">The holder to set.</param>
        public override void Serialize(ServiceHandlerContext holder)
        {
            var stats = holder.Object as Microservice.Statistics;
            if (stats == null)
                throw new ArgumentOutOfRangeException("The holder object is not of type MicroserviceStatistics");

            dynamic message = new ExpandoObject();
            message.Id = stats.Id.ExternalServiceId;
            message.Status = stats.Status;
            message.TS = DateTime.UtcNow.ToBinary();
            message.Engine = $"{stats.Id.ServiceVersionId}/{stats.Id.ServiceEngineVersionId}";
            message.Uptime = stats.Uptime;

            message.Tasks = stats.Tasks.Message;

            var authorData = JsonConvert.SerializeObject(message);

            holder.SetBlob(Encoding.UTF8.GetBytes(authorData), maxLength:UdpHelper.PacketMaxSize);
        }

        /// <summary>
        /// Returns true if the Content in the holder can be serialized.
        /// </summary>
        /// <param name="holder">The holder.</param>
        /// <returns>
        /// Returns true if it can be serialized.
        /// </returns>
        public override bool SupportsSerialization(ServiceHandlerContext holder)
        {
            return base.SupportsSerialization(holder) && holder.ObjectType == typeof(Microservice.Statistics);
        }

        /// <summary>
        /// Returns true if the serializer supports this entity type for serialization.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>
        /// Returns true if supported.
        /// </returns>
        /// <exception cref="NotSupportedException">This is not supported.</exception>
        public override bool SupportsContentTypeSerialization(Type entityType)
        {
            throw new NotSupportedException();
        }
    }
}

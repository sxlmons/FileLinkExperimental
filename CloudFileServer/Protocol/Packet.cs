using System;
using System.Collections.Generic;

namespace CloudFileServer.Protocol
{
    /// <summary>
    /// Represents a packet of data in the CloudFileServer protocol.
    /// Forms the basic unit of communication between client and server.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Gets or sets the command code that identifies the purpose of this packet.
        /// See <see cref="Commands.CommandCode"/> for defined command codes.
        /// </summary>
        public int CommandCode { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this packet.
        /// Used for tracking packets throughout the system and matching requests with responses.
        /// </summary>
        public Guid PacketId { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with this packet.
        /// Empty for unauthenticated packets.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when this packet was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the metadata dictionary for the packet.
        /// Contains additional information needed for processing the packet.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the binary payload data of the packet.
        /// The content and interpretation of this data depends on the command code.
        /// </summary>
        public byte[]? Payload { get; set; }

        /// <summary>
        /// Initializes a new instance of the Packet class with default values.
        /// </summary>
        public Packet()
        {
            PacketId = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the Packet class with the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code for this packet.</param>
        public Packet(int commandCode) : this()
        {
            CommandCode = commandCode;
        }

        /// <summary>
        /// Creates a deep copy of this packet.
        /// </summary>
        /// <returns>A new packet with the same content as this one.</returns>
        public Packet Clone()
        {
            var clone = new Packet
            {
                CommandCode = this.CommandCode,
                PacketId = this.PacketId,
                UserId = this.UserId,
                Timestamp = this.Timestamp
            };

            // Deep copy metadata
            foreach (var entry in this.Metadata)
            {
                clone.Metadata[entry.Key] = entry.Value;
            }

            // Deep copy payload
            if (this.Payload != null)
            {
                clone.Payload = new byte[this.Payload.Length];
                Array.Copy(this.Payload, clone.Payload, this.Payload.Length);
            }

            return clone;
        }

        /// <summary>
        /// Creates a response packet for this request packet.
        /// Sets the command code to the response code that corresponds to the request code.
        /// </summary>
        /// <param name="responseCommandCode">The command code for the response.</param>
        /// <returns>A new packet configured as a response to this packet.</returns>
        public Packet CreateResponse(int responseCommandCode)
        {
            var response = new Packet
            {
                CommandCode = responseCommandCode,
                PacketId = Guid.NewGuid(),
                UserId = this.UserId,
                Timestamp = DateTime.Now
            };

            // Add the original request packet ID to the metadata
            response.Metadata["RequestPacketId"] = this.PacketId.ToString();

            return response;
        }
    }
}
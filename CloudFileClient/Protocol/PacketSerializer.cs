using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudFileClient.Core.Exceptions;
//using CloudFileClient.Utils;

namespace CloudFileClient.Protocol
{
    /// <summary>
    /// Handles the binary serialization and deserialization of packets.
    /// </summary>
    public class PacketSerializer
    {
        // Protocol versioning
        private const byte PROTOCOL_VERSION = 1;
        
        // Header Structure:
        // - Protocol Version (1 byte)
        // - Command Code (4 bytes)
        // - Packet ID (16 bytes)
        // - User ID Length (4 bytes)
        // - User ID (variable)
        // - Timestamp (8 bytes)
        // - Metadata Count (4 bytes)
        // - Metadata Key-Value Pairs (variable)
        // - Payload Length (4 bytes)
        // - Payload (variable)

        /// <summary>
        /// Serializes a packet into a byte array.
        /// </summary>
        /// <param name="packet">The packet to serialize.</param>
        /// <returns>The serialized byte array.</returns>
        public byte[] Serialize(Packet packet)
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                // Write protocol version
                writer.Write(PROTOCOL_VERSION);

                // Write command code
                writer.Write(packet.CommandCode);

                // Write packet ID
                writer.Write(packet.PacketId.ToByteArray());

                // Write user ID
                byte[] userIdBytes = Encoding.UTF8.GetBytes(packet.UserId ?? string.Empty);
                writer.Write(userIdBytes.Length);
                writer.Write(userIdBytes);

                // Write timestamp (as ticks)
                writer.Write(packet.Timestamp.Ticks);

                // Write metadata
                writer.Write(packet.Metadata?.Count ?? 0);
                if (packet.Metadata != null)
                {
                    foreach (var kvp in packet.Metadata)
                    {
                        byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                        byte[] valueBytes = Encoding.UTF8.GetBytes(kvp.Value);

                        writer.Write(keyBytes.Length);
                        writer.Write(keyBytes);
                        writer.Write(valueBytes.Length);
                        writer.Write(valueBytes);
                    }
                }

                // Write payload
                if (packet.Payload != null)
                {
                    writer.Write(packet.Payload.Length);
                    writer.Write(packet.Payload);
                }
                else
                {
                    writer.Write(0);
                }

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                throw new ProtocolException("Error serializing packet", ex);
            }
        }

        /// <summary>
        /// Deserializes a byte array into a packet.
        /// </summary>
        /// <param name="data">The serialized packet data.</param>
        /// <returns>The deserialized packet.</returns>
        public Packet Deserialize(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);

                var packet = new Packet();

                // Read protocol version
                byte version = reader.ReadByte();
                if (version != PROTOCOL_VERSION)
                {
                    throw new ProtocolException($"Unsupported protocol version: {version}");
                }

                // Read command code
                packet.CommandCode = reader.ReadInt32();

                // Read packet ID
                byte[] packetIdBytes = reader.ReadBytes(16);
                packet.PacketId = new Guid(packetIdBytes);

                // Read user ID
                int userIdLength = reader.ReadInt32();
                byte[] userIdBytes = reader.ReadBytes(userIdLength);
                packet.UserId = Encoding.UTF8.GetString(userIdBytes);

                // Read timestamp
                long timestampTicks = reader.ReadInt64();
                packet.Timestamp = new DateTime(timestampTicks);

                // Read metadata
                int metadataCount = reader.ReadInt32();
                packet.Metadata = new Dictionary<string, string>(metadataCount);
                for (int i = 0; i < metadataCount; i++)
                {
                    int keyLength = reader.ReadInt32();
                    byte[] keyBytes = reader.ReadBytes(keyLength);
                    string key = Encoding.UTF8.GetString(keyBytes);

                    int valueLength = reader.ReadInt32();
                    byte[] valueBytes = reader.ReadBytes(valueLength);
                    string value = Encoding.UTF8.GetString(valueBytes);

                    packet.Metadata[key] = value;
                }

                // Read payload
                int payloadLength = reader.ReadInt32();
                if (payloadLength > 0)
                {
                    packet.Payload = reader.ReadBytes(payloadLength);
                }

                return packet;
            }
            catch (Exception ex) when (!(ex is ProtocolException))
            {
                throw new ProtocolException("Error deserializing packet", ex);
            }
        }
    }
}
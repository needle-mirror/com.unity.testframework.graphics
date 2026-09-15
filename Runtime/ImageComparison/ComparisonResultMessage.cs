using System;
using System.IO;
using System.Text;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A player-to-host message carrying the thresholds (expected) and measured values (actual) of a single
    /// image comparison, sent for every comparison whether it passed or failed.
    /// </summary>
    public class ComparisonResultMessage
    {
        /// <summary>
        /// The message ID for the comparison result message. This is used to identify the message type.
        /// </summary>
        public static Guid MessageId { get; } = new Guid("d4d6fea6-6e48-4593-8875-8956b2b5c2e6");

        /// <summary>
        /// The full name of the NUnit test the comparison ran in, or null when the comparison ran outside a test.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The name identifying the compared image, if one was configured.
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// The type name of the <see cref="TextureComparisonAlgorithm"/> that produced the values.
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Whether the comparison passed its thresholds.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The configured thresholds as a JSON object, with keys specific to <see cref="Algorithm"/>.
        /// </summary>
        public string ExpectedJson { get; set; }

        /// <summary>
        /// The measured values as a JSON object, with keys specific to <see cref="Algorithm"/>.
        /// </summary>
        public string ActualJson { get; set; }

        /// <summary>
        /// Serializes the message to a byte array.
        /// </summary>
        /// <returns>The serialized byte array representation of the message.</returns>
        public byte[] Serialize()
        {
            var encoding = Encoding.UTF8;
            var capacity =
                sizeof(int) * 5
                + sizeof(bool)
                + (TestName != null ? encoding.GetByteCount(TestName) : 0)
                + (ImageName != null ? encoding.GetByteCount(ImageName) : 0)
                + (Algorithm != null ? encoding.GetByteCount(Algorithm) : 0)
                + (ExpectedJson != null ? encoding.GetByteCount(ExpectedJson) : 0)
                + (ActualJson != null ? encoding.GetByteCount(ActualJson) : 0);
            using (var memoryStream = new MemoryStream(capacity))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.WriteString(TestName);
                    writer.WriteString(ImageName);
                    writer.WriteString(Algorithm);
                    writer.Write(Success);
                    writer.WriteString(ExpectedJson);
                    writer.WriteString(ActualJson);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a byte array produced by <see cref="Serialize"/>.
        /// </summary>
        /// <param name="data">The serialized byte array.</param>
        /// <returns>The deserialized message.</returns>
        public static ComparisonResultMessage Deserialize(byte[] data)
        {
            using (var messageStream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(messageStream))
                {
                    return new ComparisonResultMessage
                    {
                        TestName = reader.GetString(),
                        ImageName = reader.GetString(),
                        Algorithm = reader.GetString(),
                        Success = reader.ReadBoolean(),
                        ExpectedJson = reader.GetString(),
                        ActualJson = reader.GetString(),
                    };
                }
            }
        }
    }
}

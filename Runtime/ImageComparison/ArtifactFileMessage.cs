using System;
using System.IO;
using System.Text;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A player-to-host message carrying an arbitrary file (bytes plus a directory and file name) to be collected as a
    /// test artifact. Unlike <see cref="ImageMessage"/>, the file name is used verbatim, so any extension is preserved.
    /// </summary>
    public class ArtifactFileMessage
    {
        /// <summary>
        /// The message ID. Must match the TestArtifactFileMessage serializer on the UnifiedTestRunner host.
        /// </summary>
        public static Guid MessageId { get; } = new Guid("7f3c9e21-4d8a-4b16-9a5e-2c6f1b0d8e34");

        /// <summary>
        /// The directory (relative to the run's artifacts path) the file is written under.
        /// </summary>
        public string PathName { get; set; }

        /// <summary>
        /// The file name, including its extension, used verbatim.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The file contents.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Serializes the message to a byte array.
        /// </summary>
        /// <returns>The serialized byte array representation of the message.</returns>
        public byte[] Serialize()
        {
            var encoding = Encoding.UTF8;
            var capacity =
                sizeof(int) * 3
                + (PathName != null ? encoding.GetByteCount(PathName) : 0)
                + (FileName != null ? encoding.GetByteCount(FileName) : 0)
                + (Data?.Length ?? 0);
            using (var memoryStream = new MemoryStream(capacity))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.WriteString(PathName);
                    writer.WriteString(FileName);
                    writer.WriteBytes(Data);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a byte array produced by <see cref="Serialize"/>.
        /// </summary>
        /// <param name="data">The serialized byte array.</param>
        /// <returns>The deserialized message.</returns>
        public static ArtifactFileMessage Deserialize(byte[] data)
        {
            using (var messageStream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(messageStream))
                {
                    return new ArtifactFileMessage
                    {
                        PathName = reader.GetString(),
                        FileName = reader.GetString(),
                        Data = reader.GetBytes(),
                    };
                }
            }
        }
    }
}

using System;
using System.IO;
using System.Text;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class is used to represent an image message.
    /// </summary>
    public class ImageMessage
    {
        /// <summary>
        /// The message ID for the image message. This is used to identify the message type.
        /// </summary>
        public static Guid MessageId { get; } = new Guid("40c7a8e2-ad5d-475f-8119-af022a13b84c");

        /// <summary>
        /// The path name of the image. This is used to identify the location of the image file.
        /// </summary>
        public string PathName { get; set; }

        /// <summary>
        /// The name of the image. This is used to identify the image file.
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// The expected image in byte array format.
        /// </summary>
        public byte[] ExpectedImage { get; set; }

        /// <summary>
        /// The actual image in byte array format.
        /// </summary>
        public byte[] ActualImage { get; set; }

        /// <summary>
        /// The diff image in byte array format.
        /// </summary>
        public byte[] DiffImage { get; set; }

        /// <summary>
        /// Serializes the image message to a byte array.
        /// This is used to send the message over the network or to save it to a file.
        /// </summary>
        /// <returns>
        /// The serialized byte array representation of the image message.
        /// </returns>
        public byte[] Serialize()
        {
            var encoding = Encoding.UTF8;
            var capacity =
                sizeof(int) * 5
                + (PathName != null ? encoding.GetByteCount(PathName) : 0)
                + (ImageName != null ? encoding.GetByteCount(ImageName) : 0)
                + (ExpectedImage?.Length ?? 0)
                + (ActualImage?.Length ?? 0)
                + (DiffImage?.Length ?? 0);
            using (var memoryStream = new MemoryStream(capacity))
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    writer.WriteString(PathName);
                    writer.WriteString(ImageName);
                    writer.WriteBytes(ExpectedImage);
                    writer.WriteBytes(ActualImage);
                    writer.WriteBytes(DiffImage);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the byte array to an image message.
        /// This is used to receive the message over the network or to read it from a file.
        /// </summary>
        /// <param name="data">
        /// The byte array representation of the image message.
        /// This should be the result of a previous call to <see cref="Serialize"/>.
        /// </param>
        /// <returns>
        /// The deserialized image message.
        /// </returns>
        public static ImageMessage Deserialize(byte[] data)
        {
            using (var messageStream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(messageStream))
                {
                    return new ImageMessage
                    {
                        PathName = reader.GetString(),
                        ImageName = reader.GetString(),
                        ExpectedImage = reader.GetBytes(),
                        ActualImage = reader.GetBytes(),
                        DiffImage = reader.GetBytes(),
                    };
                }
            }
        }
    }

    /// <summary>
    /// Extensions for the <see cref="BinaryWriter"/> class.
    /// These extensions provide methods for writing strings and byte arrays to a binary writer.
    /// </summary>
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Writes a string to the binary writer.
        /// The string is written as a length-prefixed string.
        /// The length of the string is written as an integer, followed by the string itself.
        /// If the string is null, -1 is written as the length.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to write the string to.
        /// </param>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use for the string.
        /// </param>
        public static void WriteString(this BinaryWriter writer, string value, Encoding encoding = null)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            encoding = encoding ?? Encoding.UTF8;
            var data = encoding.GetBytes(value);
            writer.WriteBytes(data);
        }

        /// <summary>
        /// Writes a byte array to the binary writer.
        /// The byte array is written as a length-prefixed array.
        /// The length of the array is written as an integer, followed by the byte array itself.
        /// If the byte array is null, -1 is written as the length.
        /// </summary>
        /// <param name="writer">
        /// The binary writer to write the byte array to.
        /// </param>
        /// <param name="value">
        /// The byte array to write.
        /// </param>
        public static void WriteBytes(this BinaryWriter writer, byte[] value)
        {
            if (value == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(value.Length);
            writer.Write(value);
        }
    }

    /// <summary>
    /// Extensions for the <see cref="BinaryReader"/> class.
    /// These extensions provide methods for reading strings and byte arrays from a binary reader.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads a string from the binary reader.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read the string from.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use for the string.
        /// </param>
        /// <returns>
        /// The string read from the binary reader.
        /// If the length is -1, null is returned.
        /// </returns>
        public static string GetString(this BinaryReader reader, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var length = reader.ReadInt32();
            if (length < 0)
            {
                return null;
            }

            return encoding.GetString(reader.ReadBytes(length));
        }

        /// <summary>
        /// Reads a byte array from the binary reader.
        /// The byte array is read as a length-prefixed array.
        /// The length of the array is read as an integer, followed by the byte array itself.
        /// </summary>
        /// <param name="reader">
        /// The binary reader to read the byte array from.
        /// </param>
        /// <returns>
        /// The byte array read from the binary reader.
        /// If the length is -1, null is returned.
        /// </returns>
        public static byte[] GetBytes(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 0)
            {
                return null;
            }

            var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if (length > remaining)
                throw new InvalidOperationException(
                    $"Corrupt image message: declared length {length} exceeds remaining stream bytes {remaining}.");

            return reader.ReadBytes(length);
        }
    }
}

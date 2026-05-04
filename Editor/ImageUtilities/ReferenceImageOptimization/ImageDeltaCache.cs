using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace UnityEditor.TestTools.Graphics
{
    static class DeltaCache
    {
        record DeltaWriteRequest(string TestName, int Width, int Height, float[] Deltas);

        static readonly BlockingCollection<DeltaWriteRequest> k_Queue = new();
        static readonly string k_CacheFolder = Path.Combine("Library", "ReferenceImageDeltas");

        static DeltaCache()
        {
            Directory.CreateDirectory(k_CacheFolder);

            var thread = new Thread(() =>
            {
                foreach (var request in k_Queue.GetConsumingEnumerable())
                {
                    try
                    {
                        var filePath = Path.Combine(k_CacheFolder, request.TestName + ".delta.gz");
                        using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        using GZipStream gz = new(fs, CompressionLevel.Fastest);
                        using BinaryWriter writer = new(gz);

                        writer.Write(request.Width);
                        writer.Write(request.Height);
                        for (var i = 0; i < request.Deltas.Length; i++)
                            writer.Write(request.Deltas[i]);
                    }
                    catch (ThreadAbortException t)
                    {
                        GraphicsTestLogger.DebugError($"[DeltaCache] Thread aborted: {t}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DeltaCache] Write error: {ex}");
                    }
                    finally
                    {
                        ArrayPool<float>.Shared.Return(request.Deltas, clearArray: true);
                    }
                }
            })
            {
                IsBackground = true,
            };
            thread.Start();
        }

        public static void EnqueueWrite(string testName, int width, int height, float[] deltas)
        {
            k_Queue.Add(new DeltaWriteRequest(testName, width, height, deltas));
        }

        public static void Shutdown()
        {
            k_Queue.CompleteAdding();
        }
    }
}

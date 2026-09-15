using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.Networking.PlayerConnection;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Builds a <see cref="ComparisonResultMessage"/> with the thresholds and measured values of
    /// every image comparison and sends it to the host over the player connection, like
    /// <see cref="ImageMessage"/>. Works with any <see cref="TextureComparisonAlgorithm"/>:
    /// algorithms that override <see cref="TextureComparisonAlgorithm.DescribeExpected"/> and
    /// <see cref="TextureComparisonAlgorithm.DescribeActual"/> report structured values; the rest
    /// fall back to <see cref="TextureComparisonAlgorithm.Description"/> and the result's ToString().
    /// </summary>
    class ComparisonResultReporter
    {
        /// <summary>
        /// Reports the thresholds and measured values of a comparison run by any
        /// <see cref="TextureComparisonAlgorithm"/>.
        /// </summary>
        /// <param name="algorithm">The algorithm that ran the comparison.</param>
        /// <param name="result">The comparison result produced by the algorithm.</param>
        /// <param name="passed">Whether the comparison passed its thresholds.</param>
        /// <param name="imageName">Optional name identifying the compared image.</param>
        internal void Report(
            TextureComparisonAlgorithm algorithm,
            ITextureComparisonResult result,
            bool passed,
            string imageName = null
        )
        {
            try
            {
                Send(BuildMessage(algorithm, result, passed, imageName));
            }
            catch (Exception e)
            {
                GraphicsTestLogger.DebugWarning(
                    $"ComparisonResultReporter: failed to publish comparison result: {e}"
                );
            }
        }

        /// <summary>
        /// Builds the message for a comparison: the algorithm's structured values when it overrides
        /// the Describe methods, otherwise its <see cref="TextureComparisonAlgorithm.Description"/>
        /// and the result's ToString().
        /// </summary>
        /// <param name="algorithm">The algorithm that ran the comparison.</param>
        /// <param name="result">The comparison result produced by the algorithm.</param>
        /// <param name="passed">Whether the comparison passed its thresholds.</param>
        /// <param name="imageName">Optional name identifying the compared image.</param>
        /// <returns>The message to send.</returns>
        internal ComparisonResultMessage BuildMessage(
            TextureComparisonAlgorithm algorithm,
            ITextureComparisonResult result,
            bool passed,
            string imageName = null
        )
        {
            var expected = algorithm.DescribeExpected();
            if (expected == null && !string.IsNullOrEmpty(algorithm.Description))
            {
                expected = new Dictionary<string, object> { ["description"] = algorithm.Description };
            }

            var actual = algorithm.DescribeActual(result);
            if (actual == null && result != null)
            {
                actual = new Dictionary<string, object> { ["result"] = result.ToString() };
            }

            return new ComparisonResultMessage
            {
                TestName = CurrentTestName(),
                ImageName = imageName,
                Algorithm = algorithm.GetType().Name,
                Success = passed,
                ExpectedJson = expected != null ? JsonConvert.SerializeObject(expected) : null,
                ActualJson = actual != null ? JsonConvert.SerializeObject(actual) : null,
            };
        }

        /// <summary>
        /// Sends the message. Does nothing by default; override to provide a transport.
        /// </summary>
        /// <param name="message">The message to send.</param>
        protected virtual void Send(ComparisonResultMessage message) { }

        static string CurrentTestName()
        {
            try
            {
                return TestContext.CurrentContext?.Test?.FullName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Sends comparison results to the host over the player connection, like <see cref="ImageMessage"/>.
    /// Does nothing when no host is connected.
    /// </summary>
    class PlayerConnectionComparisonResultReporter : ComparisonResultReporter
    {
        /// <inheritdoc />
        protected override void Send(ComparisonResultMessage message)
        {
            var connection = PlayerConnection.instance;
            if (!connection.isConnected)
            {
                return;
            }

            connection.Send(ComparisonResultMessage.MessageId, message.Serialize());
        }
    }
}

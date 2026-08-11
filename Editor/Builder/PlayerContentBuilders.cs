using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// The registry of <see cref="IPlayerContentBuilder"/>s that produce content bundles for
    /// graphics test player builds. The framework registers its reference image and test data
    /// builders by default; register a custom builder to ship additional content with test players.
    /// </summary>
    public static class PlayerContentBuilders
    {
        static readonly List<IPlayerContentBuilder> s_Builders = new()
        {
            new AssetBundleBuilder(),
            new TestDataBundleBuilder(),
        };

        /// <summary>
        /// The registered content builders, in execution order.
        /// </summary>
        public static IReadOnlyList<IPlayerContentBuilder> All => s_Builders;

        /// <summary>
        /// Registers a content builder to run after the already registered builders. The registry
        /// is process-wide, so register once per domain reload or pair this with
        /// <see cref="Unregister"/>; registering a new instance per build would leave the previous
        /// one running too.
        /// </summary>
        /// <param name="builder">The builder to register.</param>
        public static void Register(IPlayerContentBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (s_Builders.Contains(builder))
                return;

            // Registering a fresh instance on every build accumulates them, and the next build
            // would run the stale one alongside the new one. Reported rather than deduplicated,
            // since two instances of one builder type can be deliberate.
            foreach (var registered in s_Builders)
            {
                if (registered.GetType() == builder.GetType())
                {
                    GraphicsTestLogger.LogWarning(
                        $"A {builder.GetType().Name} is already registered with PlayerContentBuilders; "
                            + "both will run. Register content builders once per domain reload, or "
                            + "unregister the previous one."
                    );
                    break;
                }
            }

            s_Builders.Add(builder);
        }

        /// <summary>
        /// Removes a previously registered content builder.
        /// </summary>
        /// <param name="builder">The builder to remove.</param>
        /// <returns>True when the builder was registered and has been removed.</returns>
        public static bool Unregister(IPlayerContentBuilder builder)
        {
            return s_Builders.Remove(builder);
        }
    }
}

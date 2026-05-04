using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    [Serializable]
    record TestSettings
    {
        [SerializeField]
        string architecture;

        [SerializeField]
        string playerGraphicsAPI;

        [SerializeField]
        string graphicsVendor;

        internal TestSettings(string architecture, string playerGraphicsAPI, string graphicsVendor = null)
        {
            this.architecture = architecture;
            this.playerGraphicsAPI = playerGraphicsAPI;
            this.graphicsVendor = graphicsVendor;
        }

        internal Architecture? Architecture
        {
            get
            {
                try
                {
                    return Enum.Parse<Architecture>(architecture, true);
                }
                catch (ArgumentException e)
                {
                    GraphicsTestLogger.DebugLog(e.Message);
                    return null;
                }
            }
        }

        internal GraphicsDeviceType? PlayerGraphicsAPI
        {
            get
            {
                try
                {
                    return Enum.Parse<GraphicsDeviceType>(playerGraphicsAPI, true);
                }
                catch (ArgumentException e)
                {
                    GraphicsTestLogger.DebugLog(e.Message);
                    return null;
                }
            }
        }

        internal GraphicsVendor? GraphicsVendor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(graphicsVendor))
                    return null;
                try
                {
                    return Enum.Parse<GraphicsVendor>(graphicsVendor, true);
                }
                catch (ArgumentException e)
                {
                    GraphicsTestLogger.DebugLog(e.Message);
                    return null;
                }
            }
        }
    }
}

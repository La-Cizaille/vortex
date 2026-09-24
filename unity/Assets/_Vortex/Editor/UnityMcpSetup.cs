using UnityEditor;
using UnityEngine;

namespace Vortex.Editor
{
    /// <summary>
    /// Development setup of Unity MCP (docs/CONTRIBUTING.md): stdio transport, so that the MCP server is started by the
    /// AI client and talks to the editor bridge on 127.0.0.1 only, and telemetry disabled. The preference keys are
    /// those of MCP for Unity v10.2.0 (MCPForUnity.Editor.Constants.EditorPrefKeys); check them when upgrading.
    /// </summary>
    public static class UnityMcpSetup
    {
        private const string UseHttpTransportKey = "MCPForUnity.UseHttpTransport";
        private const string TelemetryDisabledKey = "MCPForUnity.TelemetryDisabled";

        /// <summary>Applies the settings (menu, or batch mode with -executeMethod Vortex.Editor.UnityMcpSetup.Configure).</summary>
        [MenuItem("Vortex/Développement/Configurer Unity MCP")]
        public static void Configure()
        {
            EditorPrefs.SetBool(UseHttpTransportKey, false);
            EditorPrefs.SetBool(TelemetryDisabledKey, true);
            Debug.Log("Unity MCP configured: stdio transport (bridge on 127.0.0.1), telemetry disabled. Restart the editor to apply.");
        }

        /// <summary>True when the settings are applied.</summary>
        public static bool IsConfigured()
        {
            return !EditorPrefs.GetBool(UseHttpTransportKey, true) && EditorPrefs.GetBool(TelemetryDisabledKey, false);
        }
    }
}

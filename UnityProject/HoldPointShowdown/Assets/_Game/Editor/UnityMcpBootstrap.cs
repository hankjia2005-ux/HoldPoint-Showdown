using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;
using UnityEngine;

namespace HoldPointShowdown.Editor
{
    [InitializeOnLoad]
    public static class UnityMcpBootstrap
    {
        private const string UseHttpTransportPreference = "MCPForUnity.UseHttpTransport";

        static UnityMcpBootstrap()
        {
            EditorApplication.delayCall += EnsureBridgeAfterReload;
        }

        [MenuItem("HoldPoint Showdown/Tools/Restart Unity MCP Bridge")]
        public static void RestartBridge()
        {
            EditorPrefs.SetBool(UseHttpTransportPreference, false);
            StdioBridgeHost.StartAutoConnect();
            Debug.Log($"Unity MCP stdio bridge is listening on port {StdioBridgeHost.GetCurrentPort()}.");
        }

        private static void EnsureBridgeAfterReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureBridgeAfterReload;
                return;
            }

            EditorPrefs.SetBool(UseHttpTransportPreference, false);

            // A domain reload can preserve a stale running flag after the socket has closed.
            // StartAutoConnect is idempotent and also repairs that stale bridge state.
            StdioBridgeHost.StartAutoConnect();
            Debug.Log($"Unity MCP stdio bridge ensured on port {StdioBridgeHost.GetCurrentPort()}.");
        }
    }
}

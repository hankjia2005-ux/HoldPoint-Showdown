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

            if (!StdioBridgeHost.IsRunning)
            {
                StdioBridgeHost.StartAutoConnect();
                Debug.Log($"Unity MCP stdio bridge started automatically on port {StdioBridgeHost.GetCurrentPort()}.");
            }
        }
    }
}

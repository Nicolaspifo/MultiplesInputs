using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NetworkingLab.Server
{
    /// <summary>Reports readiness and health to the Agones SDK sidecar when the dedicated server runs inside a GameServer pod.</summary>
    public static class AgonesSdkLifecycle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            string httpPort = Environment.GetEnvironmentVariable("AGONES_SDK_HTTP_PORT");
            if (string.IsNullOrWhiteSpace(httpPort))
            {
                return;
            }

            GameObject host = new GameObject("AgonesSdkLifecycle");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<AgonesSdkLifecycleRunner>().Initialize(httpPort);
        }
    }

    internal sealed class AgonesSdkLifecycleRunner : MonoBehaviour
    {
        private const float ReadyDelaySeconds = 3f;
        private const float HealthIntervalSeconds = 2f;

        private string baseUrl;
        private bool lastSucceeded;

        public void Initialize(string httpPort)
        {
            baseUrl = "http://127.0.0.1:" + httpPort.Trim();
            StartCoroutine(RunLifecycle());
        }

        private IEnumerator RunLifecycle()
        {
            yield return new WaitForSeconds(ReadyDelaySeconds);

            while (!lastSucceeded)
            {
                yield return Send("ready");
                if (!lastSucceeded)
                {
                    yield return new WaitForSeconds(HealthIntervalSeconds);
                }
            }

            while (true)
            {
                yield return Send("health");
                yield return new WaitForSeconds(HealthIntervalSeconds);
            }
        }

        private IEnumerator Send(string endpoint)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/" + endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                lastSucceeded = request.result == UnityWebRequest.Result.Success;

                if (!lastSucceeded)
                {
                    Debug.LogWarning($"AGONES_SDK: {endpoint} -> HTTP {request.responseCode} {request.error}");
                }
                else
                {
                    Debug.Log($"AGONES_SDK: {endpoint} -> HTTP {request.responseCode}");
                }
            }
        }
    }
}

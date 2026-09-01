using System.Collections;
using UnityEngine;

public class ServerLauncher
#if UNITY_2017_1_OR_NEWER
    : MonoBehaviour
#endif
{
    private DedicateServerProgram server;
    private void Awake()
    {
        server = new();
        server.Start();
    }
    private void Update()
    {
        server.Loop();
    }
}

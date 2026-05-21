using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerLauncher
#if UNITY_2017_1_OR_NEWER
    : MonoBehaviour
#endif
{
    private DedicateServerProgram server;
    private void Awake()
    {
        server=new DedicateServerProgram();
        server.Start();
    }
    private void Update()
    {
        server.Loop();
    }
}

using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class StartCubeRotateAnim : NetworkBehaviour
{
    private NetworkAnimator netAnim;

    public override void OnNetworkSpawn()
    {
        netAnim = GetComponent<NetworkAnimator>();

        if(IsServer)
            netAnim.SetTrigger("StartRotate");
    }
}

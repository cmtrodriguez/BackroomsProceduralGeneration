using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitRoomHandling : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnTriggerEnter(Collider other)
    { 
        GenerationManager gm = FindObjectOfType<GenerationManager>();
        gm.WinGame();
    }
}

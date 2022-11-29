using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3Killed : MonoBehaviour
{
    void OnBecameInvisible()
    {
        gameObject.SetActive(false);
    }
}

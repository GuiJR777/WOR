using System;
using UnityEngine;

public class FaceToRightSide : MonoBehaviour
{
    private void OnEnable()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            transform.localScale = player.transform.localScale;
        }
    }
}

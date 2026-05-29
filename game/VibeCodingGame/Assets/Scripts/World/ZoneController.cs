using UnityEngine;
using System;

public class ZoneController : MonoBehaviour
{
    public string ZoneName;
    public string ZoneDescription;
    public int ZoneIndex;
    public Action<ZoneController> OnPlayerEnter;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            OnPlayerEnter?.Invoke(this);
    }
}

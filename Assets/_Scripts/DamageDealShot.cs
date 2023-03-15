using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageDealShot : MonoBehaviour
{
    public int damage = 10;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<TankData>().ChangeHealth(damage);
        }
    }
}

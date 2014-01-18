using System;
using UnityEngine;
public class SpawnableObject : MonoBehaviour
{
	public Spawner spawner;
	public virtual void OnDeath()
	{
		this.spawner.RemoveInstance(base.gameObject);
	}
}

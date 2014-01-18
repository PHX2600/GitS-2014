using System;
using UnityEngine;
public class PVPZone : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (!GameState.isServer)
		{
			return;
		}
		Transform transform = other.transform;
		while (transform != null)
		{
			Player component = transform.GetComponent<Player>();
			if (component != null)
			{
				component.EnterPVPZone();
				return;
			}
			transform = transform.parent;
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (!GameState.isServer)
		{
			return;
		}
		Transform transform = other.transform;
		while (transform != null)
		{
			Player component = transform.GetComponent<Player>();
			if (component != null)
			{
				component.ExitPVPZone();
				return;
			}
			transform = transform.parent;
		}
	}
}

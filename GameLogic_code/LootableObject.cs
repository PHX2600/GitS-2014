using System;
using UnityEngine;
public class LootableObject : MonoBehaviour
{
	public LootCollection collection;
	private void OnEnable()
	{
		if (base.GetComponent("LootableObjectInteractable") != null)
		{
			((MonoBehaviour)base.GetComponent("LootableObjectInteractable")).enabled = true;
		}
	}
	private void OnDisable()
	{
		if (base.GetComponent("LootableObjectInteractable") != null)
		{
			((MonoBehaviour)base.GetComponent("LootableObjectInteractable")).enabled = false;
		}
	}
}

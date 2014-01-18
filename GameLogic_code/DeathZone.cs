using System;
using UnityEngine;
public class DeathZone : MonoBehaviour
{
	public string requiredQuest;
	private void OnTriggerEnter(Collider other)
	{
		if (!GameState.isServer)
		{
			return;
		}
		Transform transform = other.transform;
		while (transform != null)
		{
			if (this.requiredQuest != null && this.requiredQuest != "")
			{
				Player component = transform.GetComponent<Player>();
				if (component != null && component.questState.HasQuest(this.requiredQuest))
				{
					return;
				}
			}
			if (transform.GetComponent<Enemy>() != null)
			{
				transform.GetComponent<Enemy>().Damage(new Damage(null, null, 1000000f));
			}
			if (transform.GetComponent<Player>() != null)
			{
				transform.GetComponent<Player>().Damage(new Damage(null, null, 1000000f));
			}
			transform = transform.parent;
		}
	}
}

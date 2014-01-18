using System;
using UnityEngine;
public class ThrownWeapon : MonoBehaviour
{
	public GameObject prefab;
	public float minSpeed = 15f;
	public float maxSpeed = 25f;
	public float attackTime;
	public virtual void PerformAttack(Player player, Ray ray)
	{
		Item component = base.GetComponent<Item>();
		if (player.health <= 0)
		{
			return;
		}
		Vector3 origin = ray.origin;
		Vector3 vector = ray.direction;
		if (vector.magnitude < 0.5f)
		{
			return;
		}
		vector = vector.normalized;
		Vector3 normalized = Vector3.Cross(vector, new Vector3(0f, -1f, 0f)).normalized;
		Vector3 normalized2 = Vector3.Cross(vector, normalized).normalized;
		if (!player.inventory.RemoveItem(component.itemName, 1))
		{
			return;
		}
		GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(this.prefab, origin + normalized2 * 0.2f + normalized * 0.2f + vector * 0.8f, Quaternion.identity);
		gameObject.rigidbody.velocity = vector * Mathf.Lerp(this.minSpeed, this.maxSpeed, Mathf.Clamp(vector.y * 2f, 0f, 1f));
		gameObject.rigidbody.angularVelocity = new Vector3(10f, 8f, 5f);
		if (gameObject.GetComponent<Explosive>() != null)
		{
			gameObject.GetComponent<Explosive>().owner = player;
		}
		GameState.instance.AddServerReplicatedObject(component.itemName, gameObject);
	}
}

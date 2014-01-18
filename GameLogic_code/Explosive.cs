using System;
using UnityEngine;
public class Explosive : MonoBehaviour
{
	public string itemName;
	public float damage = 200f;
	public float radius = 10f;
	public float fuseTime = 3f;
	public GameObject explosionPrefab;
	public Player owner;
	private void Update()
	{
		if (!GameState.isServer)
		{
			return;
		}
		this.fuseTime -= Time.deltaTime;
		if (this.fuseTime < 0f)
		{
			GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
			for (int i = 0; i < array.Length; i++)
			{
				GameObject target = array[i];
				this.TryDamage(target);
			}
			GameObject[] array2 = GameObject.FindGameObjectsWithTag("Player");
			for (int j = 0; j < array2.Length; j++)
			{
				GameObject target2 = array2[j];
				this.TryDamage(target2);
			}
			GameState.instance.RemoveReplicatedObject(base.gameObject);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
	private void OnDisable()
	{
		if (!GameState.isServer)
		{
			UnityEngine.Object.Instantiate(this.explosionPrefab, base.transform.position, Quaternion.identity);
		}
	}
	private void TryDamage(GameObject target)
	{
		Vector3 a = target.transform.position + target.rigidbody.centerOfMass;
		Vector3 vector = base.transform.position + base.rigidbody.centerOfMass;
		float magnitude = (a - vector).magnitude;
		if (magnitude > this.radius)
		{
			return;
		}
		RaycastHit raycastHit;
		if (!Physics.Raycast(vector, a - vector, out raycastHit))
		{
			return;
		}
		Transform transform = raycastHit.transform;
		while (transform != null)
		{
			if (transform.gameObject == target)
			{
				break;
			}
			transform = transform.parent;
		}
		if (transform == null)
		{
			return;
		}
		float num = 1f - (this.radius - magnitude) / this.radius;
		this.ApplyDamage(target, (int)Mathf.Lerp(0f, this.damage, 1f - num * num));
	}
	public virtual void ApplyDamage(GameObject target, int damage)
	{
		Item itemByName = GameState.GetInventoryItemCollection().GetItemByName(this.itemName);
		target.SendMessage("Damage", new Damage(this.owner.gameObject, itemByName, (float)damage), SendMessageOptions.DontRequireReceiver);
	}
}

using System;
using UnityEngine;
public class FollowPlayer : MonoBehaviour
{
	public Transform player;
	public Vector3 offset;
	private void Update()
	{
		base.transform.position = this.player.position + this.offset;
	}
}

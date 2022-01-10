using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class ExplosionScript : NetworkBehaviour {
    List<GameObject> objectsAffected = new List<GameObject>();
    public int damage;
    public float explosionSize;
    public float explosionForce;
    public Vector3 shooterPosition;

    void Start() {
        StartCoroutine(TurnOffCollider());
        Destroy(gameObject, 3.0f);
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (Vector3.Distance(player.transform.position, gameObject.transform.position) <= explosionSize)
            {
                player.GetComponent<PlayerManager>().RpcGetBoopedSon(explosionSize, explosionForce, gameObject.transform.position, shooterPosition);
            }
        }
    }

    IEnumerator TurnOffCollider(){
        yield return new WaitForSeconds(0.1f);
        gameObject.GetComponent<SphereCollider>().enabled = false;
    }

	void OnTriggerStay(Collider collision) {
        if (!isServer)  {
            return;
        }

		
		if (collision.gameObject.CompareTag("HexCell") && !objectsAffected.Contains(collision.gameObject)) {
			terrainHealth health = collision.gameObject.GetComponent<terrainHealth>();
			health.CmdTakeDamage(damage);

			objectsAffected.Add(collision.gameObject);
		}
	}

}

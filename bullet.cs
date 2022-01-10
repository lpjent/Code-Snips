using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class bullet : NetworkBehaviour {

	public int damage;
    public float damageSize;
	public float explosionSize;
	public float explosionForce;
	public GameObject explosionPrefab;
    public string weaponName;
    public Vector3 shooterPosition;



	void OnCollisionEnter(Collision collision) {
        //Checks the collision to see if it hit something and will explode.
        if (collision.gameObject.tag == "Player" || collision.gameObject.tag == "HexCell" || collision.gameObject.tag == "bullet") {
            CmdMakeExplosion();
        }
        // end if
	}

	[Command]
	void CmdMakeExplosion() {
        //Creating an explosion when the bullet hits. Uses explosion class object.
        Vector3 localPosition = gameObject.transform.position;
		GameObject explosion = Instantiate(explosionPrefab,localPosition,Quaternion.identity);
        explosion.transform.localScale = new Vector3 (damageSize, damageSize, damageSize);
		ExplosionScript explosionStats = explosion.GetComponent<ExplosionScript>();
		explosionStats.damage = damage;
		explosionStats.explosionSize = explosionSize;
		explosionStats.explosionForce = explosionForce;
        explosionStats.shooterPosition = shooterPosition;
		NetworkServer.Spawn(explosion);
        RpcPlayExplosionPFX(explosion, weaponName);
    }

    [ClientRpc]
    void RpcPlayExplosionPFX(GameObject explosion, string name) {
        //Client side plays explosion fx.
        ParticleSystem[] pfxList = explosion.GetComponent<PFXList>().pfxList;
        Debug.LogError("I am starting an explosion PFX. " + name);
        switch (name)
        {
            case "Rocket Launcher":
                pfxList[0].Play();
                break;
            case "Energy Rifle":
                pfxList[1].Play();
                break;
            case "Sniper":
                pfxList[2].Play();
                break;
            case "Lightning":
                pfxList[3].Play();
                break;
            case "Plasma Ray":
                break;
            default:
                Debug.LogError("Invalid Gun Name in Explosion PFX");
                break;
        }
        explosion.GetComponent<ExplosionAudioManager>().Play(name);
        //gets rid of the bullet.
        Destroy(gameObject);
    }
}

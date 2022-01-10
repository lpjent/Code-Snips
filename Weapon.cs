using System;

namespace UnityEngine
{
	public class Weapon
	{
		public string name;
		public float range;
		public float bulletSpeed;
		public float rateOfFire;
		public int damage;
        public float damageSize;
		public float explosionSize;
		public float explosionForce;
        public string particleName;

		public static Weapon[] GetWeapons() {
			Weapon[] weaponList = new Weapon[5];
			weaponList[0] = new Weapon("Rocket Launcher", 3.0f, 40.0f, 1.75f, 40, 5.0f, 8.0f, 50000.0f);
			weaponList[1] = new Weapon("Energy Rifle", .25f, 200f, .3f, 20, 2.0f, 4.0f, 30000.0f);
			weaponList[2] = new Weapon("Sniper", .5f, 300f, 1.0f, 50, 1.0f, 2.0f, 35000.0f);
			weaponList[3] = new Weapon("Lightning", 0.0f, 0.0f, 5.0f, 100, 2.5f, 3.0f, 45000.0f);
			weaponList[4] = new Weapon("Plasma Ray", 20.0f, 0.0f, 0.1f, 10, 1.0f, 3.0f, 38000.0f);
			return weaponList;
		}

		public Weapon() {
			name = "Default";
			range = 5.0f;
			bulletSpeed = 6.0f;
			rateOfFire = 2.0f;
			damage = 20;
            damageSize = 5.0f;
			explosionSize = 10.0f;
			explosionForce = 40000.0f;
		}
			

		public Weapon (string weaponName, float weaponRange, float weaponBulletSpeed, float weaponRateOfFire, int weaponDamage, float weaponDamageSize, float weaponExplosionSize, float weaponExplosionForce) {
			name = weaponName;
			range = weaponRange;
			bulletSpeed = weaponBulletSpeed;
			rateOfFire = weaponRateOfFire;
			damage = weaponDamage;
            damageSize = weaponDamageSize;
			explosionSize = weaponExplosionSize;
			explosionForce = weaponExplosionForce;
		}


	}
}


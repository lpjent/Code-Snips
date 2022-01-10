using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Cinemachine;

public class PlayerManager : NetworkBehaviour {
    #region Variables and constants.

    #region Movement Variables
    [Header ("Movement variables")]
	//player movement/rotation speed 
	float moveSpeed = 10.0f;
	float rotationSpeed = 120.0f;
	float aimSpeed = 40.0f;
	public float jumpForce;
    [SerializeField] bool bJumping;
    [SerializeField] bool bOnGround;
    float airTime;
    float fuel;
    #endregion

    #region Weapon Variables
    [Header ("Weapon Variables")]
	public GameObject bulletPrefab;
	[SerializeField] Weapon weapon1;
	[SerializeField] Weapon weapon2;
	[SerializeField] Weapon[] weapons;
	GameObject[] gunPrefabs;
	public GameObject weapon1Prefab;
	public GameObject weapon2Prefab;
	[SyncVar] [SerializeField] bool activeWeaponIs1;
	[SerializeField] float fireTime;
    [SerializeField] float offWeaponFireTime;
    public GameObject explosionPrefab;
    [SerializeField] Vector3 shootHere;
	[SerializeField] Vector3 bulletSpawn;
    #endregion

    #region Camera Variables
    [Header ("Camera Variables")]
    [SerializeField] Camera myCam;
	[SyncVar] public int myCamMask;
    public DeathCam myDeathCam;
	[SerializeField] CinemachineFreeLook myVirtualCam;
    [SerializeField] AnimationCurve vCamLerpCurve = AnimationCurve.Linear(0, 0, 1, 1);
    Coroutine zoomInProgress;
    [SerializeField] [SyncVar] int myCount;
	[SerializeField] bool camInitialized;
	[SyncVar] GameObject cameraObject;
	[SyncVar] GameObject vCamObject;
    List<DeathCam> deathCamList = new List<DeathCam>();
    #endregion

    #region Character and animation
    [Header ("Character and animation")]
	public GameObject[] CharacterRigs = new GameObject[2];
    public Animator[] animators = new Animator[2];
	public Animator anim;
    public SkinnedMeshRenderer[] shieldParts;
    [SerializeField] AnimationCurve shieldLerpCurve = AnimationCurve.Linear(0, 0, 1, 1);
    GameObject playerRig;
    #endregion

    #region Character Color
    [SerializeField] GameObject VeeAccessoriesPartToChange;
    [SerializeField] GameObject VeeHeavyPlatingPartToChange;
    [SerializeField] GameObject LancePartToChange;
    Color myColor;
    #endregion

    #region Terrain
    [Header ("Terrain")]
    public GameObject[] AllHexGrid;
    public GameObject[] ActiveHexGrid;
    #endregion

    #region Lives and Respawn
    [Header ("Lives, and respawn data")]
    //Lives. Number of total lives should be selectable on menu screen later.
    [SyncVar] public int livesLeft;
	[SyncVar] public bool noLivesLeft;
    public float spawnDropHeight;
    public int runOnce;
    public int hexRandom;
    GameObject currentPoint;
    public Transform spawnPoint;
	PlayerSelectionContainer myChoices;
    #endregion

    #region UI Elements
    [Header("UI Elements")]
    public GameObject AliveContainer;
    public Image FireRateBarImage;
    //public Text LivesText;
    public Image LivesImage1;
    public Image LivesImage2;
    public Image LivesImage3;
    private Sprite LivesSprite;
    public Text WeaponText;
    public Image WeaponImage;
    private Sprite Weapon1Sprite;
    private Sprite Weapon2Sprite;
    private float FuelBarMaxImageWidth;
    //public Text FuelText;
    public Image FuelBarMaskImage;
    public GameObject CountdownContainer;
    public Text CountdownText;
    public GameObject OutOfGameContainer;
    public GameObject GameOverContainer;
    public Text WinnerText;
    public Image[] directionalIndicator;
    #endregion

    #region Bools
    [Header("Flags")]
    //bools for paused, death, etc.
    public bool isPaused;
	private bool camReleased;
    [SyncVar]
    public bool fadeComplete;
    public bool isGameOver;
    [SyncVar]
    public bool setupComplete;
    private bool zoom;
    private bool canZoom;
    #endregion


    #region Constants
    private const int NUMOFHEXESTOREGENERATE = 10;

    //Constants for air movement.
    private const float COUNTDOWNSTARTVALUE = 3f;
    private const float MAXFUEL = 20.0f;
    private const float FUELBURN = 0.1f;
    private const float JUMPBURN = 5.0f;
    private const float FUELREGEN = 0.5f;
    #endregion

    #endregion

    #region Intialization
    // Use this for initialization
    void Start () {
        fadeComplete = false;
        isGameOver = false;
        setupComplete = false;
        zoom = false;
        canZoom = false;

		activeWeaponIs1 = true;
        bJumping = false;

		myChoices = gameObject.GetComponent<PlayerSelectionContainer>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GetHexGrid();

        InitializeGameStartUI();
        isPaused = true;

        StartCoroutine(GetAllActiveHexes());
		StartCoroutine(DelaySetup());
        //StartCoroutine(CountdownTimeLogic());
        //StartCoroutine(SpawnPlayer());

        runOnce = 1;

		jumpForce = 12000.0f;
        fuel = MAXFUEL;
        FuelBarMaxImageWidth = FuelBarMaskImage.rectTransform.rect.width;
        livesLeft = 3;
        //LivesText.text = "Lives\n" + livesLeft;
        WinnerText.text = "Player # Wins!";
        noLivesLeft = false;
        //Debug.Log("Lives left " + livesLeft);

        LivesSprite = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().CharacterLivesIcons[myChoices.characterSelection];
        LivesImage1.sprite = LivesSprite;
        LivesImage2.sprite = LivesSprite;
        LivesImage3.sprite = LivesSprite;

        camReleased = false;
		myCount = -1;
        if (cameraObject == null && GameObject.Find("CameraDistributor") != null)
        {

            Debug.Log("Camera Object: " + cameraObject);
            Debug.Log("Gettng Camera");
        }

    }

    public void FadeInComplete() {
        StartCoroutine(FadeInCompleteCoroutine());
    }

    public IEnumerator FadeInCompleteCoroutine() {
        CmdSendFadeComplete();

        bool allPlayersLoaded = false;

        while(!allPlayersLoaded) {
            yield return null;

            // retrieving player list here in case a player leaves prior to all others being ready
            GameObject[] playerList = GameObject.FindGameObjectsWithTag("Player");

            foreach (var player in playerList) {
                if (!player.GetComponent<PlayerManager>().fadeComplete || !setupComplete) {
                    allPlayersLoaded = false;
                    break;
                }
                // end if

                allPlayersLoaded = true;
            }
            // end foreach
        }
        // end while

        StartCoroutine(CountdownTimeLogic());
        // end if
    }
    [Command]
    public void CmdSendFadeComplete() {
        fadeComplete = true;
    }

	void GetHexGrid() {
		if (GameObject.Find("HexSpawner") != null && isLocalPlayer) {
			ActiveHexGrid = GameObject.FindGameObjectsWithTag("HexCell");
		}
	}

    private IEnumerator GetAllActiveHexes() {
        yield return new WaitForSeconds(1f);
        
        AllHexGrid = GameObject.FindGameObjectsWithTag("HexCell");
    }

    private IEnumerator AllHexesInactive() {
        for (int i = NUMOFHEXESTOREGENERATE; i > 0; i--) {
            while (true) {
                var rng = Random.Range(0, AllHexGrid.Length);

                if (!AllHexGrid[rng].activeSelf) {
                    RpcReactivateHex(rng);
                    break;
                }

                yield return null;
            }
            // end while
        }
        // end for
    }

    [ClientRpc]
    private void RpcReactivateHex(int index) {
        Debug.LogError("index of hex passed to method=" + index);
        AllHexGrid[index].SetActive(true);
    }


    private IEnumerator DelaySetup() {
		yield return new WaitForSeconds(0.1f);
        if (isLocalPlayer)
		    CmdCharacterSetup();
	}

	[Command]
	void CmdCharacterSetup () {
		myChoices = gameObject.GetComponent<PlayerSelectionContainer>();
		weapons = Weapon.GetWeapons();
		gunPrefabs = gameObject.GetComponent<GunPrefabContainer>().getGunPrefabs();


        playerRig = CharacterRigs[myChoices.characterSelection];
        anim = animators[myChoices.characterSelection];
        myColor = myChoices.characterColorSelection;

        if (playerRig == CharacterRigs[0])
        {
            //Vee color swap
            Material matRef = VeeAccessoriesPartToChange.GetComponent<SkinnedMeshRenderer>().material;
            matRef.SetColor("_Color", myColor);
            matRef = VeeHeavyPlatingPartToChange.GetComponent<SkinnedMeshRenderer>().material;
            matRef.SetColor("_Color", myColor);

        }
        else if (playerRig == CharacterRigs[1])
        {
            //Lance color swap
            Material matRef = LancePartToChange.GetComponent<SkinnedMeshRenderer>().materials[3];
            matRef.SetColor("_Color", myColor);
        }

        foreach (GameObject rig in CharacterRigs)
        {
            rig.SetActive(false);
        }
        playerRig.SetActive(true);


        //Set weapons.
        foreach (GameObject gun in gunPrefabs)
        {
            gun.SetActive(true);
        }

        weapon1 = weapons[myChoices.weaponSelection1];
        weapon1Prefab = gunPrefabs[myChoices.weaponSelection1];
        Weapon1Sprite = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().WeaponImages[myChoices.weaponSelection1];
        weapon2 = weapons[myChoices.weaponSelection2];
        weapon2Prefab = gunPrefabs[myChoices.weaponSelection2];
        Weapon2Sprite = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().WeaponImages[myChoices.weaponSelection2];


        AimWeapon gunScript1 = weapon1Prefab.GetComponent<AimWeapon>();
        AimWeapon gunScript2 = weapon2Prefab.GetComponent<AimWeapon>();

        gunScript1.pseudoParent = gunScript1.pseudoParentTransforms[myChoices.characterSelection];
        weapon1Prefab.GetComponent<AimWeapon>().rightHand = gunScript1.rightHandTransforms[myChoices.characterSelection];
        gunScript1.leftHand = gunScript1.leftHandTransforms[myChoices.characterSelection];

        gunScript2.pseudoParent = gunScript2.pseudoParentTransforms[myChoices.characterSelection];
        gunScript2.rightHand = gunScript2.rightHandTransforms[myChoices.characterSelection];
        gunScript2.leftHand = gunScript2.leftHandTransforms[myChoices.characterSelection];

		RpcCharacterSetup();
	}

	[ClientRpc]
	void RpcCharacterSetup () {
		myChoices = gameObject.GetComponent<PlayerSelectionContainer>();
		weapons = Weapon.GetWeapons();
		gunPrefabs = gameObject.GetComponent<GunPrefabContainer>().getGunPrefabs();

        playerRig = CharacterRigs[myChoices.characterSelection];
        anim = animators[myChoices.characterSelection];
        myColor = myChoices.characterColorSelection;

        if(playerRig == CharacterRigs[0])
        {
            //Vee color swap
            Material matRef = VeeAccessoriesPartToChange.GetComponent<SkinnedMeshRenderer>().material;
            matRef.SetColor("_Color", myColor);
            matRef = VeeHeavyPlatingPartToChange.GetComponent<SkinnedMeshRenderer>().material;
            matRef.SetColor("_Color", myColor);

        } else if(playerRig == CharacterRigs[1])
        {
            //Lance color swap
            Material matRef = LancePartToChange.GetComponent<SkinnedMeshRenderer>().materials[3];
            matRef.SetColor("_Color", myColor);
        }

        foreach (GameObject rig in CharacterRigs)
        {
            rig.SetActive(false);
        }
        playerRig.SetActive(true);


        //Set weapons.
        foreach (GameObject gun in gunPrefabs)
        {
            gun.SetActive(true);
        }
        //set the guns from the menu choices.
        weapon1 = weapons[myChoices.weaponSelection1];
		weapon1Prefab = gunPrefabs[myChoices.weaponSelection1];
        weapon2 = weapons[myChoices.weaponSelection2];
        weapon2Prefab = gunPrefabs[myChoices.weaponSelection2];

        //Set up the aim for the weapons.
        AimWeapon gunScript1 = weapon1Prefab.GetComponent<AimWeapon>();
        AimWeapon gunScript2 = weapon2Prefab.GetComponent<AimWeapon>();
        
        //Animation stuff
        gunScript1.pseudoParent = gunScript1.pseudoParentTransforms[myChoices.characterSelection];
        weapon1Prefab.GetComponent<AimWeapon>().rightHand = gunScript1.rightHandTransforms[myChoices.characterSelection];
        gunScript1.leftHand = gunScript1.leftHandTransforms[myChoices.characterSelection];

        gunScript2.pseudoParent = gunScript2.pseudoParentTransforms[myChoices.characterSelection];
        gunScript2.rightHand = gunScript2.rightHandTransforms[myChoices.characterSelection];
        gunScript2.leftHand = gunScript2.leftHandTransforms[myChoices.characterSelection];

        //Turns on the weapon sprites for the local player's selection
        if (isLocalPlayer) {
            playerRig.GetComponent<PlayerIK>().bIsLocal = true;
            CmdGetCamera();

            Weapon1Sprite = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().WeaponImages[myChoices.weaponSelection1];
            Weapon2Sprite = GameObject.Find("LobbyManager").GetComponent<LobbyManager>().WeaponImages[myChoices.weaponSelection2];
        }
    }

	[Command] 
	void CmdGetCamera() {

        //Assigns the cameras to the player objects during initialization.
		CameraDistributor camDist = GameObject.Find("CameraDistributor").GetComponent<CameraDistributor>();
		Transform cameraReference =  gameObject.transform.Find("LookHere");
        int slot = gameObject.GetComponent<PlayerSelectionContainer>().slotNumber + 1;

		switch (slot) {
		case 1:
			vCamObject = camDist.Player1VCam;
			camDist.Player1VCam.GetComponent<CinemachineFreeLook>().enabled = true;
			camDist.Player1VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
			camDist.Player1VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

			cameraObject = camDist.Player1Camera;
			myCam = cameraObject.GetComponent<Camera>();
			myCam.enabled = true;
			myCamMask = myCam.cullingMask;
			break;
		case 2:
			vCamObject = camDist.Player2VCam;
			camDist.Player2VCam.GetComponent<CinemachineFreeLook>().enabled = true;
			camDist.Player2VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
			camDist.Player2VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

			cameraObject = camDist.Player2Camera;
			myCam = cameraObject.GetComponent<Camera>();
			myCam.enabled = true;
			myCamMask = myCam.cullingMask;

			break;
		case 3:
			vCamObject = camDist.Player3VCam;
			camDist.Player3VCam.GetComponent<CinemachineFreeLook>().enabled = true;
			camDist.Player3VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
			camDist.Player3VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

			cameraObject = camDist.Player3Camera;
			myCam = cameraObject.GetComponent<Camera>();
			myCam.enabled = true;
			myCamMask = myCam.cullingMask;
			break;
		case 4:
			vCamObject = camDist.Player4VCam;
			camDist.Player4VCam.GetComponent<CinemachineFreeLook>().enabled = true;
			camDist.Player4VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
			camDist.Player4VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

			cameraObject = camDist.Player4Camera;
			myCam = cameraObject.GetComponent<Camera>();
			myCam.enabled = true;
			myCamMask = myCam.cullingMask;

			break;
		default :
			cameraObject = null;
			myCam = null;
			Debug.LogError("Too many cameras assigned.");
			break;
		}


        myVirtualCam = vCamObject.GetComponent<CinemachineFreeLook>();
        //Finds the invisible target object to aim the gun at, and assigns it to the active guns for the camera.
        weapon1Prefab.GetComponent<AimWeapon>().shootTo = cameraObject.transform.Find("ShootHere").position;
		weapon2Prefab.GetComponent<AimWeapon>().shootTo = cameraObject.transform.Find("ShootHere").position;
        weapon1Prefab.GetComponent<AimWeapon>().pseudoParent = playerRig.GetComponent<PlayerIK>().gunPseudoParent;
        weapon2Prefab.GetComponent<AimWeapon>().pseudoParent = playerRig.GetComponent<PlayerIK>().gunPseudoParent;

        playerRig.GetComponent<PlayerIK>().headTarget = cameraObject.transform.Find("ShootHere");

        foreach (GameObject gun in gunPrefabs)
        {
            gun.SetActive(false);
        }

        weapon2Prefab.SetActive(false);
        weapon1Prefab.SetActive(true);

        playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon1Prefab.GetComponent<AimWeapon>().leftHand;
        playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon1Prefab.GetComponent<AimWeapon>().rightHand;

		RpcCameraSetup();

    }

	[ClientRpc] void RpcCameraSetup() {
        //client side camera settings.
        CameraDistributor camDist = GameObject.Find("CameraDistributor").GetComponent<CameraDistributor>();
        Transform cameraReference = gameObject.transform.Find("LookHere");
        int slot = gameObject.GetComponent<PlayerSelectionContainer>().slotNumber + 1;
        
        switch (slot)
        {
            case 1:
                vCamObject = camDist.Player1VCam;
                camDist.Player1VCam.GetComponent<CinemachineFreeLook>().enabled = true;
                camDist.Player1VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
                camDist.Player1VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

                cameraObject = camDist.Player1Camera;
                myCam = cameraObject.GetComponent<Camera>();
                myCam.enabled = true;
                myCamMask = myCam.cullingMask;
                break;
            case 2:
                vCamObject = camDist.Player2VCam;
                camDist.Player2VCam.GetComponent<CinemachineFreeLook>().enabled = true;
                camDist.Player2VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
                camDist.Player2VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

                cameraObject = camDist.Player2Camera;
                myCam = cameraObject.GetComponent<Camera>();
                myCam.enabled = true;
                myCamMask = myCam.cullingMask;

                break;
            case 3:
                vCamObject = camDist.Player3VCam;
                camDist.Player3VCam.GetComponent<CinemachineFreeLook>().enabled = true;
                camDist.Player3VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
                camDist.Player3VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

                cameraObject = camDist.Player3Camera;
                myCam = cameraObject.GetComponent<Camera>();
                myCam.enabled = true;
                myCamMask = myCam.cullingMask;
                break;
            case 4:
                vCamObject = camDist.Player4VCam;
                camDist.Player4VCam.GetComponent<CinemachineFreeLook>().enabled = true;
                camDist.Player4VCam.GetComponent<CinemachineFreeLook>().m_Follow = cameraReference;
                camDist.Player4VCam.GetComponent<CinemachineFreeLook>().m_LookAt = cameraReference;

                cameraObject = camDist.Player4Camera;
                myCam = cameraObject.GetComponent<Camera>();
                myCam.enabled = true;
                myCamMask = myCam.cullingMask;

                break;
            default:
                cameraObject = null;
                myCam = null;
                Debug.LogError("Too many cameras assigned.");
                break;
        }
        
		camReleased = false;
        myVirtualCam = vCamObject.GetComponent<CinemachineFreeLook>();
        if (isLocalPlayer) myVirtualCam.m_YAxis.m_InputAxisName = "Mouse Y";

        weapon1Prefab.GetComponent<AimWeapon>().shootTo = cameraObject.transform.Find("ShootHere").position;
        weapon2Prefab.GetComponent<AimWeapon>().shootTo = cameraObject.transform.Find("ShootHere").position;
        weapon1Prefab.GetComponent<AimWeapon>().pseudoParent = playerRig.GetComponent<PlayerIK>().gunPseudoParent;
        weapon2Prefab.GetComponent<AimWeapon>().pseudoParent = playerRig.GetComponent<PlayerIK>().gunPseudoParent;

        playerRig.GetComponent<PlayerIK>().headTarget = cameraObject.transform.Find("ShootHere");
        

        //Turning off the gun models and turning on the one we're using.
        foreach (GameObject gun in gunPrefabs)
        {
            gun.SetActive(false);
        }

        weapon2Prefab.SetActive(false);
        weapon1Prefab.SetActive(true);
        //Getting the stats and filling them into the weapons.
        playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon1Prefab.GetComponent<AimWeapon>().leftHand;
        playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon1Prefab.GetComponent<AimWeapon>().rightHand;
        //Allows you to see while dead.
        myDeathCam = new DeathCam
        {
            cullingMask = myCam.cullingMask,
            playerCam = cameraObject,
            vCam = myVirtualCam,
            isActive = true
        };

        //turns on the zoom if you choose sniper as your primary weapon.
        if (isLocalPlayer)
        {
            if (weapon1.name == "Sniper")
            {
                canZoom = true;
            }
            CmdTurnOffCharacterBits();
            CmdSetupComplete();
        }
    }

    [Command] void CmdSetupComplete()
    {
        //Lets the clients know we're done with setup.
        setupComplete = true;
    }

    #endregion

    #region main gameplay

    #region Fixed update and Update
    void FixedUpdate() {
        if (isGameOver)
            return;

        if (transform.parent != null)
        Debug.Log("parent name=" + transform.parent.name);

		
		if (!isLocalPlayer) {
			
			if (myCam != null && myCam.enabled) {
				Debug.Log("Camera Object: " + cameraObject);
				Debug.Log("MyCam: " + myCam);
				myCam.enabled = false;
			}

            transform.Find("Canvas").gameObject.SetActive(false);
			return;
		}
        //close if

        GetHexGrid();

        if (ActiveHexGrid.Length == 0 && AllHexGrid.Length > 0 && isServer) {
            StartCoroutine(AllHexesInactive());
        }

        if (isPaused) {
			if (noLivesLeft && camReleased) {
				if (Input.GetKeyDown(KeyCode.Q)) {
					PrevPlayerCam();
				}
				if (Input.GetKeyDown(KeyCode.E)) {
					NextPlayerCam();
				}
			}

        }
        // end if
        else {
            GameInput();
            UIUpdate();

            // Ground Check Stuff (animations, fuel regen, etc.
            if (bOnGround)
            {
                if (gameObject.GetComponent<PlayerAudioManager>().IsPlaying("Burn"))
                {
                    gameObject.GetComponent<PlayerAudioManager>().Stop("Burn");
                }
                if (fuel < MAXFUEL && (fuel + FUELREGEN) <= MAXFUEL)
                {
                    fuel += FUELREGEN;
                }
                else fuel = MAXFUEL;
            }
            if (bOnGround != !anim.GetBool("bJumping"))
            {
                CmdSetAnimation(gameObject, "bJumping", !bOnGround, 1.0f);
            }

            GunInput();
        }
        // end else


       
    }
    //endFixedUpdate

    private void Update() {
        if (!isLocalPlayer) {
            return;
        }
        // end if

        if (!isPaused) {
            Vector3 target = new Vector3();
            if (Input.GetKeyUp(KeyCode.Q)) {
                //Debug.Log("Switching Weapons");

                CmdWeaponSwap();
            }

            target = cameraObject.transform.Find("ShootHere").position;
            CmdDoIK(target);
        }
        // end if
    }
    #endregion

    #region Input, UI, Movement
    void GameInput()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        //check if on ground
        bOnGround = gameObject.GetComponent<PlayerGravityBehavior>().bOnGround;

        //Rotation speed will be lower if zoomed.
        float rotSpeedAltered = rotationSpeed;

        //zoom code
        if (canZoom)
        {
            bool isZoomButtonPressed = Input.GetMouseButton(1);
            if (isZoomButtonPressed && !zoom)
            {
                zoom = true;
                
                if (zoomInProgress != null)
                    StopCoroutine(zoomInProgress);
                zoomInProgress = StartCoroutine(ZoomIn());

                myVirtualCam.m_YAxis.m_MaxSpeed = 0.75f;
                rotSpeedAltered -= 40;
            }
            else if (zoom && !isZoomButtonPressed)
            {
                zoom = false;
                if (zoomInProgress != null)
                    StopCoroutine(zoomInProgress);
                zoomInProgress = StartCoroutine(ZoomOut());
                myVirtualCam.m_YAxis.m_MaxSpeed = 2.0f;
            }
            else if (zoom)
            {
                rotSpeedAltered -= 40;
            }
        }
        else if (zoom)
        {
            zoom = false;

            if (zoomInProgress != null)
                StopCoroutine(zoomInProgress);
            zoomInProgress = StartCoroutine(ZoomOut());
            myVirtualCam.m_YAxis.m_MaxSpeed = 2.0f;
        }

        //Animation Inputs
        float hInput = Input.GetAxis("Horizontal");
        float vInput = Input.GetAxis("Vertical");
        CmdSetWalkAnim(hInput, vInput);


        //Movement inputs
        float x = hInput * moveSpeed;
        float z = vInput * moveSpeed;
        float y = Input.GetAxis("Mouse X") * Time.deltaTime * rotSpeedAltered;


        //if in air, slow movement.
        if (!bOnGround) {
            if (fuel + .05f <= MAXFUEL)
                fuel += .05f;
            else fuel = MAXFUEL;
            //if fuel for air movement, do not reduce speed.
            if (fuel > 0.0f )
            {
                if (x != 0 || z != 0) {
                    fuel -= FUELBURN;
                }
                if (!gameObject.GetComponent<PlayerAudioManager>().IsPlaying("Burn"))
                {
                    gameObject.GetComponent<PlayerAudioManager>().Play("Burn");
                }
            }//end if
            //else reduce speed.
            else
            {
                x = x / 3;
                z = z / 3;
            }
        }

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.AddRelativeForce(x, 0.0f, z, ForceMode.Impulse);

        

        //mouseRotate
        transform.Rotate(0, y, 0);

        if (Input.GetKey(KeyCode.Space))
        {
            //Debug.Log ("Spacebar Pressed");
            Jump();
        }//end if
        if (Input.GetKeyUp(KeyCode.Space))
        {
            airTime = 0.25f;
            bJumping = false;
            
        }


        
    }

    void UIUpdate()
    {
        InitializeAliveUI();

        // set Lives remaining icons to lives left
        if (livesLeft < 3)
        {
            LivesImage3.gameObject.SetActive(false);

            if (livesLeft < 2)
            {
                LivesImage2.gameObject.SetActive(false);

                if (livesLeft < 1)
                {
                    LivesImage1.gameObject.SetActive(false);
                }
                // end if
            }
            // end if
        }
        // end if

        //LivesText.text = "Lives\n" + livesLeft;
        //FuelText.text = "Fuel: " + fuel.ToString("0.0");
        FuelBarMaskImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, FuelBarMaxImageWidth * (fuel / MAXFUEL));

        

        if (activeWeaponIs1)
        {
            WeaponText.text = weapon1.name;
            WeaponImage.sprite = Weapon1Sprite;
        }
        // end if
        else
        {
            WeaponText.text = weapon2.name;
            WeaponImage.sprite = Weapon2Sprite;
        }
        // end else
    }

    void GunInput()
    {

        //Get mouse input to fire
        float RoF = weapon1.rateOfFire;
        if (!activeWeaponIs1)
        {
            RoF = weapon2.rateOfFire;
        }

        // determine how much fill the fire rate UI element should have
        // should be opposite of the ability to fire, since if you can't fire, this bar should not be full
        if (fireTime > Time.realtimeSinceStartup - RoF)
        {
            FireRateBarImage.fillAmount = (Time.realtimeSinceStartup - fireTime) / RoF;
        }
        // end if
        else
        {
            FireRateBarImage.fillAmount = 1;
        }
        // end else

        if (Input.GetMouseButton(0) && fireTime <= Time.realtimeSinceStartup - RoF)
        {
            fireTime = Time.realtimeSinceStartup;

            shootHere = cameraObject.transform.Find("ShootHere").position;
            bulletSpawn = weapon1Prefab.GetComponent<AimWeapon>().shootFrom.position;
            float weaponRange = weapon1.range;
            float bulletSpeed = weapon1.bulletSpeed;
            int damage = weapon1.damage;
            float damageSize = weapon1.damageSize;
            float explosionSize = weapon1.explosionSize;
            float explosionForce = weapon1.explosionForce;
            string currentWeapon = weapon1.name;

            if (!activeWeaponIs1)
            {
                weaponRange = weapon2.range;
                bulletSpeed = weapon2.bulletSpeed;
                damage = weapon2.damage;
                damageSize = weapon2.damageSize;
                explosionSize = weapon2.explosionSize;
                explosionForce = weapon2.explosionForce;
                currentWeapon = weapon2.name;
                bulletSpawn = weapon2Prefab.GetComponent<AimWeapon>().shootFrom.position;

            }
            RaycastHit hitInfo;
            if (Physics.Raycast(cameraObject.transform.position, cameraObject.transform.TransformDirection(Vector3.forward), out hitInfo))
            {
                shootHere = hitInfo.point;
            }

            CmdFireWeapon(bulletSpawn, shootHere, weaponRange, bulletSpeed, damage, damageSize, explosionSize, explosionForce, currentWeapon, gameObject.transform.position);
        }
        else if (!Input.GetMouseButton(0))
        {
            if (weapon1Prefab.GetComponent<AimWeapon>().gunPfx.isPlaying && weapon1.name == "Plasma Ray")
            {
                CmdStopGunFX(0);
            }
            else if (weapon2Prefab.GetComponent<AimWeapon>().gunPfx.isPlaying && weapon2.name == "Plasma Ray")
            {
                CmdStopGunFX(1);
            }
        }
    }

    void Jump()
    {

        //TODO: Play jump animation masked to lower body

        Vector3 jumpDir = gameObject.GetComponent<PlayerGravityBehavior>().GetUpDir();
        if (fuel >= 0.0f)
        {
            if (!bJumping && fuel >= JUMPBURN)
            {
                bJumping = true;
                airTime = 0.25f;
                GetComponent<Rigidbody>().AddForce(jumpDir * jumpForce);
                fuel -= JUMPBURN;
                gameObject.GetComponent<PlayerAudioManager>().Play("Jump");
            }
            else if (fuel >= FUELBURN)
            {
                airTime += Time.deltaTime;
                if (airTime > 4f) airTime = 4.0f;
                GetComponent<Rigidbody>().AddForce(jumpDir * 500f * airTime);
                fuel -= FUELBURN;
                if (!gameObject.GetComponent<PlayerAudioManager>().IsPlaying("Burn"))
                {
                    gameObject.GetComponent<PlayerAudioManager>().Play("Burn");
                }

            }
            else
            {
                airTime = 0;
            }
        }
    }//end Jump()
    #endregion

    #region weapon swap
    [Command] void CmdWeaponSwap()
    {
        
        // Server side, Changes weapon 1 to off or on as necessary, swaps models between weapon 1 and 2.
        activeWeaponIs1 = !activeWeaponIs1;
        if (activeWeaponIs1)
        {
            weapon2Prefab.SetActive(false);
            weapon1Prefab.SetActive(true);
            playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon1Prefab.GetComponent<AimWeapon>().leftHand;
            playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon1Prefab.GetComponent<AimWeapon>().rightHand;
            
        }
        else
        {
            weapon1Prefab.SetActive(false);
            weapon2Prefab.SetActive(true);
            playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon2Prefab.GetComponent<AimWeapon>().leftHand;
            playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon2Prefab.GetComponent<AimWeapon>().rightHand;
        }
        RpcWeaponSwap(activeWeaponIs1);
    }

    [ClientRpc] void RpcWeaponSwap(bool activeWeaponIs1Client)
    {


        // Client side, changes weapons
        if (activeWeaponIs1Client)
        {
            
            weapon2Prefab.SetActive(false);
            weapon1Prefab.SetActive(true);
            playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon1Prefab.GetComponent<AimWeapon>().leftHand;
            playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon1Prefab.GetComponent<AimWeapon>().rightHand;
            if (weapon1.name == "Sniper")
            {
                canZoom = true;
            }
            else canZoom = false;
        }
        // end if
        else
        {
            weapon1Prefab.SetActive(false);
            weapon2Prefab.SetActive(true);
            playerRig.GetComponent<PlayerIK>().leftHandTarget = weapon2Prefab.GetComponent<AimWeapon>().leftHand;
            playerRig.GetComponent<PlayerIK>().rightHandTarget = weapon2Prefab.GetComponent<AimWeapon>().rightHand;
            if (weapon2.name == "Sniper")
            {
                canZoom = true;
            }
            else canZoom = false;
        }
        // End else
        if (isLocalPlayer)
        {
            //prevents weapon swapping from switching the fire cooldown off.
            float lastFireTime = offWeaponFireTime;
            offWeaponFireTime = fireTime;
            fireTime = lastFireTime;
        }
        //end if
        
    }
    #endregion

    #region Gunfire
    [Command]
    void CmdFireWeapon(Vector3 spawnPoint, Vector3 target, float weaponRange, float bulletSpeed, int damage, float damageSize, float explosionSize, float explosionForce, string name, Vector3 shooterPosition)
    {

        //play firing animation
        anim.ResetTrigger("tFiring");
        anim.SetTrigger("tFiring");
        RaycastHit hitInfo;



        //get shoot direction
        var heading = target - spawnPoint;
        var shotVector = heading.normalized;

        if (name == "Lightning" || name == "Plasma Ray" || name == "Punch")
        {
            if (name == "Lightning")
            {
                //special physics for ray gun Lighting
                if (Physics.Raycast(spawnPoint, heading, out hitInfo))
                {
                    MakeExplosion(damage, damageSize, explosionSize, explosionForce, hitInfo.point, name, shooterPosition);
                }
                CmdPlayGunPFX();
            }
            //end if lightning
            if (name == "Plasma Ray")
            {
                //Special physics for raygun Plasma Ray
                if (Physics.Raycast(spawnPoint, heading, out hitInfo, weaponRange))
                {
                    MakeExplosion(damage, damageSize, explosionSize, explosionForce, hitInfo.point, name, shooterPosition);
                }
                CmdPlayGunPFX();
            }
            //end if raygun

            //implement special physics for punch

        }
        //end if

        else
        {
            //CreateBullet
            GameObject newBullet = Instantiate(bulletPrefab, spawnPoint, Quaternion.LookRotation(shotVector, gameObject.GetComponent<PlayerGravityBehavior>().GetUpDir()));
            //Impulse on bullet
            newBullet.GetComponent<Rigidbody>().velocity = shotVector * bulletSpeed;

            //set bits of bullet code for later use in explosions.
            bullet bulletCode = newBullet.GetComponent<bullet>();
            bulletCode.damage = damage;
            bulletCode.damageSize = damageSize;
            bulletCode.explosionForce = explosionForce;
            bulletCode.explosionSize = explosionSize;
            bulletCode.weaponName = name;
            bulletCode.shooterPosition = shooterPosition;

            //spawnBullet
            NetworkServer.Spawn(newBullet);
            CmdPlayBulletPFX(newBullet, name);

            Destroy(newBullet, weaponRange);
            CmdPlayGunPFX();
        }
        //end else


    }


    void MakeExplosion(int damage, float damageSize, float explosionSize, float explosionForce, Vector3 hitPosition, string name, Vector3 shooterPosition)
    {
        //Creating a boop on explosion. Affects players, and damages ground.
        GameObject explosion = Instantiate(explosionPrefab, hitPosition, Quaternion.identity);
        explosion.transform.localScale = new Vector3(damageSize, damageSize, damageSize);
        ExplosionScript explosionStats = explosion.GetComponent<ExplosionScript>();
        explosionStats.damage = damage;
        explosionStats.explosionSize = explosionSize;
        explosionStats.explosionForce = explosionForce;
        explosionStats.shooterPosition = shooterPosition;
        NetworkServer.Spawn(explosion);

        //Plays the explosion FX
        CmdPlayExplosionPFX(explosion, name);
    }

    [Command]
    void CmdPlayBulletPFX(GameObject bullet, string name)
    {
        //Plays the bullet FX (Visible rockets! Yay!)
        RpcPlayBulletPFX(bullet, name);
    }

    [ClientRpc]
    void RpcPlayBulletPFX(GameObject bullet, string name)
    {
        //client side play bullets.
        switch (name)
        {
            case "Rocket Launcher":
                bullet.GetComponent<PFXManager>().bulletPFX[0].Play();
                break;
            case "Energy Rifle":
                bullet.GetComponent<PFXManager>().bulletPFX[1].Play();
                break;
            case "Sniper":
                bullet.GetComponent<PFXManager>().bulletPFX[2].Play();
                break;
        }
    }



    [Command]
    void CmdPlayGunPFX()
    {
        //Server play gun flash
        RpcPlayGunPFX();
    }

    [ClientRpc]
    void RpcPlayGunPFX()
    {
        //client play gun flash.
        if (activeWeaponIs1)
        {
            weapon1Prefab.GetComponent<AimWeapon>().gunPfx.Play();
            if (!weapon1Prefab.GetComponent<AudioSource>().isPlaying)
                weapon1Prefab.GetComponent<AudioSource>().Play();
        }
        else
        {
            weapon2Prefab.GetComponent<AimWeapon>().gunPfx.Play();
            if (!weapon2Prefab.GetComponent<AudioSource>().isPlaying)
                weapon2Prefab.GetComponent<AudioSource>().Play();
        }
    }

    [Command]
    void CmdStopGunFX(int weapon)
    {
        // Server Stops and clears particles for gunfire.
        if (weapon == 0)
        {
            weapon1Prefab.GetComponent<AimWeapon>().gunPfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            weapon1Prefab.GetComponent<AudioSource>().Stop();
        }
        else
        {
            weapon2Prefab.GetComponent<AimWeapon>().gunPfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            weapon2Prefab.GetComponent<AudioSource>().Stop();
        }
        RpcStopGunFX(weapon);
    }

    [ClientRpc]
    void RpcStopGunFX(int weapon)
    {
        // Client Stops and clears particles for gunfire.
        if (weapon == 0)
        {
            weapon1Prefab.GetComponent<AimWeapon>().gunPfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            weapon1Prefab.GetComponent<AudioSource>().Stop();
        }
        else
        {
            weapon2Prefab.GetComponent<AimWeapon>().gunPfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            weapon2Prefab.GetComponent<AudioSource>().Stop();
        }
    }

    [Command]
    void CmdPlayExplosionPFX(GameObject explosion, string name)
    {
        //Server plays the FX for the explosion.
        RpcPlayExplosionPFX(explosion, name);
    }

    [ClientRpc]
    void RpcPlayExplosionPFX(GameObject explosion, string name)
    {
        //Client plays the FX for the explosion.
        ParticleSystem[] pfxList = explosion.GetComponent<PFXList>().pfxList;
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
    }

    [ClientRpc]
    public void RpcGetBoopedSon(float explosionSize, float explosionForce, Vector3 explosion, Vector3 enemyPosition)
    {

        //Client reaction to explosion volume.
        Vector3 playerUp = gameObject.GetComponent<PlayerGravityBehavior>().GetUpDir();
        Vector3 positionOffset = Vector3.Scale(new Vector3(0f, 1.5f, 0f), playerUp);
        Vector3 playerPosition = gameObject.transform.position + positionOffset;
        Vector3 direction = playerPosition - explosion;
        float distance = direction.magnitude;
        direction = direction.normalized;

        explosionForce = ((explosionSize - distance) / explosionSize) * explosionForce;

        Vector3 forceDirection = direction * explosionForce;

        gameObject.GetComponent<Rigidbody>().AddForce(forceDirection);
        gameObject.GetComponent<Rigidbody>().AddForce(playerUp * explosionForce);
        
        //Does some math based on where you got shot from.
        //Debug.LogError("Enemy Position: " + enemyPosition);
        Vector3 localPosition = transform.InverseTransformPoint(enemyPosition);
        //Debug.LogError("Local Position: " + localPosition);
        float angle = Mathf.Rad2Deg * Mathf.Atan2(localPosition.z, localPosition.x);
        Vector3 shooterDirection = enemyPosition - gameObject.transform.position;
        
        //Debug.LogError("My angle is: " + angle.ToString());

        //Plays the shield FX for being booped.
        StartCoroutine(ShieldAlphaSlide());
        if (isLocalPlayer)
        {

            if (shooterDirection.magnitude >= 2)
            {
                if (localPosition.x >= 0 && localPosition.z >= 0)
                {
                    if (localPosition.z >= localPosition.x)
                    {
                        //Debug.LogError("Directional Indicator Up");
                        StartCoroutine(DirectionalIndicatorFlash(0));
                    }
                    else if (localPosition.x >= localPosition.z)
                    {
                        //Debug.LogError("Directional Indicator Right");
                        StartCoroutine(DirectionalIndicatorFlash(1));
                    }
                }
                else if (localPosition.z >= 0 && localPosition.x <= 0)
                {
                    if (localPosition.z >= Mathf.Abs(localPosition.x))
                    {
                        //Debug.LogError("Directional Indicator Up");
                        StartCoroutine(DirectionalIndicatorFlash(0));
                    }
                    else if (Mathf.Abs(localPosition.x) >= localPosition.z)
                    {
                        //Debug.LogError("Directional Indicator Left");
                        StartCoroutine(DirectionalIndicatorFlash(3));
                    }
                }
                else if (localPosition.x <= 0 && localPosition.z <= 0)
                {
                    if (Mathf.Abs(localPosition.z) <= Mathf.Abs(localPosition.x))
                    {
                        //Debug.LogError("Directional Indicator Left");
                        StartCoroutine(DirectionalIndicatorFlash(3));
                    }
                    if (Mathf.Abs(localPosition.x) <= Mathf.Abs(localPosition.z))
                    {
                       // Debug.LogError("Directional Indicator Down");
                        StartCoroutine(DirectionalIndicatorFlash(2));
                    }

                }
                else if (localPosition.x >= 0 && localPosition.z <= 0)
                {
                    if (Mathf.Abs(localPosition.z) <= localPosition.x)
                    {
                        //Debug.LogError("Directional Indicator Right");
                        StartCoroutine(DirectionalIndicatorFlash(1));
                    }
                    if (localPosition.x <= Mathf.Abs(localPosition.z))
                    {
                        //Debug.LogError("Directional Indicator Down");
                        StartCoroutine(DirectionalIndicatorFlash(2));
                    }

                }
                
            }
        }

    }
    #endregion

    #region Zoom Coroutines
    private IEnumerator ZoomIn()
    {
        //Smooths the zoom in so that it's not an instant jump in and out.
        float maxZoom = 20f;
        float currentZoom = myVirtualCam.m_Lens.FieldOfView;
        float zoomRate = .3f / 50f;
        float zoomDistance = currentZoom - maxZoom;
        float timeSinceHit = 0.0f;
        float timeToZoom = zoomRate * zoomDistance;
        float evaluation = 0.0f;

        while (currentZoom > maxZoom)
        {

            timeSinceHit += Time.deltaTime;
            evaluation = timeSinceHit / timeToZoom;
            float zoomModifire = vCamLerpCurve.Evaluate(evaluation) * zoomDistance;
            currentZoom -= zoomModifire;
            zoomDistance = currentZoom - maxZoom;

            myVirtualCam.m_Lens.FieldOfView = currentZoom;

            yield return new WaitForFixedUpdate();
        }
        myVirtualCam.m_Lens.FieldOfView = 20;

    }

    private IEnumerator ZoomOut()
    {
        //Smooths the zoom out so that it's not an instant jump in and out.
        float maxZoom = 70f;
        float currentZoom = myVirtualCam.m_Lens.FieldOfView;
        float zoomRate = .5f / 50f;
        float zoomDistance = maxZoom - currentZoom;
        float timeSinceHit = 0.0f;
        float timeToZoom = zoomRate * zoomDistance;
        float evaluation = 0.0f;

        while (currentZoom < maxZoom)
        {

            timeSinceHit += Time.deltaTime;
            evaluation = timeSinceHit / timeToZoom;
            float zoomModifire = vCamLerpCurve.Evaluate(evaluation) * zoomDistance;
            zoomDistance = maxZoom - currentZoom;
            currentZoom += zoomModifire;

            myVirtualCam.m_Lens.FieldOfView = currentZoom;

            yield return new WaitForFixedUpdate();
        }

        myVirtualCam.m_Lens.FieldOfView = 70f;

    }

    #endregion

    #region death and rebirth
    void OnTriggerExit(Collider exiting)
    {
        //Checks for game end, resets fuel.
        if (isGameOver)
            return;

        if (exiting.gameObject.tag == "Killzone")
        {
            fuel = MAXFUEL;
            airTime = 0;
        }
            

        if (!isServer)
            return;
        //Server removes a life
        if (exiting.gameObject.tag == "Killzone")
        {
            CmdDeductLifeFromPlayer();


        }

    }//end OnCollisionExit

    private IEnumerator CountdownTimeLogic()
    {
        //Countdown until respawn
        float CountTilTime = Time.time + COUNTDOWNSTARTVALUE;

        CountdownText.text = COUNTDOWNSTARTVALUE.ToString();

        while (CountTilTime > Time.time)
        {
            CountdownText.text = (CountTilTime - Time.time).ToString("0.00");
            // yield specifically needs to be here, otherwise Unity dislikes the loop and takes too long to process
            yield return null;
        }
        // end while

        CountdownText.text = "Go!";

        yield return new WaitForSecondsRealtime(1f);
        //Spawns after countdown over.
        SpawnPlayer();

        //Unpauses control
        isPaused = false;
    }

    public void SpawnPlayer()
    {
        //Respawns a player on a still existing random hex.
        if (isLocalPlayer)
        {
            GameObject hex = GetRandomHex();
            CmdSpawn(hex);
        }// end if
    }

    public GameObject GetRandomHex()
    {
        //Finds a hex that hasn't been destroyed
        hexRandom = Random.Range(0, ActiveHexGrid.Length);
        currentPoint = ActiveHexGrid[hexRandom];
        return currentPoint;
    }

    [Command]
    void CmdSpawn(GameObject hex)
    {
        //Grabs the hex spawn point from the hex.
        Transform spawn = hex.transform.Find("HexSpawnPoint");
        gameObject.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        RpcSpawn(hex);
    }

    [ClientRpc]
    void RpcSpawn(GameObject hex)
    {
        //client spawning.
        NetworkMovement movementCode = gameObject.GetComponent<NetworkMovement>();
        Transform spawn = hex.transform.Find("HexSpawnPoint");
        gameObject.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        movementCode.bSpawning = true;
        movementCode.spawnPosition = spawn.position;
        movementCode.spawnRotation = spawn.rotation;
        if (isLocalPlayer)
        {
            CmdTurnOnCharacterBits();
        }

    }

    [Command]
    public void CmdDeductLifeFromPlayer()
    {
        livesLeft--;
        Debug.LogError("Player with netID " + netId + " lost life - Lives after deduct = " + livesLeft);

        if (livesLeft == 0)
        {
            //Later on, Game Over screen should be camera view following other random player.
            Debug.LogError("You lose. Into the abyss with you!");
            noLivesLeft = true;
            Debug.LogError("Player with netID " + netId + " has no more lives");

            //Keeps you from doing things
            CmdTurnOffCharacterBits();
            //Checks for a winner
            CheckAlivePlayers();
            RpcOutOfGame();

        }
        else
        {
            //Keeps you from being active during countdown.
            CmdTurnOffCharacterBits();
            RpcOnDeath();
        }
    }

    private void CheckAlivePlayers()
    {
        //Checks for a winner. Performed after a character runs out of lives.
        var foundPlayerList = GameObject.FindGameObjectsWithTag("Player");
        List<GameObject> livePlayerList = new List<GameObject>();

        Debug.LogError("Player List Length: " + foundPlayerList.Length);
        int livePlayers = 0;
        foreach (GameObject Player in foundPlayerList)
        {
            if (!(Player.GetComponent<PlayerManager>().noLivesLeft))
            {
                livePlayers++;
                livePlayerList.Add(Player);
            }
        }
        //end Foreach
        Debug.LogError("Live Players Count: " + livePlayers);
        if (livePlayers <= 1)
        {
            //Debug.Log(livePlayers + " player remaining - Player with netID " + foundPlayerList[0].GetComponent<PlayerManager>().netId + " wins");
            //Debug.Log(GameObject.Find("LobbyManager").ToString());
            if (livePlayerList.Count > 0)
            {
                RpcCallFadeOut(livePlayerList[0].GetComponent<PlayerManager>().myChoices.slotNumber);
            }
            // end if
            else
            {
                RpcCallFadeOut(-1);
            }
            // end else
            //GameObject.Find("LobbyManager").GetComponent<LobbyManager>().ServerReturnToLobby();

        }
    }

    [ClientRpc]
    private void RpcCallFadeOut(int slotNum)
    {
        StartCoroutine(DisplayWinnerAndFade(slotNum));
    }

    IEnumerator DisplayWinnerAndFade(int slotNum)
    {
        //Displays a winner at the end of game.
        var playerList = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in playerList)
        {
            player.GetComponent<PlayerManager>().isGameOver = true;
            player.GetComponent<PlayerManager>().InitializeGameOverUI();
            player.GetComponent<PlayerManager>().WinnerText.text = "Player " + (slotNum + 1) + " Wins!";
        }
        // end foreach

        yield return new WaitForSeconds(2f);

        GameObject.Find("LevelChanger").GetComponent<LevelChanger>().NetworkFadeOut();
    }

    [ClientRpc]
    public void RpcOnDeath()
    {
        //Debug.Log("PlayerDied");
        //Debug.Log("Lives left: " + livesLeft);
        gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        gameObject.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);

        if (isLocalPlayer && !noLivesLeft)
        {
            DisplayCountdownUI();
            StartCoroutine(CountdownTimeLogic());
        }
        // end if

        //Debug.Log("Current Point from random in HexGrid:" + currentPoint);
        //gameObject.transform.rotation = Quaternion.identity;
        //gameObject.transform.position = spawnPoint.position;
    }

    [Command]
    void CmdTurnOffCharacterBits()
    {
        //Server Stops the character from animating, turns off the visuals.
        gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        gameObject.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
        HideAliveUI();

        if (noLivesLeft)
        {
            //Turns off the collider for physics
            gameObject.GetComponent<CapsuleCollider>().enabled = false;
        }
        //end if

        playerRig.SetActive(false);
        weapon1Prefab.SetActive(false);
        weapon2Prefab.SetActive(false);
        isPaused = true;

        RpcTurnOffCharacterBits();
    }

    [ClientRpc]
    void RpcTurnOffCharacterBits()
    {
        //Client side turning off the character bits.
        gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        gameObject.GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
        playerRig.SetActive(false);
        weapon1Prefab.SetActive(false);
        weapon2Prefab.SetActive(false);
        isPaused = true;
        HideAliveUI();

        if (noLivesLeft)
        {
            gameObject.GetComponent<CapsuleCollider>().enabled = false;
        }

    }

    [Command]
    void CmdTurnOnCharacterBits()
    {

        //Server Turns on a character once respawned.
        gameObject.GetComponent<CapsuleCollider>().enabled = true;
        playerRig.SetActive(true);
        weapon1Prefab.SetActive(true);
        if (!activeWeaponIs1)
        {
            weapon1Prefab.SetActive(false);
            weapon2Prefab.SetActive(true);
        }
        HideCountdownUI();
        DisplayAliveUI();
        RpcTurnOnCharacterBits();
    }
    [ClientRpc]
    void RpcTurnOnCharacterBits()
    {
        //Client turns on character.
        gameObject.GetComponent<CapsuleCollider>().enabled = true;
        playerRig.SetActive(true);
        weapon1Prefab.SetActive(true);
        if (!activeWeaponIs1)
        {
            weapon1Prefab.SetActive(false);
            weapon2Prefab.SetActive(true);
        }
        HideCountdownUI();
        DisplayAliveUI();

    }

    [ClientRpc]
    void RpcOutOfGame()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        //Client Removes other players.
        StartCoroutine(PlayerOutOfGame());
    }

    private IEnumerator PlayerOutOfGame()
    {

        //Displays that you are out of the game.
        if (isLocalPlayer)
        {
            DisplayOutOfGameUI();
            yield return new WaitForSeconds(2.0f);
            HideOutOfGameUI();
            camReleased = true;
            CmdTurnOffDeathCam();
            //Debug.LogError("OutOfGame Coroutine Completed.");
            
            //nextPlayerCam();
        }
    }

    #endregion

    #region AfterDeathCams

    [Command] void CmdTurnOffDeathCam()
    {
        //Server tells the client ot turn off its death camera
        RpcTurnOffDeathCam();
    }

    [ClientRpc] void RpcTurnOffDeathCam()
    {
        //Turns off client camera, and kicks all the people watching it.
        myDeathCam.isActive = false;
        if (isLocalPlayer)
            myDeathCam.KickAllWatchers();
    }

    [Command] public void CmdKickPlayerCamera()
    {
        //Tells the client to stop watching a camera.
        RpcKickPlayerCamera();
    }

    [ClientRpc] void RpcKickPlayerCamera()
    {
        
        if (isLocalPlayer)
        {
            //Turns off the local camera, and checks for someone else to watch.
            //Debug.LogError("I'm Local, and I've been Kicked!");
            myDeathCam.TurnOffControl();
            GetNewDeathCam();
        }
    }

    void GetNewDeathCam()
    {
        //Creates a list of live player cams, and assigns one that you can change through.
        MakeDeathCamList();
        myDeathCam = deathCamList[0];
        myCam.cullingMask = myDeathCam.cullingMask;
        myDeathCam.TurnOnControl();
        //Debug.LogError("I should have a new camera?!");
    }

    void NextPlayerCam(){
        //Allows players to switch between cameras after death.
		int index = 0;

		MakeDeathCamList();

		if (deathCamList.Contains(myDeathCam)) {
			index = deathCamList.IndexOf(myDeathCam);
            
		}
		if (index == deathCamList.Count -1 )
        {
            myDeathCam.TurnOffControl();
            myDeathCam = deathCamList[0];
            myCam.cullingMask = myDeathCam.cullingMask;
            myDeathCam.TurnOnControl();
        }
        else
        {
            myDeathCam.TurnOffControl();
            myDeathCam = deathCamList[index + 1];
            myCam.cullingMask = myDeathCam.cullingMask;
            myDeathCam.TurnOnControl();
        }
	}

	void PrevPlayerCam(){
        //allows players to switch between cameras after death
		int index = 0;

		MakeDeathCamList();

		if (deathCamList.Contains(myDeathCam)) {
            index = deathCamList.IndexOf(myDeathCam);
            myDeathCam.TurnOffControl();
        }
		if (index == 0 )
        {
            myDeathCam.TurnOffControl();
            myDeathCam = deathCamList[deathCamList.Count - 1];
            myCam.cullingMask = myDeathCam.cullingMask;
            myDeathCam.TurnOnControl();
        }
        else
        {
            myDeathCam.TurnOffControl();
            myDeathCam = deathCamList[index = 1];
            myCam.cullingMask = myDeathCam.cullingMask;
            myDeathCam.TurnOnControl();
        }


    }

	void MakeDeathCamList() {
        //Creates a list of death cams.
		deathCamList.Clear();
		GameObject[] playerList = GameObject.FindGameObjectsWithTag("Player");
		foreach (GameObject player in playerList) {
			if (!player.GetComponent<PlayerManager>().noLivesLeft) {
				deathCamList.Add(player.GetComponent<PlayerManager>().myDeathCam);
			}
		}

	}
    #endregion

    #endregion

    #region Animation, IK, Visuals
    [Command]
    private void CmdSetAnimation(GameObject player, string animName, bool shouldAnimate, float animSpeed)
    {
        RpcSetAnimation(player, animName, shouldAnimate, animSpeed);
    }

    [ClientRpc]
    private void RpcSetAnimation(GameObject player, string animName, bool animState, float animSpeed)
    {
        player.GetComponent<PlayerManager>().anim.SetBool(animName, animState);
        player.GetComponent<PlayerManager>().anim.speed = animSpeed;
    }
    [Command] private void CmdSetWalkAnim(float hInput, float vInput)
    {
        RpcSetWalkAnim(hInput, vInput);
    }
    [ClientRpc] private void RpcSetWalkAnim(float hInput, float vInput)
    {
        anim.SetFloat("Horizontal", hInput);
        anim.SetFloat("Vertical", vInput);
        if (vInput != 0)
        {
            anim.SetFloat("Speed", vInput);
        }
        else if (hInput != 0)
        {
            anim.SetFloat("Speed", hInput);
        }
    }

    [Command]
    public void CmdDoIK(Vector3 target)
    {
        RpcDoIK(target);
    }

    [ClientRpc]
    public void RpcDoIK(Vector3 target)
    {
        playerRig.GetComponent<PlayerIK>().updatedTarget = target;
        if (activeWeaponIs1)
        {
            weapon1Prefab.GetComponent<AimWeapon>().shootTo = target;
        }
        else
        {
            weapon2Prefab.GetComponent<AimWeapon>().shootTo = target;
        }
    }

    IEnumerator DirectionalIndicatorFlash(int quadrant)
    {
        float timeSinceHit = 0.0f;
        float timeToFade = 1.0f;
        float evaluation = 0.0f;
        float alpha = 0.0f;
        Image quad = directionalIndicator[quadrant];

        while (timeSinceHit < timeToFade)
        {

            timeSinceHit += Time.deltaTime;
            evaluation = timeSinceHit / timeToFade;
            alpha = shieldLerpCurve.Evaluate(evaluation);

            Color tempColor = new Color(quad.color.r, quad.color.g, quad.color.b, alpha);
            quad.color = tempColor;

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator ShieldAlphaSlide()
    {
        float timeSinceHit = 0.0f;
        float timeToFade = 1.0f;
        float evaluation = 0.0f;
        float alpha = 0.0f;

        while (timeSinceHit < timeToFade)
        {

            timeSinceHit += Time.deltaTime;
            evaluation = timeSinceHit / timeToFade;
            alpha = shieldLerpCurve.Evaluate(evaluation);

            foreach (SkinnedMeshRenderer shield in shieldParts)
            {
                Material[] matRef = shield.materials;
                foreach (Material mat in matRef)
                {
                    mat.SetFloat("Vector1_71EAD38A", alpha);
                }
                
            }

            yield return new WaitForFixedUpdate();
        }

    }
    #endregion

	#region UI init and switching
    public void InitializeGameStartUI() {
        HideAliveUI();
        HideOutOfGameUI();
        HideGameOverUI();

        DisplayCountdownUI();
    }

    public void InitializeAliveUI() {
        HideCountdownUI();
        HideOutOfGameUI();
        HideGameOverUI();

        DisplayAliveUI();
    }

    public void InitializeGameOverUI() {
        HideCountdownUI();
        HideAliveUI();
        HideOutOfGameUI();

        DisplayGameOverUI();
    }

    public void DisplayAliveUI() {
        AliveContainer.SetActive(true);
    }

    public void HideAliveUI() {
        AliveContainer.SetActive(false);
    }

    public void DisplayCountdownUI() {
        CountdownContainer.SetActive(true);
    }

    public void HideCountdownUI() {
        CountdownContainer.SetActive(false);
    }

    public void DisplayOutOfGameUI() {
        OutOfGameContainer.SetActive(true);
    }

    public void HideOutOfGameUI() {
        OutOfGameContainer.SetActive(false);
    }

    public void DisplayGameOverUI() {
        GameOverContainer.SetActive(true);
    }

    public void HideGameOverUI() {
        GameOverContainer.SetActive(false);
    }

    #endregion

    
}

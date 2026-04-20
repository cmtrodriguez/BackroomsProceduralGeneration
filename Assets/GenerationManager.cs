using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;

public enum GenerationState
{
    Idle,
    GeneratingRooms,
    GeneratingLighting,

    GeneratingSpawn,
    GeneratingExit,

    GeneratingBarrier
}

public class GenerationManager: MonoBehaviour
{
    [Header("References")]
    [SerializeField] NavMeshSurface navMeshSurface; // The NavMeshSurface component in our scene, used to build the navmesh after generation.

    [SerializeField] Transform WorldGrid; // The Parent of our World

    [SerializeField] List<GameObject> RoomTypes; // The prefab that spawns in our world grid
    
    [SerializeField] List<GameObject> LightTypes; // The prefabs that spawn in our rooms to give light
    
    [SerializeField] int mapSize = 16; // The size of the map
    
    [SerializeField] Slider MapSizeSlider, EmptinessSlider, BrightnessSlider;
    
    [SerializeField] Button GenerateButton;
    
    [SerializeField] GameObject E_Room; // The Empty Room type
    
    [SerializeField] GameObject B_Room; // The Barrier Room type
    
    [SerializeField] GameObject SpawnRoom, ExitRoom;
    
    public List<GameObject> GeneratedRooms;

    [SerializeField] GameObject PlayerObject, MainCameraObject;

    [SerializeField] private GameObject EnemyPrefab;
    
    private GameObject spawnedEnemy;

    [Header("Settings")]
    public int mapEmptiness; // The chance of an E_Room spawning in.
    
    public int mapBrightness; // The chance of a light type spawning in.

    private int mapSizeSqr; // The square root of the map size
    
    private float currentPosX, currentPosZ, currentPosTracker, currentRoom;

    public float roomSize = 7;
    
    private Vector3 currentPos; // The current position of the room that the script will generate public GenerationState currentState; // The current state of generation of our script
    
    public GenerationState currentState;

    private void Update()
    {
        mapSize = (int)Mathf.Pow(MapSizeSlider.value, 4);

        mapSizeSqr = (int)Mathf.Sqrt(mapSize);

        mapEmptiness = (int)EmptinessSlider.value;

        mapBrightness = (int)BrightnessSlider.value;
    }

    public void ReloadWorld()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the currently loaded scene.
    }

    public void GenerateWorld()
    {
        Debug.Log("WorldGrid = " + WorldGrid);
        Debug.Log("navMeshSurface = " + navMeshSurface);

        for (int i = 0; i < mapEmptiness; i++)
        {
            RoomTypes.Add(E_Room); // Adds empty rooms to the Room Types array.
        }

        GenerateButton.interactable = false;

        for (int state = 0; state < 6; state++)
        {
            for (int i = 0; i < mapSize; i++)
            {
                if (currentPosTracker == mapSizeSqr) // Move the position back to the beginning of the grid, so it can go upwards
                {
                    if (currentState == GenerationState.GeneratingBarrier) GenerateBarrier(); // Right
                    
                    currentPosX = 0;
                    currentPosTracker = 0;
                    
                    currentPosZ += roomSize;
                   
                   if (currentState == GenerationState.GeneratingBarrier) GenerateBarrier(); // Left
                }

                currentPos = new(currentPosX, 0, currentPosZ); // Pass in our positions

                switch (currentState)
                {
                    case GenerationState.GeneratingRooms:
                        GeneratedRooms.Add(Instantiate(RoomTypes[Random.Range(0, RoomTypes.Count)], currentPos, Quaternion.identity, WorldGrid)); // Instantiates the room type at the currentPos.
                        break;

                    case GenerationState.GeneratingLighting:
                        int lightSpawn = Random.Range(-1, mapBrightness);
                        
                        if (lightSpawn == 0)
                            Instantiate(LightTypes[Random.Range(0, LightTypes.Count)], currentPos, Quaternion.identity, WorldGrid); // Instantiates the room type at the currentPos.
                        break;

                    case GenerationState.GeneratingBarrier:

                        if (currentRoom <= mapSizeSqr && currentRoom >= 0)
                        {
                            GenerateBarrier(); // Bottom
                        }

                        if (currentRoom <= mapSize && currentRoom >= mapSize - mapSizeSqr)
                        {
                            GenerateBarrier(); // Top
                        }
                        break;
                }

                currentRoom++;
                currentPosTracker++; // Keeps track of the position X, without using the room size.
                currentPosX += roomSize; // Adds more position to the currentPosX, which makes it go to the right a bit more.
            }

            NextState();

            switch (currentState)
            {
                case GenerationState.GeneratingExit:
                    int roomToReplace = Random.Range(0, GeneratedRooms.Count);
                    GameObject exitRoom = Instantiate(ExitRoom, GeneratedRooms[roomToReplace].transform.position, Quaternion.identity, WorldGrid);
                    Destroy(GeneratedRooms[roomToReplace]);
                    GeneratedRooms[roomToReplace] = exitRoom;
                    break;

                case GenerationState.GeneratingSpawn:
                    int spawnRoomToReplace = Random.Range(0, GeneratedRooms.Count);
                    spawnRoom = Instantiate(SpawnRoom, GeneratedRooms[spawnRoomToReplace].transform.position, Quaternion.identity, WorldGrid);
                    Destroy(GeneratedRooms[spawnRoomToReplace]);
                    GeneratedRooms[spawnRoomToReplace] = spawnRoom;

                    PlayerObject.transform.position = spawnRoom.transform.position + new Vector3(0, 1, 0); // Move the player to the spawn room.
                    MainCameraObject.transform.position = spawnRoom.transform.position + new Vector3(0, 1.5f, -2); // Move the camera to the spawn room.
                    break;
            }
        }

        navMeshSurface.BuildNavMesh();
    }

    public GameObject spawnRoom;

 public void SpawnPlayer()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // wait 1 frame so generation is fully done
        yield return null;

        // =========================
        // PLAYER SPAWN
        // =========================
        PlayerObject.SetActive(false);

        Vector3 playerSpawnPos = spawnRoom.transform.position + new Vector3(0, 1.8f, 0);
        PlayerObject.transform.position = playerSpawnPos;
        PlayerObject.SetActive(true);

        // =========================
        // CAMERA
        // =========================
        MainCameraObject.SetActive(false);

        // =========================
        // BUILD NAVMESH AFTER GENERATION
        // =========================
        navMeshSurface.BuildNavMesh();

        yield return null; // let NavMesh register

        // =========================
        // ENEMY SPAWN (DIFFERENT ROOM)
        // =========================
        if (EnemyPrefab != null && GeneratedRooms.Count > 0)
        {
            GameObject chosenRoom;

            do
            {
                chosenRoom = GeneratedRooms[Random.Range(0, GeneratedRooms.Count)];
            }
            while (chosenRoom == spawnRoom);

            Vector3 rawPos = chosenRoom.transform.position + new Vector3(0, 1f, 0);

            Vector3 finalPos = rawPos;

            if (NavMesh.SamplePosition(rawPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                finalPos = hit.position;
            }

            GameObject enemy = Instantiate(EnemyPrefab, finalPos, Quaternion.identity);

            EnemyController enemyScript = enemy.GetComponent<EnemyController>();
            if (enemyScript != null)
            {
                enemyScript.SetPlayer(PlayerObject.transform);
            }
        }
    }

    public void NextState()
    {
        currentState++;

        currentRoom = 0;
        currentPosX = 0;
        currentPosZ = 0;
        currentPosTracker = 0;
        currentPos = Vector3.zero;
    }

    public void WinGame()
    {
        MainCameraObject.SetActive(true);
        PlayerObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("You win!");
    }

    public void GenerateBarrier()
    {
        currentPos = new(currentPosX, 0, currentPosZ);
        Instantiate(B_Room, currentPos, Quaternion.identity, WorldGrid); // Instantiates the room type at the currentPos.
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerationManager : MonoBehaviour
{
    [Header("Generators")]
    [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;
    [SerializeField]
    private GenerateObjectsInArea boatGenerator;


    [Space(10)]
    [Header("Parenting and Mutation")]
    [SerializeField]
    private float mutationFactor;
    [SerializeField] 
    private float mutationChance;
    [SerializeField] 
    private int boatParentSize;


    [Space(10)] 
    [Header("Simulation Controls")]
    [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;
    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;
    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;
    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    [Space(10)] 
    [Header("Saving and storage")]
    [SerializeField]
    private bool saveCompleteGenerations;
    [SerializeField]
    private string savePrefabsAt;
    [SerializeField, Tooltip("txt file storing the scores")]
    private string saveScoreDataAt;
    [SerializeField, Tooltip("Add stored prefabs here to use them.")]
    private GameObject[] storedboats;


    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")]
    [SerializeField]
    private AgentData lastBoatWinnerData;


    private bool _runningSimulation;
    private List<BoatLogic> _activeBoats;
    private BoatLogic[] _boatParents;
    private StreamWriter scoreWriter;


    private void Start()
    {
        //if savePrefabsAt ends in '/' we need to remove it so AssetDatabase.CreateFolder works
        if (savePrefabsAt[savePrefabsAt.Length - 1] == '/')
            savePrefabsAt = savePrefabsAt.Substring(0, savePrefabsAt.Length - 1);

        scoreWriter = new StreamWriter(saveScoreDataAt, true);
        scoreWriter.AutoFlush = true;

        if (runOnStart)
        {
            StartSimulation();
        }
    }
    
    private void Update()
    {
        if (_runningSimulation)
        {
            //Creates a new generation.
            if (simulationCount >= simulationTimer)
            {
                MakeNewGeneration();
                ++generationCount;
                simulationCount = -Time.deltaTime;
            } 
            simulationCount += Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        scoreWriter.Close();

    }

    /// <summary>
    /// Generates the boxes on all box areas.
    /// </summary>
    public void GenerateBoxes()
    {
        foreach (GenerateObjectsInArea generateObjectsInArea in boxGenerators)
        {
            generateObjectsInArea.RegenerateObjects();
        }
    }
    

     /// <summary>
     /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
     /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
     /// /// Newly create agents will be Awaken (calling AwakeUp()).
     /// </summary>
     /// <param name="boatParents"></param>
    public void GenerateBoats(BoatLogic[] boatParents = null)
    {
        _activeBoats = new List<BoatLogic>();
        List<GameObject> objects = boatGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            BoatLogic boat = obj.GetComponent<BoatLogic>();
            if (boat != null)
            {
                _activeBoats.Add(boat);
                if (boatParents != null)
                {
                    BoatLogic boatParent = boatParents[Random.Range(0, boatParents.Length)];
                    boat.Birth(boatParent.GetData());
                }

                boat.Mutate(mutationFactor, mutationChance);
                boat.AwakeUp();
            }
        }
    }

     /// <summary>
     /// Creates a new generation by using GenerateBoxes and GenerateBoats.
     /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
     /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
     /// will use the [generationCount] as an identifier.
     /// </summary>
    public void MakeNewGeneration()
    {
        GenerateBoxes();
        
        //Fetch parents
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.Sort();

        if (_activeBoats.Count == 0)
        {
            GenerateBoats(_boatParents);
        }
        _boatParents = new BoatLogic[boatParentSize];
        for (int i = 0; i < boatParentSize; i++)
        {
            _boatParents[i] = _activeBoats[i];
        }

        //Winner:
        BoatLogic lastBoatWinner = _activeBoats[0];
        Debug.Log("Last winner boat had: " + lastBoatWinner.GetPoints() + " points!");
        scoreWriter.WriteLine(generationCount + " " + lastBoatWinner.GetPoints());


        //we save the generation sorted by how well each boat performed or we save just the best boat
        if (saveCompleteGenerations)
        {
            string guid = AssetDatabase.CreateFolder(savePrefabsAt, "Complete Gen-" + generationCount);
            string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
            for (int i = 0; i < _activeBoats.Count; i++)
            {
                _activeBoats[i].name = "(" + (i + 1) + ")" + _activeBoats[i].name + "Gen-" + generationCount;
                PrefabUtility.SaveAsPrefabAsset(_activeBoats[i].gameObject, newFolderPath + "/" + _activeBoats[i].name + ".prefab");
            }
        }
        else
        {
            lastBoatWinner.name += "Gen-" + generationCount;
            lastBoatWinnerData = lastBoatWinner.GetData();
            PrefabUtility.SaveAsPrefabAsset(lastBoatWinner.gameObject, savePrefabsAt +"/" + lastBoatWinner.name + ".prefab");
        }
   
        GenerateBoats(_boatParents);
    }

     /// <summary>
     /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBoxes and GenerateObjects and
     /// then sets the _runningSimulation flag to true.
     /// </summary>
    public void StartSimulation()
    {
        clearScoresFile();
        simulationCount = 0;
        generationCount = 0;
        _boatParents = null;
        GenerateBoxes();
        GenerateBoats(_boatParents);
        _runningSimulation = true;
    }

     /// <summary>
     /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
     /// It sets the _runningSimulation flag to true.
     /// </summary>
     public void ContinueSimulation()
     {
        if (!_runningSimulation && _activeBoats!=null && _activeBoats.Count>0)
        {
            simulationCount = 0;
            MakeNewGeneration();
            _runningSimulation = true;
        }
     }

    /// <summary>
    /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
    /// all boats to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        if (_activeBoats != null)
        {
            _activeBoats.RemoveAll(item => item == null);
            _activeBoats.ForEach(boat => boat.Sleep());
        }
    }

    /// <summary>
    /// Overrides current generation with stored boats and starts the simulation from there.
    /// Requires the array of stored boat to be the same size as the one we were using and the simulation to be stopped before
    /// </summary>
    public void StartSimulationStored()
    {
        if (storedboats.Length>boatParentSize)
        {
            GenerateBoxes();
            simulationCount = 0;

            _boatParents = null;

            _activeBoats = new List<BoatLogic>();

            List<GameObject> instatiatedBoats = boatGenerator.RegenerateObjects(storedboats);

            foreach (GameObject boatObject in instatiatedBoats)
            {
                _activeBoats.Add(boatObject.GetComponent<BoatLogic>());
                boatObject.GetComponent<BoatLogic>().AwakeUp();
            }

            _runningSimulation = true;
        }
    }

    public void clearScoresFile()
    {
        print("hi");
        try
        {
            scoreWriter.Close();
            StreamWriter file = new StreamWriter(saveScoreDataAt);
            file.Write("");
            file.Close();

            scoreWriter = new StreamWriter(saveScoreDataAt, true);
            scoreWriter.AutoFlush = true;
            scoreWriter.WriteLine("Scores");
            Debug.Log("Scores file cleared");
        }
        catch
        {
            Debug.LogWarning("Error when clearing the scores file, be aware it may still contain other data");
        }
    }
}

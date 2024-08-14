using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlueRaja;

public class DungeonGenerator : MonoBehaviour
{
    //Classes / Enums
    #region Classes / Enums
    public class Edge
    {
        public Vector3Int V1;
        public Vector3Int V2;
        public Vector3Int V2IdealOffset;
        public List<Vector3Int> PathPoints;
        public List<Edge> SubEdges;

        public Edge(Vector3Int Vert1, Vector3Int Vert2, Vector3Int Vert2IdealOffset)
        {
            V1 = Vert1;
            V2 = Vert2;
            V2IdealOffset = Vert2IdealOffset;
        }
    }

    public class Door
    {
        public Vector3Int Pos;
        public Vector3Int Dir;
        public int OwnerID;

        public Door(Vector3Int Position, Vector3Int Direction, int ID)
        {
            Pos = Position;
            Dir = Direction;
            OwnerID = ID;
        }
    }

    public class AStarNode
    {
        public Vector3Int Pos;
        public AStarNode PreviousNode;

        public AStarNode(Vector3Int Position, AStarNode PrevNode)
        {
            Pos = Position;
            PreviousNode = PrevNode;
        }
    }

    public struct PathCost
    {
        public bool traversable;
        public float cost;
        public bool isStairs;
    }

    enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs
    }
    #endregion

    //Variables
    #region Seed
    public int Seed;
    public bool RandomizeSeed;
    #endregion

    #region Grid
    [Header("Grid")]
    public Vector3Int GridSize;
    public int GridScale = 10;
    #endregion

    #region Rooms
    [Header("Rooms")]
    public DungeonSet Dset;
    public float SeperationDistance;
    public float GridPadding;
    public int MaxRooms;
    public int RoomPlacementSamples;

    List<DungeonRoom> AllRooms;
    #endregion

    #region MST
    List<Door> MSTDoors;
    List<Edge> MSTEdges;
    #endregion

    #region Astar
    static readonly Vector3Int[] Neighbours = {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
    };

    Grid3D<CellType> CellTypeGrid;
    Grid3D<Vector3> StairOffsetsGrid;
    SimplePriorityQueue<AStarNode> Queue;
    List<Vector3Int> UsedPoints;
    #endregion

    #region Player
    public Transform PlayerReference;
    Vector3 PlayerSpawn;
    #endregion

    #region Debug
    [Header("Debug")]
    public bool DrawGridBounds = true;
    public bool DrawDoors = true;
    public bool DrawMST = true;
    public bool DrawSubEdges = true;
    public bool DrawHallways = true;

    List<GameObject> PrefabReferences; //For destroying all instantiated prefabs when pressing R
    #endregion

    //Used to manually refresh
    private void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            foreach (GameObject GO in PrefabReferences)
                Destroy(GO);

            Start();
        }
    }

    //Runs all the functions in order to allow for the generation of the dungeon
    private void Start()
    {
        PrefabReferences = new List<GameObject>();

        InitializeSeed();
        CreateGrid();
        PlaceRooms();
        RemoveExcessComponenets();
        CreateMST();
        PathfindHallways();
        InstantiateHallways();
        FillUnusedDoors();
        SpawnPlayer();
    }

    void InitializeSeed()
    {
        if (RandomizeSeed)
            Seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(Seed);
    }

    void CreateGrid()
    {
        CellTypeGrid = new Grid3D<CellType>(GridSize, Vector3Int.zero);
        StairOffsetsGrid = new Grid3D<Vector3>(GridSize, Vector3Int.zero);
    }

    void PlaceRooms()
    {
        //Intialize lists
        AllRooms = new List<DungeonRoom>();
        MSTDoors = new List<Door>();
        List<Vector3Int> OccupiedPoints = new List<Vector3Int>(); //Temporary list to do a distance check (better than looping through entire grid performance wise)

        for (int CurrentRoom = 0; CurrentRoom < MaxRooms; CurrentRoom++)
        {
            for (int CurrentSample = 0; CurrentSample < RoomPlacementSamples; CurrentSample++)
            {
                //Spawnroom
                GameObject RandomRoom = null;
                if (CurrentRoom == 0)
                    RandomRoom = Dset.SpawnRoom;
                else if (CurrentRoom == 1)
                    RandomRoom = Dset.BossRoom;

                //Get random values
                else
                    RandomRoom = Dset.Rooms[Random.Range(0, Dset.Rooms.Length)];
                Vector3Int RandomPos = new Vector3Int(Random.Range(0, GridSize.x), Random.Range(0, GridSize.y), Random.Range(0, GridSize.z));
                int RandomRot = Random.Range(0, 4);

                //Get required positions
                List<Vector3Int> RequiredPositions = new List<Vector3Int>();
                foreach (Transform RoomPoint in RandomRoom.GetComponent<DungeonRoom>().Points)
                    RequiredPositions.Add(RandomPos + RotatePointAroundPivot(Vector3Int.RoundToInt(RoomPoint.localPosition / GridScale), RandomRot * 90));

                //Loop through required positions to make sure they're valid
                bool Valid = true;
                foreach (Vector3Int RequiredPos in RequiredPositions)
                {
                    //Make sure spot isnt out of bounds
                    if (RequiredPos.x >= GridSize.x || RequiredPos.x < 0 
                        || RequiredPos.y >= GridSize.y || RequiredPos.y < 0
                        || RequiredPos.z >= GridSize.z || RequiredPos.z < 0)
                    {
                        Valid = false;
                        break;
                    }

                    //Make sure spot isnt occupied
                    if (CellTypeGrid[RequiredPos] != CellType.None)
                    {
                        Valid = false;
                        break;
                    }

                    //Grid padding
                    if (RequiredPos.x > GridSize.x - GridPadding || RequiredPos.x < GridPadding ||
                        RequiredPos.y > GridSize.y - GridPadding || RequiredPos.y < GridPadding ||
                        RequiredPos.z > GridSize.z - GridPadding || RequiredPos.z < GridPadding)
                    {
                        Valid = false;
                        break;
                    }

                    //Make sure spot isnt too close to any other rooms
                    foreach (Vector3Int OccupiedPoint in OccupiedPoints)
                        if (Vector3.Distance(RequiredPos, OccupiedPoint) < SeperationDistance)
                        {
                            Valid = false;
                            break;
                        }

                    if (Valid == false)
                        break;
                }

                if (Valid == false)
                    continue;

                else
                {
                    //Instantiate room and adjust collections
                    GameObject CreatedRoom = Instantiate(RandomRoom, RandomPos * GridScale, Quaternion.Euler(new Vector3(0, 90 * RandomRot, 0)), transform);
                    AllRooms.Add(CreatedRoom.GetComponent<DungeonRoom>());
                    PrefabReferences.Add(CreatedRoom); //For regenerating dungeon with R

                    //If spawn
                    if (CurrentRoom == 0)
                    {
                        PlayerSpawn = CreatedRoom.GetComponent<DungeonRoom>().SpawnPoint.position;
                    }

                    foreach (Vector3Int RequiredPos in RequiredPositions)
                    {
                        CellTypeGrid[RequiredPos] = CellType.Room;
                        OccupiedPoints.Add(RequiredPos);
                    }

                    //Add valid doors to MST points
                    foreach (Transform D in CreatedRoom.GetComponent<DungeonRoom>().Doors)
                    {
                        if (CellTypeGrid.InBounds(Vector3Int.RoundToInt(D.position / GridScale)))
                            MSTDoors.Add(new Door(Vector3Int.RoundToInt(D.position / GridScale),  Vector3Int.RoundToInt(D.forward), CurrentRoom));
                    }

                    break;
                }
            }
        }

        //Local functions
        Vector3Int RotatePointAroundPivot(Vector3Int Point, float YAngle)
        {
            if (YAngle == 90)
                return new Vector3Int(Point.z, Point.y, -Point.x);
            else if (YAngle == 180)
                return new Vector3Int(-Point.x, Point.y, -Point.z);
            else if (YAngle == 270)
                return new Vector3Int(-Point.z, Point.y, Point.x);

            return Point;
        }
    }

    void RemoveExcessComponenets()
    {
        foreach(DungeonRoom RoomScript in AllRooms)
            Destroy(RoomScript);
    }

    void CreateMST()
    {
        //Initialize lists
        MSTEdges = new List<Edge>();
        List<Door> UsedPoints = new List<Door>();
        List<Door> PendingPoints;

        //Set MST start point
        Door SelectedPoint = MSTDoors[0];
        UsedPoints.Add(SelectedPoint);
        MSTDoors.RemoveAt(0);

        //Populate pending points
        PendingPoints = new List<Door>(MSTDoors);

        //Re-add start point so it can be used by 'InstantiateHallways();'
        MSTDoors.Add(SelectedPoint);

        while (PendingPoints.Count > 0)
        {
            //Find closest point
            float ClosestDist = Mathf.Infinity;
            Door ClosestPPoint = null;

            foreach (Door PPoint in PendingPoints)
            {
                float Dist = Vector3.Distance(SelectedPoint.Pos, PPoint.Pos);

                if (Dist < ClosestDist)
                {
                    ClosestDist = Dist;
                    ClosestPPoint = PPoint;
                }
            }

            //Check that no other point in used points is closer
            Door ClosestUPoint = SelectedPoint;

            foreach (Door UPoint in UsedPoints)
            {
                float Dist = Vector3.Distance(UPoint.Pos, ClosestPPoint.Pos);

                if (Dist < ClosestDist)
                {
                    ClosestDist = Dist;
                    ClosestUPoint = UPoint;
                }
            }

            //Create edge if both points (Doors) are from different rooms
            if (ClosestUPoint.OwnerID != ClosestPPoint.OwnerID)
                MSTEdges.Add(new Edge(ClosestUPoint.Pos, ClosestPPoint.Pos, Vector3Int.zero));

            //Set selected point
            SelectedPoint = ClosestPPoint;

            //Adjust lists
            UsedPoints.Add(ClosestPPoint);
            PendingPoints.Remove(ClosestPPoint);
        }
    }

    void PathfindHallways()
    {
        //Find the path for each edge (hallway)
        foreach (Edge E in MSTEdges)
        {
            E.PathPoints = FindPath(E.V1, E.V2, out List<Edge> SEdges);
            E.SubEdges = SEdges;
        }

        //Finds the path between the specified start and end
        List<Vector3Int> FindPath(Vector3Int Start, Vector3Int End, out List<Edge> SubEdges)
        {
            List<Vector3Int> FinalPath = new List<Vector3Int>();
            SubEdges = new List<Edge>();

            #region CalculateStairs
            int YDelta = Mathf.Clamp(End.y - Start.y, -1, 1);

            if (YDelta != 0)
            {
                //Get ideal offset
                Vector3Int IdealOffset = Vector3Int.zero;
                Vector3Int PreviousIdealOffset = Vector3Int.zero;

                PreviousIdealOffset = GetIdealOffset(Start);

                //Initialize vars
                int SelectedY = Start.y;

                Vector3Int PreviousBestStairSpot = -Vector3Int.one;

                while (SelectedY != End.y)
                {
                    //Initialize var
                    Vector3Int CurrentBestStairSpot = Vector3Int.zero;
                    float CurrentLowestCost = Mathf.Infinity;

                    //Search entire current floor
                    for (int x = 0; x < GridSize.x; x++)
                    {
                        for (int z = 0; z < GridSize.z; z++)
                        {
                            if (CellTypeGrid.InBounds(new Vector3Int(x, SelectedY, z)) && CellTypeGrid.InBounds(new Vector3Int(x, SelectedY + YDelta, z))
                                && CellTypeGrid.InBounds(new Vector3Int(x + 1, SelectedY, z)) && CellTypeGrid.InBounds(new Vector3Int(x - 1, SelectedY, z))
                                && CellTypeGrid.InBounds(new Vector3Int(x, SelectedY, z + 1)) && CellTypeGrid.InBounds(new Vector3Int(x, SelectedY, z - 1))
                                && CellTypeGrid.InBounds(new Vector3Int(x + 1, SelectedY + YDelta, z)) && CellTypeGrid.InBounds(new Vector3Int(x - 1, SelectedY + YDelta, z))
                                && CellTypeGrid.InBounds(new Vector3Int(x, SelectedY + YDelta, z + 1)) && CellTypeGrid.InBounds(new Vector3Int(x, SelectedY + YDelta, z - 1)))
                            {
                                if (CellTypeGrid[x, SelectedY, z] == CellType.None && CellTypeGrid[x, SelectedY + YDelta, z] == CellType.None
                                && CellTypeGrid[x + 1, SelectedY, z] == CellType.None && CellTypeGrid[x - 1, SelectedY, z] == CellType.None
                                && CellTypeGrid[x, SelectedY, z + 1] == CellType.None && CellTypeGrid[x, SelectedY, z - 1] == CellType.None
                                && CellTypeGrid[x + 1, SelectedY + YDelta, z] == CellType.None && CellTypeGrid[x - 1, SelectedY + YDelta, z] == CellType.None
                                && CellTypeGrid[x, SelectedY + YDelta, z + 1] == CellType.None && CellTypeGrid[x, SelectedY + YDelta, z - 1] == CellType.None)
                                {
                                    float Cost = Vector3.Distance(Start, new Vector3(x, SelectedY + YDelta, z)) + Vector3.Distance(End, new Vector3(x, SelectedY + YDelta, z));
                                    if (Cost < CurrentLowestCost)
                                    {
                                        CurrentLowestCost = Cost;
                                        CurrentBestStairSpot = new Vector3Int(x, SelectedY, z);
                                    }
                                }
                            }
                        }
                    }

                    //Get ideal offset
                    IdealOffset = GetIdealOffset(CurrentBestStairSpot);

                    //Add to final list
                    FinalPath.Add(CurrentBestStairSpot);
                    FinalPath.Add(CurrentBestStairSpot + new Vector3Int(0, YDelta, 0));

                    CellTypeGrid[CurrentBestStairSpot] = CellType.Stairs;
                    CellTypeGrid[CurrentBestStairSpot + new Vector3Int(0, YDelta, 0)] = CellType.Stairs;

                    if (PreviousBestStairSpot == -Vector3Int.one)
                        PreviousBestStairSpot = Start - new Vector3Int(0, YDelta, 0) - PreviousIdealOffset;

                    Vector3Int V2IdealOffset = Vector3Int.zero;
                    if (YDelta == -1)
                        V2IdealOffset = -IdealOffset;
                    else if (YDelta == 1)
                        V2IdealOffset = IdealOffset;

                    SubEdges.Add(new Edge(PreviousBestStairSpot + new Vector3Int(0, YDelta, 0) + PreviousIdealOffset, CurrentBestStairSpot - IdealOffset, V2IdealOffset));

                    PreviousIdealOffset = IdealOffset;
                    PreviousBestStairSpot = CurrentBestStairSpot;

                    SelectedY += YDelta;
                }

                SubEdges.Add(new Edge(SubEdges[SubEdges.Count-1].V2 + new Vector3Int(0, YDelta, 0) + (IdealOffset * 2), End, Vector3Int.zero));
            }

            else
                SubEdges.Add(new Edge(Start, End, Vector3Int.zero));

            Vector3Int GetIdealOffset(Vector3Int Current)
            {
                float XDist = End.x - Current.x;
                float YDist = End.y - Current.y;
                float ZDist = End.z - Current.z;

                float XDistNorm = XDist;
                float YDistNorm = YDist;
                float ZDistNorm = ZDist;

                if (XDistNorm < 0)
                    XDistNorm = -XDistNorm;
                if (YDistNorm < 0)
                    YDistNorm = -YDistNorm;
                if (ZDistNorm < 0)
                    ZDistNorm = -ZDistNorm;

                if (XDistNorm >= YDistNorm && XDistNorm >= ZDistNorm)
                    return new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(XDist), -1, 1), 0, 0);
                else if (YDistNorm >= XDistNorm && YDistNorm >= ZDistNorm)
                    return new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(YDist), -1, 1), 0, 0);
                else if (ZDistNorm >= XDistNorm && ZDistNorm >= YDistNorm)
                    return new Vector3Int(Mathf.Clamp(Mathf.RoundToInt(ZDist), -1, 1), 0, 0);

                return Vector3Int.zero;
            }

            #endregion

            #region Pathfind
            foreach(Edge E in SubEdges)
            {
                Queue = new SimplePriorityQueue<AStarNode>();
                UsedPoints = new List<Vector3Int>();

                Queue.Enqueue(new AStarNode(E.V1, null), 0);

                int AntiCrash = 0;

                while (Queue.Count > 0)
                {
                    AntiCrash++;
                    if (AntiCrash > 10000)
                    {
                        Debug.LogError("Anti crash !");
                        break;
                    }

                    AStarNode CurrentNode = Queue.Dequeue();
                    UsedPoints.Add(CurrentNode.Pos);

                    if (CurrentNode.Pos == E.V2)
                    {
                        ConstructPath(CurrentNode);
                        break;
                    }

                    foreach(Vector3Int Offset in Neighbours)
                    {
                        Vector3Int CurrentNeighbour = CurrentNode.Pos + Offset;

                        //Bounds check
                        if (!CellTypeGrid.InBounds(CurrentNeighbour))
                            continue;

                        //Occupied check
                        if (CellTypeGrid[CurrentNeighbour] != CellType.None)
                            continue;

                        //Used check
                        if (UsedPoints.Contains(CurrentNeighbour))
                            continue;

                        //Queue check
                        bool Valid = true;
                        foreach (AStarNode N in Queue)
                        {
                            if (N.Pos == CurrentNeighbour)
                            {
                                Valid = false;
                                break;
                            }
                        }
                        if (!Valid)
                            continue;

                        //Enqueue
                        float ToCost = Mathf.Abs(CurrentNeighbour.x - E.V2.x) + Mathf.Abs(CurrentNeighbour.z - E.V2.z);
                        float FromCost = Mathf.Abs(CurrentNeighbour.x - E.V1.x) + Mathf.Abs(CurrentNeighbour.z - E.V1.z);
                        //float ToCost = Vector3.Distance(E.V2, CurrentNeighbour);
                        //float FromCost = Vector3.Distance(E.V1, CurrentNeighbour);
                        float Cost = ToCost + FromCost;
                        Queue.Enqueue(new AStarNode(CurrentNeighbour, CurrentNode), Cost);
                    }
                }

                void ConstructPath(AStarNode Goal)
                {
                    AddPrevNode(Goal);

                    void AddPrevNode(AStarNode CurNode)
                    {
                        FinalPath.Add(CurNode.Pos);
                        
                        if (CurNode.PreviousNode != null)
                            AddPrevNode(CurNode.PreviousNode);
                    }
                }
            }
            #endregion

            #region UpdateCellGrid
            foreach (Vector3Int PathPoint in FinalPath)
            {
                if (CellTypeGrid[PathPoint] == CellType.None)
                    CellTypeGrid[PathPoint] = CellType.Hallway;
            }
            #endregion

            return FinalPath;
        }
    }

    void InstantiateHallways()
    {
        //Loop through edges
        foreach (Edge E in MSTEdges)
        {
            //Hallway
            int SubEdgeIndex = 0;

            for (int i = 0; i < E.PathPoints.Count; i++)
            {
                Vector3Int PathPoint = E.PathPoints[i];

                #region Hallway
                if (CellTypeGrid[PathPoint] == CellType.Hallway)
                {
                    //Check for hall walls
                    bool Forward = false;
                    bool Right = false;
                    bool Backward = false;
                    bool Left = false;

                    //Check if door
                    foreach(Door D in MSTDoors)
                    {
                        if (PathPoint == D.Pos) //Is door
                        {
                            //Get door direction
                            Vector3Int DoorDir = D.Dir;

                            if (DoorDir == new Vector3Int(0, 0, -1))
                                Forward = true;
                            else if (DoorDir == new Vector3Int(-1, 0, 0))
                                Right = true;
                            else if (DoorDir == new Vector3Int(0, 0, 1))
                                Backward = true;
                            else if (DoorDir == new Vector3Int(1, 0, 0))
                                Left = true;

                            break;
                        }
                    }

                    //Check adj
                    foreach(Vector3Int OtherV3 in E.PathPoints)
                    {
                        if (OtherV3 != PathPoint)
                        {
                            if (OtherV3 == PathPoint + new Vector3Int(0, 0, 1))
                                Forward = true;
                            else if (OtherV3 == PathPoint + new Vector3Int(1, 0, 0))
                                Right = true;
                            else if (OtherV3 == PathPoint + new Vector3Int(0, 0, -1))
                                Backward = true;
                            else if (OtherV3 == PathPoint + new Vector3Int(-1, 0, 0))
                                Left = true;
                        }
                    }

                    //Check if sharing
                    foreach(Edge OtherEdge in MSTEdges)
                    {
                        if (OtherEdge != E)
                        {
                            bool Sharing = false;

                            for (int OtherPathPointIndex = 0; OtherPathPointIndex < OtherEdge.PathPoints.Count; OtherPathPointIndex++)
                            {
                                if (OtherEdge.PathPoints[OtherPathPointIndex] == PathPoint)
                                {
                                    Sharing = true;
                                    break;
                                }
                            }

                            if (Sharing)
                            {
                                foreach (Vector3Int OtherV3 in OtherEdge.PathPoints)
                                {
                                    if (OtherV3 != PathPoint)
                                    {
                                        if (OtherV3 == PathPoint + new Vector3Int(0, 0, 1))
                                            Forward = true;
                                        else if (OtherV3 == PathPoint + new Vector3Int(1, 0, 0))
                                            Right = true;
                                        else if (OtherV3 == PathPoint + new Vector3Int(0, 0, -1))
                                            Backward = true;
                                        else if (OtherV3 == PathPoint + new Vector3Int(-1, 0, 0))
                                            Left = true;
                                    }
                                }
                            }
                        }
                    }

                    //Check for adj stairs
                    if (CellTypeGrid.InBounds(PathPoint + new Vector3Int(0, 0, 1)) && CellTypeGrid[PathPoint + new Vector3Int(0, 0, 1)] == CellType.Stairs)
                        Forward = true;
                    else if (CellTypeGrid.InBounds(PathPoint + new Vector3Int(1, 0, 0)) && CellTypeGrid[PathPoint + new Vector3Int(1, 0, 0)] == CellType.Stairs)
                        Right = true;
                    else if (CellTypeGrid.InBounds(PathPoint + new Vector3Int(0, 0, -1)) && CellTypeGrid[PathPoint + new Vector3Int(0, 0, -1)] == CellType.Stairs)
                        Backward = true;
                    else if (CellTypeGrid.InBounds(PathPoint + new Vector3Int(-1, 0, 0)) && CellTypeGrid[PathPoint + new Vector3Int(-1, 0, 0)] == CellType.Stairs)
                        Left = true;

                    //Instantiate base
                    PrefabReferences.Add(Instantiate(Dset.HallwayBase, PathPoint * GridScale, Quaternion.identity, transform));

                    //Instantate walls
                    if (!Forward)
                        PrefabReferences.Add(Instantiate(Dset.HallwayWall, PathPoint * GridScale, Quaternion.Euler(new Vector3Int(0, 0, 0)), transform));
                    if (!Right)
                        PrefabReferences.Add(Instantiate(Dset.HallwayWall, PathPoint * GridScale, Quaternion.Euler(new Vector3Int(0, 90, 0)), transform));
                    if (!Backward)
                        PrefabReferences.Add(Instantiate(Dset.HallwayWall, PathPoint * GridScale, Quaternion.Euler(new Vector3Int(0, 180, 0)), transform));
                    if (!Left)
                        PrefabReferences.Add(Instantiate(Dset.HallwayWall, PathPoint * GridScale, Quaternion.Euler(new Vector3Int(0, 270, 0)), transform));
                }
                #endregion

                #region Stairs
                else if (CellTypeGrid[PathPoint] == CellType.Stairs)
                {
                    if ((i != E.PathPoints.Count-1 && E.PathPoints[i + 1] == E.PathPoints[i] + Vector3Int.up) || 
                        (i != 0 && E.PathPoints[i - 1] == E.PathPoints[i] + Vector3Int.up))
                    {
                        Edge SubEdge = E.SubEdges[SubEdgeIndex];

                        Vector3Int Rot = Vector3Int.zero;
                        if (SubEdge.V2IdealOffset == new Vector3Int(0, 0, 1))
                            Rot = new Vector3Int(0, 0, 0);
                        else if (SubEdge.V2IdealOffset == new Vector3Int(1, 0, 0))
                            Rot = new Vector3Int(0, 90, 0);
                        else if (SubEdge.V2IdealOffset == new Vector3Int(0, 0, -1))
                            Rot = new Vector3Int(0, 180, 0);
                        else if (SubEdge.V2IdealOffset == new Vector3Int(-1, 0, 0))
                            Rot = new Vector3Int(0, 270, 0);

                        PrefabReferences.Add(Instantiate(Dset.Stairs, PathPoint * GridScale, Quaternion.Euler(Rot), transform));

                        SubEdgeIndex++;
                    }
                }
                #endregion
            }
        }
    }

    void FillUnusedDoors()
    {
        foreach(Door D in MSTDoors)
        {
            bool Used = false;

            foreach(Edge E in MSTEdges)
            {
                if (E.V1 == D.Pos || E.V2 == D.Pos)
                {
                    Used = true;
                    break;
                }
            }

            if (!Used)
            {
                Vector3Int Rot = Vector3Int.zero;

                if (D.Dir == new Vector3Int(0, 0, -1))
                    Rot = new Vector3Int(0, 0, 0);
                else if (D.Dir == new Vector3Int(-1, 0, 0))
                    Rot = new Vector3Int(0, 90, 0);
                else if (D.Dir == new Vector3Int(0, 0, 1))
                    Rot = new Vector3Int(0, 180, 0);
                else if (D.Dir == new Vector3Int(1, 0, 0))
                    Rot = new Vector3Int(0, 270, 0);

                PrefabReferences.Add(Instantiate(Dset.UnusedDoorFiller, D.Pos * GridScale, Quaternion.Euler(Rot), transform));
            }
        }
    }

    void SpawnPlayer()
    {
        PlayerReference.position = PlayerSpawn;
    }

    private void OnDrawGizmos()
    {
        //Draw grid bounds
        Gizmos.color = Color.white;
        if (DrawGridBounds)
            Gizmos.DrawWireCube(GridSize/2 * GridScale, GridSize * GridScale);

        //Draw Doors
        Gizmos.color = Color.cyan;
        if (DrawDoors && MSTDoors != null)
            foreach (Door D in MSTDoors)
                Gizmos.DrawSphere(D.Pos * GridScale, 1);

        //Draw MST
        Gizmos.color = Color.blue;
        if (DrawMST && MSTEdges != null)
        {
            foreach (Edge E in MSTEdges)
                Gizmos.DrawLine(E.V1 * GridScale, E.V2 * GridScale);
        }

        //Draw SubEdges
        Gizmos.color = Color.yellow;
        if (DrawSubEdges && MSTEdges != null)
        {
            foreach(Edge MSTEdge in MSTEdges)
                foreach (Edge SubEdge in MSTEdge.SubEdges)
                {
                    Gizmos.DrawLine(SubEdge.V1 * GridScale, SubEdge.V2 * GridScale);
                    Gizmos.DrawSphere((SubEdge.V2 + SubEdge.V2IdealOffset) * GridScale, 1);
                }
        }

        //Draw hallways
        if (DrawHallways && MSTEdges != null)
        {
            foreach(Edge E in MSTEdges)
            {
                for (int i = 0; i < E.PathPoints.Count; i++)
                {
                    Vector3Int CurrentPPoint = E.PathPoints[i];

                    if (CellTypeGrid[CurrentPPoint] == CellType.Stairs)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawCube(CurrentPPoint * GridScale, new Vector3Int(1 * GridScale, 0, 1 * GridScale));
                    }

                    else if (CellTypeGrid[CurrentPPoint] == CellType.Hallway)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube((CurrentPPoint * GridScale) + (Vector3.up * 0.1f), new Vector3Int(1 * GridScale, 0, 1 * GridScale));
                    }

                    else //Shouldnt happen
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawCube(CurrentPPoint * GridScale, new Vector3Int(1 * GridScale, 0, 1 * GridScale));
                    }
                }
            }

        }
    }
}

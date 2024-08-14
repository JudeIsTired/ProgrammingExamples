using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SmoothCaveGenerator : MonoBehaviour
{
    [Header("Rescources")]
    Vector2[] EdgeTable = new Vector2[12]
        {
            new Vector2(0,1),
            new Vector2(1,2),
            new Vector2(2,3),
            new Vector2(3,0),
            new Vector2(4,5),
            new Vector2(5,6),
            new Vector2(6,7),
            new Vector2(7,4),
            new Vector2(0,4),
            new Vector2(5,1),
            new Vector2(6,2),
            new Vector2(7,3)
        };
    int[][] TriTable = new int[][]
        {
            new int[] {},
            new int[] { 0, 8, 3 },
            new int[] { 0, 1, 9 },
            new int[] { 1, 8, 3, 9, 8, 1 },
            new int[] { 1, 2, 10 },
            new int[] { 0, 8, 3, 1, 2, 10 },
            new int[] { 9, 2, 10, 0, 2, 9 },
            new int[] { 2, 8, 3, 2, 10, 8, 10, 9, 8 },
            new int[] { 3, 11, 2 },
            new int[] { 0, 11, 2, 8, 11, 0 },
            new int[] { 1, 9, 0, 2, 3, 11 },
            new int[] { 1, 11, 2, 1, 9, 11, 9, 8, 11 },
            new int[] { 3, 10, 1, 11, 10, 3 },
            new int[] { 0, 10, 1, 0, 8, 10, 8, 11, 10 },
            new int[] { 3, 9, 0, 3, 11, 9, 11, 10, 9 },
            new int[] { 9, 8, 10, 10, 8, 11 },
            new int[] { 4, 7, 8 },
            new int[] { 4, 3, 0, 7, 3, 4 },
            new int[] { 0, 1, 9, 8, 4, 7 },
            new int[] { 4, 1, 9, 4, 7, 1, 7, 3, 1 },
            new int[] { 1, 2, 10, 8, 4, 7 },
            new int[] { 3, 4, 7, 3, 0, 4, 1, 2, 10 },
            new int[] { 9, 2, 10, 9, 0, 2, 8, 4, 7 },
            new int[] { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4 },
            new int[] { 8, 4, 7, 3, 11, 2 },
            new int[] { 11, 4, 7, 11, 2, 4, 2, 0, 4 },
            new int[] { 9, 0, 1, 8, 4, 7, 2, 3, 11 },
            new int[] { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1 },
            new int[] { 3, 10, 1, 3, 11, 10, 7, 8, 4 },
            new int[] { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4 },
            new int[] { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3 },
            new int[] { 4, 7, 11, 4, 11, 9, 9, 11, 10 },
            new int[] { 9, 5, 4 },
            new int[] { 9, 5, 4, 0, 8, 3 },
            new int[] { 0, 5, 4, 1, 5, 0 },
            new int[] { 8, 5, 4, 8, 3, 5, 3, 1, 5 },
            new int[] { 1, 2, 10, 9, 5, 4 },
            new int[] { 3, 0, 8, 1, 2, 10, 4, 9, 5 },
            new int[] { 5, 2, 10, 5, 4, 2, 4, 0, 2 },
            new int[] { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8 },
            new int[] { 9, 5, 4, 2, 3, 11 },
            new int[] { 0, 11, 2, 0, 8, 11, 4, 9, 5 },
            new int[] { 0, 5, 4, 0, 1, 5, 2, 3, 11 },
            new int[] { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5 },
            new int[] { 10, 3, 11, 10, 1, 3, 9, 5, 4 },
            new int[] { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10 },
            new int[] { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3 },
            new int[] { 5, 4, 8, 5, 8, 10, 10, 8, 11 },
            new int[] { 9, 7, 8, 5, 7, 9 },
            new int[] { 9, 3, 0, 9, 5, 3, 5, 7, 3 },
            new int[] { 0, 7, 8, 0, 1, 7, 1, 5, 7 },
            new int[] { 1, 5, 3, 3, 5, 7 },
            new int[] { 9, 7, 8, 9, 5, 7, 10, 1, 2 },
            new int[] { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3 },
            new int[] { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2 },
            new int[] { 2, 10, 5, 2, 5, 3, 3, 5, 7 },
            new int[] { 7, 9, 5, 7, 8, 9, 3, 11, 2 },
            new int[] { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11 },
            new int[] { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7 },
            new int[] { 11, 2, 1, 11, 1, 7, 7, 1, 5 },
            new int[] { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11 },
            new int[] { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0 },
            new int[] { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0 },
            new int[] { 11, 10, 5, 7, 11, 5 },
            new int[] { 10, 6, 5 },
            new int[] { 0, 8, 3, 5, 10, 6 },
            new int[] { 9, 0, 1, 5, 10, 6 },
            new int[] { 1, 8, 3, 1, 9, 8, 5, 10, 6 },
            new int[] { 1, 6, 5, 2, 6, 1 },
            new int[] { 1, 6, 5, 1, 2, 6, 3, 0, 8 },
            new int[] { 9, 6, 5, 9, 0, 6, 0, 2, 6 },
            new int[] { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8 },
            new int[] { 2, 3, 11, 10, 6, 5 },
            new int[] { 11, 0, 8, 11, 2, 0, 10, 6, 5 },
            new int[] { 0, 1, 9, 2, 3, 11, 5, 10, 6 },
            new int[] { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11 },
            new int[] { 6, 3, 11, 6, 5, 3, 5, 1, 3 },
            new int[] { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6 },
            new int[] { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9 },
            new int[] { 6, 5, 9, 6, 9, 11, 11, 9, 8 },
            new int[] { 5, 10, 6, 4, 7, 8 },
            new int[] { 4, 3, 0, 4, 7, 3, 6, 5, 10 },
            new int[] { 1, 9, 0, 5, 10, 6, 8, 4, 7 },
            new int[] { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4 },
            new int[] { 6, 1, 2, 6, 5, 1, 4, 7, 8 },
            new int[] { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7 },
            new int[] { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6 },
            new int[] { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9 },
            new int[] { 3, 11, 2, 7, 8, 4, 10, 6, 5 },
            new int[] { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11 },
            new int[] { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6 },
            new int[] { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6 },
            new int[] { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6 },
            new int[] { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11 },
            new int[] { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7 },
            new int[] { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9 },
            new int[] { 10, 4, 9, 6, 4, 10 },
            new int[] { 4, 10, 6, 4, 9, 10, 0, 8, 3 },
            new int[] { 10, 0, 1, 10, 6, 0, 6, 4, 0 },
            new int[] { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10 },
            new int[] { 1, 4, 9, 1, 2, 4, 2, 6, 4, },
            new int[] { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4 },
            new int[] { 0, 2, 4, 4, 2, 6 },
            new int[] { 8, 3, 2, 8, 2, 4, 4, 2, 6 },
            new int[] { 10, 4, 9, 10, 6, 4, 11, 2, 3 },
            new int[] { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6 },
            new int[] { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10 },
            new int[] { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1 },
            new int[] { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3 },
            new int[] { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1 },
            new int[] { 3, 11, 6, 3, 6, 0, 0, 6, 4 },
            new int[] { 6, 4, 8, 11, 6, 8 },
            new int[] { 7, 10, 6, 7, 8, 10, 8, 9, 10 },
            new int[] { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10 },
            new int[] { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0 },
            new int[] { 10, 6, 7, 10, 7, 1, 1, 7, 3 },
            new int[] { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7 },
            new int[] { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9 },
            new int[] { 7, 8, 0, 7, 0, 6, 6, 0, 2 },
            new int[] { 7, 3, 2, 6, 7, 2},
            new int[] { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7 },
            new int[] { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7 },
            new int[] { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11 },
            new int[] { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1 },
            new int[] { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6 },
            new int[] { 0, 9, 1, 11, 6, 7 },
            new int[] { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0 },
            new int[] { 7, 11, 6 },
            new int[] { 7, 6, 11 },
            new int[] { 3, 0, 8, 11, 7, 6 },
            new int[] { 0, 1, 9, 11, 7, 6 },
            new int[] { 8, 1, 9, 8, 3, 1, 11, 7, 6 },
            new int[] { 10, 1, 2, 6, 11, 7 },
            new int[] { 1, 2, 10, 3, 0, 8, 6, 11, 7 },
            new int[] { 2, 9, 0, 2, 10, 9, 6, 11, 7 },
            new int[] { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8 },
            new int[] { 7, 2, 3, 6, 2, 7 },
            new int[] { 7, 0, 8, 7, 6, 0, 6, 2, 0 },
            new int[] { 2, 7, 6, 2, 3, 7, 0, 1, 9 },
            new int[] { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6 },
            new int[] { 10, 7, 6, 10, 1, 7, 1, 3, 7 },
            new int[] { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8 },
            new int[] { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7 },
            new int[] { 7, 6, 10, 7, 10, 8, 8, 10, 9 },
            new int[] { 6, 8, 4, 11, 8, 6 },
            new int[] { 3, 6, 11, 3, 0, 6, 0, 4, 6 },
            new int[] { 8, 6, 11, 8, 4, 6, 9, 0, 1 },
            new int[] { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6 },
            new int[] { 6, 8, 4, 6, 11, 8, 2, 10, 1 },
            new int[] { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6 },
            new int[] { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9 },
            new int[] { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3 },
            new int[] { 8, 2, 3, 8, 4, 2, 4, 6, 2 },
            new int[] { 0, 4, 2, 4, 6, 2 },
            new int[] { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8 },
            new int[] { 1, 9, 4, 1, 4, 2, 2, 4, 6 },
            new int[] { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1 },
            new int[] { 10, 1, 0, 10, 0, 6, 6, 0, 4 },
            new int[] { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3 },
            new int[] { 10, 9, 4, 6, 10, 4 },
            new int[] { 4, 9, 5, 7, 6, 11 },
            new int[] { 0, 8, 3, 4, 9, 5, 11, 7, 6 },
            new int[] { 5, 0, 1, 5, 4, 0, 7, 6, 11 },
            new int[] { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5 },
            new int[] { 9, 5, 4, 10, 1, 2, 7, 6, 11 },
            new int[] { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5 },
            new int[] { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2 },
            new int[] { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6 },
            new int[] { 7, 2, 3, 7, 6, 2, 5, 4, 9 },
            new int[] { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7 },
            new int[] { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0 },
            new int[] { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8 },
            new int[] { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7 },
            new int[] { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4 },
            new int[] { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10 },
            new int[] { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10 },
            new int[] { 6, 9, 5, 6, 11, 9, 11, 8, 9 },
            new int[] { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5 },
            new int[] { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11 },
            new int[] { 6, 11, 3, 6, 3, 5, 5, 3, 1 },
            new int[] { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6 },
            new int[] { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10 },
            new int[] { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5 },
            new int[] { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3 },
            new int[] { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2 },
            new int[] { 9, 5, 6, 9, 6, 0, 0, 6, 2 },
            new int[] { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8 },
            new int[] { 1, 5, 6, 2, 1, 6 },
            new int[] { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6 },
            new int[] { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0 },
            new int[] { 0, 3, 8, 5, 6, 10 },
            new int[] { 10, 5, 6 },
            new int[] { 11, 5, 10, 7, 5, 11 },
            new int[] { 11, 5, 10, 11, 7, 5, 8, 3, 0 },
            new int[] { 5, 11, 7, 5, 10, 11, 1, 9, 0 },
            new int[] { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1 },
            new int[] { 11, 1, 2, 11, 7, 1, 7, 5, 1 },
            new int[] { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11 },
            new int[] { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7 },
            new int[] { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2 },
            new int[] { 2, 5, 10, 2, 3, 5, 3, 7, 5 },
            new int[] { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5 },
            new int[] { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2 },
            new int[] { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2 },
            new int[] { 1, 3, 5, 3, 7, 5 },
            new int[] { 0, 8, 7, 0, 7, 1, 1, 7, 5 },
            new int[] { 9, 0, 3, 9, 3, 5, 5, 3, 7 },
            new int[] { 9, 8, 7, 5, 9, 7 },
            new int[] { 5, 8, 4, 5, 10, 8, 10, 11, 8, },
            new int[] { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0 },
            new int[] { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5 },
            new int[] { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4 },
            new int[] { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8 },
            new int[] { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11 },
            new int[] { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5 },
            new int[] { 9, 4, 5, 2, 11, 3 },
            new int[] { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4 },
            new int[] { 5, 10, 2, 5, 2, 4, 4, 2, 0 },
            new int[] { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9 },
            new int[] { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2 },
            new int[] { 8, 4, 5, 8, 5, 3, 3, 5, 1 },
            new int[] { 0, 4, 5, 1, 0, 5 },
            new int[] { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5 },
            new int[] { 9, 4, 5 },
            new int[] { 4, 11, 7, 4, 9, 11, 9, 10, 11 },
            new int[] { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11 },
            new int[] { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11 },
            new int[] { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4 },
            new int[] { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2 },
            new int[] { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3 },
            new int[] { 11, 7, 4, 11, 4, 2, 2, 4, 0 },
            new int[] { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4 },
            new int[] { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9 },
            new int[] { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7 },
            new int[] { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10 },
            new int[] { 1, 10, 2, 8, 7, 4 },
            new int[] { 4, 9, 1, 4, 1, 7, 7, 1, 3 },
            new int[] { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1 },
            new int[] { 4, 0, 3, 7, 4, 3 },
            new int[] { 4, 8, 7 },
            new int[] { 9, 10, 8, 10, 11, 8 },
            new int[] { 3, 0, 9, 3, 9, 11, 11, 9, 10 },
            new int[] { 0, 1, 10, 0, 10, 8, 8, 10, 11 },
            new int[] { 3, 1, 10, 11, 3, 10 },
            new int[] { 1, 2, 11, 1, 11, 9, 9, 11, 8 },
            new int[] { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9 },
            new int[] { 0, 2, 11, 8, 0, 11 },
            new int[] { 3, 2, 11 },
            new int[] { 2, 3, 8, 2, 8, 10, 10, 8, 9 },
            new int[] { 9, 10, 2, 0, 9, 2 },
            new int[] { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8 },
            new int[] { 1, 10, 2 },
            new int[] { 1, 3, 8, 9, 1, 8 },
            new int[] { 0, 9, 1 },
            new int[] { 0, 3, 8 },
            new int[] { }
        };

    [Header("Settings")]
    public Vector3 Size;
    public Vector3 NoiseSamplePoint;
    public Vector3 SamplePointOffset;
    public float NoiseThreshold = 0.5f;
    public float NoiseScale = 0.1f;

    [Header("Lists")]
    List<Vector3> CurrentPoints;
    List<float> CurrentNoiseValues;
    List<List<Vector3>> Cubes = new List<List<Vector3>>();

    [Header("Mesh")]
    Mesh MainMesh;
    [SerializeField] List<Vector3> Vertices = new List<Vector3>();
    [SerializeField] List<int> Triangles = new List<int>();
    [SerializeField] List<Vector2> uvs = new List<Vector2>();

    [Header("Debug")]
    string BinaryID;
    public bool Regenerate;
    public bool DebugMode;

    //Private
    int i_IndexLooper = 0;

    //Async operations
    CancellationTokenSource TokenSource;

    private void Start()
    {
        //Async token
        TokenSource = new CancellationTokenSource();

        GenerateMap();
    }

    private void Update()
    {
        if (Regenerate)
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        //StartStopwatch();

        NoiseSamplePoint = new Vector3(
            ((transform.position.x / 100) * Size.x) + SamplePointOffset.x,
            ((transform.position.y / 100) * Size.y) + SamplePointOffset.y,
            ((transform.position.z / 100) * Size.z) + SamplePointOffset.z);

        MainMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = MainMesh;

        Vertices = new List<Vector3>();
        Triangles = new List<int>();
        i_IndexLooper = 0; //Value used to convert indexs to triangle indexs

        MarchCubesAsync();
    }

    //Async cancel sources
    private void OnDestroy()
    {
        TokenSource.Cancel();
    }

    async void MarchCubesAsync()
    {
        bool ProcessComplete = false;

        var Result = await Task.Run(() =>
        {
            SetCubes();

            foreach (List<Vector3> CurrentCube in Cubes)
            {
                GetMeshInfo(CurrentCube);
                if (TokenSource.IsCancellationRequested)
                {
                    return (Vertices, Triangles, ProcessComplete);
                }
            }

            ProcessComplete = true;

            return (Vertices, Triangles, ProcessComplete);
        });

        if (ProcessComplete)
        {
            UpdateMesh();
            Destroy(GetComponent<SmoothCaveGenerator>());
            //StopStopwatch("Generation complete in: ");
        }
    }

    void SetCubes()
    {
        Vector3[] BaseCubeVertices = new Vector3[8]
        {
            new Vector3(0,0,1),
            new Vector3(1,0,1),
            new Vector3(1,0,0),
            new Vector3(0,0,0),
            new Vector3(0,1,1),
            new Vector3(1,1,1),
            new Vector3(1,1,0),
            new Vector3(0,1,0)
        };

        Vector3[] CurrentCubeVertices = new Vector3[8]
        {
            new Vector3(0,0,1),
            new Vector3(1,0,1),
            new Vector3(1,0,0),
            new Vector3(0,0,0),
            new Vector3(0,1,1),
            new Vector3(1,1,1),
            new Vector3(1,1,0),
            new Vector3(0,1,0)
        };

        Vector3 OverflowValues = new Vector3(Size.x, Size.y, Size.z);

        while (true)
        {
            Cubes.Add(new List<Vector3> { CurrentCubeVertices[0], CurrentCubeVertices[1], CurrentCubeVertices[2], CurrentCubeVertices[3], CurrentCubeVertices[4], CurrentCubeVertices[5], CurrentCubeVertices[6], CurrentCubeVertices[7] });

            ShiftV3Array("Z", CurrentCubeVertices);
            if (CheckForArrayOverflow("Z", CurrentCubeVertices, OverflowValues))
            {
                ReturnArrayValues("Z", CurrentCubeVertices, BaseCubeVertices);
                ShiftV3Array("Y", CurrentCubeVertices);

                if (CheckForArrayOverflow("Y", CurrentCubeVertices, OverflowValues))
                {
                    ReturnArrayValues("Y", CurrentCubeVertices, BaseCubeVertices);
                    ShiftV3Array("X", CurrentCubeVertices);
                    if (CheckForArrayOverflow("X", CurrentCubeVertices, OverflowValues))
                    {
                        break;
                    }
                }
            }
        }
    }

    void ShiftV3Array(string Axis, Vector3[] SelectedArray)
    {
        if (Axis == "X")
        {
            for (int i = 0; i < SelectedArray.Length; i++)
            {
                SelectedArray[i] += new Vector3(1, 0, 0);
            }
        }

        if (Axis == "Y")
        {
            for (int i = 0; i < SelectedArray.Length; i++)
            {
                SelectedArray[i] += new Vector3(0, 1, 0);
            }
        }

        if (Axis == "Z")
        {
            for (int i = 0; i < SelectedArray.Length; i++)
            {
                SelectedArray[i] += new Vector3(0, 0, 1);
            }
        }
    }

    bool CheckForArrayOverflow(string Axis, Vector3[] SelectedArray, Vector3 MaxValues)
    {
        bool Overflow = false;

        foreach (Vector3 V3 in SelectedArray)
        {
            if (V3.x > MaxValues.x && Axis == "X")
            {
                Overflow = true;
                break;
            }

            if (V3.y > MaxValues.y && Axis == "Y")
            {
                Overflow = true;
                break;
            }

            if (V3.z > MaxValues.z && Axis == "Z")
            {
                Overflow = true;
                break;
            }

        }

        return Overflow;
    }

    void ReturnArrayValues(string Axis, Vector3[] SelectedArray, Vector3[] OrignalValues)
    {
        for (int i = 0; i < OrignalValues.Length; i++)
        {
            if (Axis == "X")
            {
                SelectedArray[i].x = OrignalValues[i].x;
            }

            if (Axis == "Y")
            {
                SelectedArray[i].y = OrignalValues[i].y;
            }

            if (Axis == "Z")
            {
                SelectedArray[i].z = OrignalValues[i].z;
            }
        }
    }

    void GetMeshInfo(List<Vector3> CurrentCube)
    {
        //Create new list to contain binary code for current cubes setup
        List<int> CurrentBinary = new List<int>();

        //Get noise values
        List<float> NoiseValues = new List<float>();
        foreach (Vector3 V3 in CurrentCube)
        {
            NoiseValues.Add(Perlin3D((NoiseSamplePoint.x + V3.x) * NoiseScale, (NoiseSamplePoint.y + V3.y) * NoiseScale, (NoiseSamplePoint.z + V3.z) * NoiseScale));
        }

        for (int i = 7; i >= 0; i--)
        {
            if (NoiseValues[i] > NoiseThreshold)
                CurrentBinary.Add(0);

            else
                CurrentBinary.Add(1);
        }

        BinaryID = (CurrentBinary[0].ToString() + CurrentBinary[1].ToString() + CurrentBinary[2].ToString() + CurrentBinary[3].ToString() + CurrentBinary[4].ToString() + CurrentBinary[5].ToString() + CurrentBinary[6].ToString() + CurrentBinary[7].ToString());

        if (BinaryID != "00000000")
        {
            int CubeIndex = 0;
            CubeIndex += CurrentBinary[0] * 128;
            CubeIndex += CurrentBinary[1] * 64;
            CubeIndex += CurrentBinary[2] * 32;
            CubeIndex += CurrentBinary[3] * 16;
            CubeIndex += CurrentBinary[4] * 8;
            CubeIndex += CurrentBinary[5] * 4;
            CubeIndex += CurrentBinary[6] * 2;
            CubeIndex += CurrentBinary[7] * 1;

            //Refer back to triangulation table and add tris
            int[] Triangulation = TriTable[CubeIndex];

            foreach (int EdgeIndex in Triangulation)
            {
                //Get vert numbers
                int VertA = Mathf.RoundToInt(EdgeTable[EdgeIndex].x); //Converts edge index to int (already int but requires mathf.round)
                int VertB = Mathf.RoundToInt(EdgeTable[EdgeIndex].y); //Converts edge index to int (already int but requires mathf.round)

                //Refer back to current points array
                //Get midpoint
                Vector3 MidPoint = (CurrentCube[VertA] + CurrentCube[VertB]) / 2;

                if (!Vertices.Contains(MidPoint))
                {
                    Vertices.Add(MidPoint);
                    Triangles.Add(i_IndexLooper);
                    i_IndexLooper++;
                }

                else
                {
                    Triangles.Add(Vertices.IndexOf(MidPoint));
                }
            }
        }
    }

    void InterpolateVertices()
    {

    }

    void UpdateMesh()
    {
        MainMesh.Clear();

        MainMesh.vertices = Vertices.ToArray();
        MainMesh.triangles = Triangles.ToArray();
        MainMesh.uv = uvs.ToArray();

        MainMesh.RecalculateNormals();
    }

    public float Perlin3D(float x, float y, float z)
    {
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);

        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        float ABC = AB + BC + AC + BA + CB + CA;
        return ABC / 6;
    }

    //Stopwatch
    Stopwatch time;
    string t;

    void StartStopwatch()
    {
        time = new Stopwatch();
        time.Start();
        t = time.ElapsedMilliseconds.ToString();
    }

    void StopStopwatch(string OutputBrief)
    {
        time.Stop();
        System.TimeSpan timeSpan = time.Elapsed;
        print(OutputBrief + timeSpan.ToString());
    }

    private void OnDrawGizmos()
    {
        if (DebugMode)
        {
            if (Vertices.Count != 0)
            {
                foreach (Vector3 Point in Vertices)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(Point, 0.2f);
                }
            }

            if (CurrentPoints != null)
            {
                foreach (Vector3 Point in CurrentPoints)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(Point, 0.1f);
                }
            }
        }
    }
}
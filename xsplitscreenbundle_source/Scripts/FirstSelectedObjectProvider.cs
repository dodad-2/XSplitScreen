using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstSelectedObjectProvider : MonoBehaviour
{
	public GameObject firstSelectedObject;
	public GameObject[] fallBackFirstSelectedObjects;
	public GameObject[] enforceCurrentSelectionIsInList;
	public bool takeAbsolutePriority;
}

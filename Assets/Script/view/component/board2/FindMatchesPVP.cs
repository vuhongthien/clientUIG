using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindMatchesPVP : MonoBehaviour
{
    public static FindMatchesPVP Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private BoardPVP board;
    public List<GameObject> currentMatches = new List<GameObject>();
    void Start()
    {
        board = FindFirstObjectByType<BoardPVP>();

    }
    public void FindAllMatches()
    {
        StartCoroutine(FindAllMatchesCo());
    }
    public IEnumerator FindAllMatchesCo()
    {
        yield return new WaitForSeconds(.1f);
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                GameObject currentDot = board.allDots[i, j];
                if (currentDot != null)
                {
                    if (i > 0 && i < board.width - 1)
                    {
                        GameObject leftdot = board.allDots[i - 1, j];
                        GameObject rightdot = board.allDots[i + 1, j];
                        if (leftdot != null && rightdot != null)
                        {
                            if (leftdot.tag == currentDot.tag && rightdot.tag == currentDot.tag)
                            {
                                if (!currentMatches.Contains(leftdot))
                                {
                                    currentMatches.Add(leftdot);
                                }
                                leftdot.GetComponent<DotPVP>().isMathched = true;
                                if (!currentMatches.Contains(rightdot))
                                {
                                    currentMatches.Add(rightdot);
                                }
                                rightdot.GetComponent<DotPVP>().isMathched = true;
                                if (!currentMatches.Contains(currentDot))
                                {
                                    currentMatches.Add(currentDot);
                                }
                                currentDot.GetComponent<DotPVP>().isMathched = true;
                            }
                        }
                    }

                    if (j > 0 && j < board.height - 1)
                    {
                        GameObject updot = board.allDots[i, j + 1];
                        GameObject downdot = board.allDots[i, j - 1];
                        if (updot != null && downdot != null)
                        {
                            if (updot.tag == currentDot.tag && downdot.tag == currentDot.tag)
                            {
                                if (!currentMatches.Contains(updot))
                                {
                                    currentMatches.Add(updot);
                                }
                                updot.GetComponent<DotPVP>().isMathched = true;
                                if (!currentMatches.Contains(downdot))
                                {
                                    currentMatches.Add(downdot);
                                }
                                downdot.GetComponent<DotPVP>().isMathched = true;
                                if (!currentMatches.Contains(currentDot))
                                {
                                    currentMatches.Add(currentDot);
                                }
                                currentDot.GetComponent<DotPVP>().isMathched = true;
                            }
                        }
                    }
                }
            }
        }
    }

    public void VirtualFindAllMatches(HashSet<GameObject> matches)
{
    for (int i = 0; i < Board.Instance.width; i++)
    {
        for (int j = 0; j < Board.Instance.height; j++)
        {
            GameObject currentDot = Board.Instance.allDots[i, j];
            if (currentDot != null)
            {
                // Kiểm tra match ngang
                if (i > 0 && i < Board.Instance.width - 1)
                {
                    GameObject leftDot = Board.Instance.allDots[i - 1, j];
                    GameObject rightDot = Board.Instance.allDots[i + 1, j];
                    if (leftDot != null && rightDot != null &&
                        leftDot.tag == currentDot.tag && 
                        rightDot.tag == currentDot.tag)
                    {
                        matches.Add(leftDot);
                        matches.Add(currentDot);
                        matches.Add(rightDot);
                    }
                }

                // Kiểm tra match dọc
                if (j > 0 && j < Board.Instance.height - 1)
                {
                    GameObject upDot = Board.Instance.allDots[i, j + 1];
                    GameObject downDot = Board.Instance.allDots[i, j - 1];
                    if (upDot != null && downDot != null &&
                        upDot.tag == currentDot.tag && 
                        downDot.tag == currentDot.tag)
                    {
                        matches.Add(upDot);
                        matches.Add(currentDot);
                        matches.Add(downDot);
                    }
                }
            }
        }
    }
}

    // Update is called once per frame
    void Update()
    {

    }
}

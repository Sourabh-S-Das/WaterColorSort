using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using rng = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public BottleController firstBottle;
    public BottleController secondBottle;

    public GameObject bottle;

    private Color[] colorList = { Color.red, Color.blue, Color.green, Color.cyan, Color.magenta, Color.yellow, Color.white };

    private int colorsCount;
    private int repeatsPerColor = 4;
    private int extraTubes;
    private int shuffleMoves = 100;
    public int solvedCount = 0;
    public LineRenderer lineRenderer;

    private float spacing = 1.0f;

    private List<GameObject> spawnedBottles = new List<GameObject>();

    void Start()
    {
        colorsCount = rng.Range(3, 8);
        extraTubes = rng.Range(2, 4);

        var configuration = GenerateConfiguration();

        float totalWidth = (configuration.Count - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < configuration.Count; i++)
        {
            Vector3 pos = new Vector3(startX + i * spacing, 0f, 0f);
            GameObject newBottle = Instantiate(bottle, pos, Quaternion.identity);

            newBottle.GetComponent<BottleController>().numberOfColorsInBottle = configuration[i].Count;
            newBottle.GetComponent<BottleController>().lineRenderer = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
            for (int j = 0; j < configuration[i].Count; j++)
                newBottle.GetComponent<BottleController>().bottleColors[j] = colorList[configuration[i][j]];
            newBottle.GetComponent<BottleController>().Init();

            spawnedBottles.Add(newBottle);
            if (newBottle.GetComponent<BottleController>().numberOfTopColorLayers == repeatsPerColor) solvedCount++;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && solvedCount != colorsCount)
            processBottle();

        bool animFinished = true;
        for (int i = 0; i < spawnedBottles.Count; i++)
        {
            if (spawnedBottles[i].GetComponent<BottleController>().animFinished == false)
            {
                animFinished = false;
                break;
            }
        }

        if (solvedCount == colorsCount && animFinished)
        {
            SceneManager.LoadScene("WinScreen");
        }
    }

    void processBottle()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.GetComponent<BottleController>() != null)
            {
                if (firstBottle == null)
                {
                    firstBottle = hit.collider.GetComponent<BottleController>();
                    firstBottle.transform.position += new Vector3(0f, 0.2f, 0f);
                    if (firstBottle.numberOfColorsInBottle == 0)
                    {
                        firstBottle.transform.position -= new Vector3(0f, 0.2f, 0f);
                        firstBottle = null;
                    }
                }
                else
                {
                    if (firstBottle == hit.collider.GetComponent<BottleController>())
                    {
                        firstBottle.transform.position -= new Vector3(0f, 0.2f, 0f);
                        firstBottle = null;
                    }
                    else
                    {
                        secondBottle = hit.collider.GetComponent<BottleController>();
                        firstBottle.bottleControllerRef = secondBottle;

                        if (secondBottle.FillBottleCheck(firstBottle.topColor) == true)
                        {
                            int filled = 0;
                            if (firstBottle.numberOfTopColorLayers == repeatsPerColor) filled = 1;
                            firstBottle.StartColorTransfer();
                            if (secondBottle.numberOfTopColorLayers == repeatsPerColor) solvedCount += 1 - filled;
                        }

                        firstBottle = null;
                        secondBottle = null;
                    }
                }
            }
        }
    }

    List<List<int>> GenerateConfiguration()
    {
        List<List<int>> tubes = new List<List<int>>();

        for (int i = 0; i < colorsCount; i++)
        {
            List<int> tube = new List<int>();
            for (int j = 0; j < repeatsPerColor; j++)
            {
                tube.Add(i);
            }
            tubes.Add(tube);
        }

        for (int i = 0; i < extraTubes; i++)
        {
            tubes.Add(new List<int>());
        }

        for (int move = 0; move < shuffleMoves; move++)
        {
            int sourceIndex = rng.Range(0, tubes.Count);
            var source = tubes[sourceIndex];
            while (source.Count == 0)
            {
                sourceIndex = rng.Range(0, tubes.Count);
                source = tubes[sourceIndex];
            }

            int destIndex = rng.Range(0, tubes.Count);
            var dest = tubes[destIndex];
            while (sourceIndex == destIndex || dest.Count == 4)
            {
                destIndex = rng.Range(0, tubes.Count);
                dest = tubes[destIndex];
            }

            int color = source[source.Count - 1];

            int count = 1;
            for (int i = source.Count - 2; i >= 0; i--)
            {
                if (source[i] == color)
                    count++;
                else
                    break;
            }
            if (count != source.Count)
                count--;
            if (count == 0) continue;

            int destEmpty = 4 - dest.Count;
            int maxMove = Math.Min(count, destEmpty);

            bool validDestination = false;
            if (dest.Count == 0)
                validDestination = true;
            else
            {
                if (source.Count == 4 || dest[dest.Count - 1] != color)
                    validDestination = true;
            }
            if (!validDestination) continue;

            int removeCount = rng.Range(1, maxMove + 1);
            for (int i = 0; i < removeCount; i++)
            {
                source.RemoveAt(source.Count - 1);
                dest.Add(color);
            }
        }

        return tubes;
    }
}

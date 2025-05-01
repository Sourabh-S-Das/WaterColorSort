using System.Collections;
using UnityEngine;

public class BottleController : MonoBehaviour
{
    public Color[] bottleColors;
    public SpriteRenderer bottleMaskSR;

    public AnimationCurve ScaleAndRotationMultiplierCurve;
    public AnimationCurve FillAmountCurve;

    public AnimationCurve RotationSpeedMultiplier;

    public float[] fillAmounts;
    public float[] rotationValues;

    private int rotationIndex = 0;

    [Range(0, 4)]
    public int numberOfColorsInBottle;

    public Color topColor;
    private Color oldColor;
    public int numberOfTopColorLayers = 0;

    public BottleController bottleControllerRef;
    private int numberOfColorsToTransfer = 0;

    public Transform leftRotationPoint;
    public Transform rightRotationPoint;
    private Transform chosenRotationPoint;

    private float directionMultiplier = 1.0f;

    Vector3 originalPosition;
    Vector3 startPosition;
    Vector3 endPosition;

    public LineRenderer lineRenderer;

    public float timeToRotate = 1.0f;

    public bool animFinished = true;

    public void Init()
    {
        bottleMaskSR.material.SetFloat("_FillAmount", fillAmounts[numberOfColorsInBottle]);

        UpdateColorsOnShader();
        UpdateTopColorValues();
    }

    public void StartColorTransfer()
    {
        originalPosition = transform.position;
        animFinished = false;
        ChooseRotationPointandDirection();

        numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);

        for (int i = 0; i < numberOfColorsToTransfer; i++)
        {
            bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
        }

        CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);

        numberOfColorsInBottle -= numberOfColorsToTransfer;
        bottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;

        bottleControllerRef.UpdateColorsOnShader();
        bottleControllerRef.UpdateTopColorValues();

        oldColor = topColor;

        UpdateTopColorValues();

        transform.GetComponent<SpriteRenderer>().sortingOrder += 2;
        bottleMaskSR.sortingOrder += 2;

        StartCoroutine(MoveBottle());
    }

    IEnumerator MoveBottle()
    {
        startPosition = transform.position;
        if (chosenRotationPoint == leftRotationPoint)
        {
            endPosition = bottleControllerRef.rightRotationPoint.position;
        }
        else
        {
            endPosition = bottleControllerRef.leftRotationPoint.position;
        }

        float t = 0.0f;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            t += Time.deltaTime * 2;

            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;

        StartCoroutine(RotateBottle());
    }

    IEnumerator RotateBottle()
    {
        float t = 0.0f;
        float lerpValue;
        float angleValue;

        float lastAngleValue = 0.0f;

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(0.0f, directionMultiplier *  rotationValues[rotationIndex], lerpValue);

            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);
            bottleMaskSR.material.SetFloat("_SARM", ScaleAndRotationMultiplierCurve.Evaluate(angleValue));

            if (fillAmounts[numberOfColorsInBottle + numberOfColorsToTransfer] > FillAmountCurve.Evaluate(angleValue) + 0.005f)
            {
                if (lineRenderer.enabled == false)
                {
                    lineRenderer.startColor = oldColor;
                    lineRenderer.endColor = oldColor;

                    lineRenderer.SetPosition(0, chosenRotationPoint.position);
                    lineRenderer.SetPosition(1, chosenRotationPoint.position - Vector3.up * 1.45f);

                    lineRenderer.enabled = true;
                }

                bottleMaskSR.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));

                bottleControllerRef.FillUp(FillAmountCurve.Evaluate(lastAngleValue) - FillAmountCurve.Evaluate(angleValue));
            }

            t += Time.deltaTime * RotationSpeedMultiplier.Evaluate(angleValue);
            lastAngleValue = angleValue;

            yield return new WaitForEndOfFrame();
        }

        angleValue = directionMultiplier * rotationValues[rotationIndex];
        bottleMaskSR.material.SetFloat("_SARM", ScaleAndRotationMultiplierCurve.Evaluate(angleValue));
        bottleMaskSR.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));

        lineRenderer.enabled = false;

        StartCoroutine(RotateBottleBack());
    }

    IEnumerator RotateBottleBack()
    {
        float t = 0.0f;
        float lerpValue;
        float angleValue;

        float lastAngleValue = directionMultiplier * rotationValues[rotationIndex];

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(directionMultiplier * rotationValues[rotationIndex], 0.0f, lerpValue);

            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);
            bottleMaskSR.material.SetFloat("_SARM", ScaleAndRotationMultiplierCurve.Evaluate(angleValue));

            lastAngleValue = angleValue;
            t += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        angleValue = 0.0f;
        transform.eulerAngles = new Vector3(0.0f, 0.0f, angleValue);
        bottleMaskSR.material.SetFloat("_SARM", ScaleAndRotationMultiplierCurve.Evaluate(angleValue));

        StartCoroutine(MoveBottleBack());
    }

    IEnumerator MoveBottleBack()
    {
        startPosition = transform.position;
        endPosition = originalPosition - new Vector3(0f, 0.2f, 0f);

        float t = 0.0f;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            t += Time.deltaTime * 2;

            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;

        transform.GetComponent<SpriteRenderer>().sortingOrder -= 2;
        bottleMaskSR.sortingOrder -= 2;

        animFinished = true;
    }

    void UpdateColorsOnShader()
    {
        bottleMaskSR.material.SetColor("_C1", bottleColors[0]);
        bottleMaskSR.material.SetColor("_C2", bottleColors[1]);
        bottleMaskSR.material.SetColor("_C3", bottleColors[2]);
        bottleMaskSR.material.SetColor("_C4", bottleColors[3]);
    }

    public void UpdateTopColorValues()
    {
        numberOfTopColorLayers = 0;
        topColor = Color.clear;

        if (numberOfColorsInBottle == 0)
            return;

        topColor = bottleColors[numberOfColorsInBottle - 1];
        numberOfTopColorLayers = 1;

        for (int i = numberOfColorsInBottle - 2; i >= 0; i--)
        {
            if (bottleColors[i] == topColor)
                numberOfTopColorLayers++;
            else
                break;
        }
    }

    public bool FillBottleCheck(Color colorToCheck)
    {
        if (numberOfColorsInBottle == 0)
            return true;
        else
        {
            if (numberOfColorsInBottle == 4)
                return false;
            else
            {
                if(topColor.Equals(colorToCheck))
                    return true;
                else
                    return false;
            }
        }
    }

    private void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
    {
        rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
    }

    private void FillUp(float fillAmountToAdd)
    {
        bottleMaskSR.material.SetFloat("_FillAmount", bottleMaskSR.material.GetFloat("_FillAmount") + fillAmountToAdd);
    }

    private void ChooseRotationPointandDirection()
    {
        if (transform.position.x > bottleControllerRef.transform.position.x)
        {
            chosenRotationPoint = leftRotationPoint;
            directionMultiplier = -1.0f;
        }
        else
        {
            chosenRotationPoint = rightRotationPoint;
            directionMultiplier = 1.0f;
        }
    }
}

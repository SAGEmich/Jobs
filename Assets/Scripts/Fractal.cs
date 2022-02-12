using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Fractal : MonoBehaviour
{
    struct FractalPart
    {
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
    }

    private static Vector3[] directions =
    {
        Vector3.up,
        Vector3.right,
        Vector3.left,
        Vector3.back,
        Vector3.forward
    };

    private static Quaternion[] rotations =
    {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f),
        Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f),
        Quaternion.Euler(-90f, 0f, 0f)
    };

    [SerializeField, Range(1, 8)]
    private int depth = 4;

    [SerializeField]
    private Mesh mesh;

    [SerializeField]
    private Material material;
    
    private FractalPart[][] parts;

    private Matrix4x4[][] matrices;
    
    ComputeBuffer[] matricesBuffer;

    private void OnEnable()
    {
        parts = new FractalPart[depth][];
        matrices = new Matrix4x4[depth][];
        matricesBuffer = new ComputeBuffer[depth];
        int stride = 16 * 4; //Macierz 4×4 ma szesnaście wartości zmiennoprzecinkowych, więc skok buforów wynosi szesnaście razy cztery bajty.

        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new FractalPart[length];
            matrices[i] = new Matrix4x4[length];
            matricesBuffer[i] = new ComputeBuffer(length, stride);
        }

        parts[0][0] = CreatePart(0);

        for(int li = 1; li < parts.Length; li++) //li = level index
        {
            FractalPart[] levelParts = parts[li];

            for(int fpi = 0; fpi < levelParts.Length; fpi += 5) //fpi = fractal part iterator
            {
                for(int ci = 0; ci < 5; ci++) //ci = children index
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }
    }

    private void OnDisable()
    {
        for(int i = 0; i < matricesBuffer.Length; i++)
        {
            matricesBuffer[i].Release();
        }

        parts = null;
        matrices = null;
        matricesBuffer = null;
    }

    private void OnValidate()
    {
        if(parts != null && enabled)
        {
            OnDisable();
            OnEnable();   
        }
    }

    FractalPart CreatePart(int childIndex) =>
        new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };

    void Update()
    {
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation = rootPart.rotation * quaternion.Euler(0f, rootPart.spinAngle, 0f);
        parts[0][0] = rootPart;
        matrices[0][0] = Matrix4x4.TRS(rootPart.worldPosition, rootPart.worldRotation, Vector3.one);

        float scale = 1f;
        for(int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            Matrix4x4[] levelMatrices = matrices[li];

            for(int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];
                part.spinAngle += spinAngleDelta;
                part.worldRotation = parent.worldRotation * (part.rotation * quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);
                levelParts[fpi] = part;
                
                levelMatrices[fpi] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
            }
        }

        for(int i = 0; i < matricesBuffer.Length; i++)
        {
            matricesBuffer[i].SetData(matrices[i]);
        }
    }
}

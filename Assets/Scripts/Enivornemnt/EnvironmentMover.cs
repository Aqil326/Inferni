using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentMover : MonoBehaviour
{

    private Vector3 startPostion;
    public Vector3 rotationSpeed = new Vector3(0f, 0f, 0f);
    public Vector3 sineMove = new Vector3(0f, 0f, 0f);
    public float sineMoveSpeed = 1f;

    private void Start()
    {
        startPostion = transform.position;
    }

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
        transform.position = startPostion +
            sineMove * Mathf.Sin(Time.time * sineMoveSpeed);
    }
}

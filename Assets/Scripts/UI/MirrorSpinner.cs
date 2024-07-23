using UnityEngine;

public class MirrorSpinner : MonoBehaviour
{
    private Transform centerTransform;
    private float radius = 1f;
    private float angularSpeed = 45f;
    private float angleOffset;
    private int index;
    private bool isPaused;

    private void Update()
    {
        if (centerTransform == null ||  isPaused) return;
        
        var angle = (Time.time * angularSpeed + index * angleOffset) * Mathf.Deg2Rad;
        var offset = new Vector3(Mathf.Cos(angle) * radius, 1, Mathf.Sin(angle) * radius);
        transform.localPosition = centerTransform.localPosition + offset;
        
        var directionToTarget = centerTransform.position - transform.position;
        directionToTarget.y = 0;
        transform.rotation = Quaternion.LookRotation(directionToTarget, Vector3.up);   
    }
    
    public void ShowMirrors(float spinOffset, int spinIndex, Transform playerTransform, float spinRadius, float spinAngularSpeed)
    {
        angleOffset = spinOffset;
        index = spinIndex;
        centerTransform = playerTransform;
        radius = spinRadius;
        angularSpeed = spinAngularSpeed;
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }
}

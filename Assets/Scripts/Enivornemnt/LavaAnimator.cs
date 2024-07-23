using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LavaAnimator : MonoBehaviour
{
    [SerializeField] private bool _moveX;
    [SerializeField] private bool _moveY;
    [SerializeField] private float _speed;
    
    private const string TEXTURE_NAME = "_MainTex";
    private Material _temproraryMaterial;
    private Coroutine _animationCoroutine;
    private Vector2 _tempOffset = Vector2.zero;
    void OnEnable()
    {
        _temproraryMaterial = GetComponent<Renderer>().material;
        _temproraryMaterial = new Material(_temproraryMaterial);
        GetComponent<Renderer>().material = _temproraryMaterial;
        _animationCoroutine = StartCoroutine("MovementAnimation");
    }
    
    private void OnDisable()
    {
        StopCoroutine(_animationCoroutine);
    }

    private IEnumerator MovementAnimation()
    {
        yield return new WaitForEndOfFrame();
        if (_moveX) _tempOffset.x += _speed;
        if (_moveY) _tempOffset.y += _speed;
        _temproraryMaterial.SetTextureOffset(TEXTURE_NAME, _tempOffset);
        _animationCoroutine = StartCoroutine("MovementAnimation");
    }
}

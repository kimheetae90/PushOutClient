using UnityEngine;
using System.Collections.Generic;

public class Actor : MonoBehaviour
{
    public static float ScaleFactor = 5;

    public GameObject Model { get; private set; }
    public Animator ModelAnimator { get; private set; }
    public SkinnedMeshRenderer MeshRenderer { get; private set; }
    public new Rigidbody rigidbody;
    public new Transform transform; 
    new public CapsuleCollider collider;
    public CameraSetting cameraSetting;
    

    public float Height { get; set; }

    private void Awake()
    {
        if (rigidbody == null)
        {
            rigidbody = this.GetComponent<Rigidbody>();
        }

        if (transform == null)
        {
            transform = this.GetComponent<Transform>();
        }

        if (collider == null)
        {
            collider = this.GetComponent<CapsuleCollider>();
        }
    }

    public void Initiallize(string resourceKey, bool containAnimator = true)
    {
        if(!SetModel(resourceKey))
        {
            return;
        }

        if (containAnimator)
        {
            SetAnimator();
            SetMeshRenderer();
        }
    }

    public bool SetModel(string resourceKey)
    {
        GameObject model = ResourceLoader.Instance.Get(resourceKey) as GameObject;
        if (model == null)
        {
            Debug.LogError("[Actor]Model doesn't exist!");
            return false;
        }

        model = Instantiate(model);
        SetModel(model);

        return true;
    }

    public void SetModel(GameObject model)
    {
        model.transform.SetParent(transform);
        Model = model;
    }

    public void SetAnimator()
    {
        ModelAnimator = Model.GetComponentInChildren<Animator>();
        if (ModelAnimator == null)
        {
            Debug.LogError("[Actor]ModelAnimator doesn't exist!");
        }
    }

    public void SetMeshRenderer()
    {
        if (MeshRenderer == null && ModelAnimator != null)
        {
            MeshRenderer = ModelAnimator.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }

    /// <summary>
    /// Move Transform! Doesn't use physics
    /// </summary>
    public void Translate(Vector3 moveDirection)
    {
        rigidbody.MovePosition(transform.position + transform.right);
    }

    public void SetPosition(Vector3 moveDirection)
    {
        rigidbody.MovePosition(moveDirection * ScaleFactor);
    }

    public void SetRotate(Vector3 lookVector)
    {
        Model.transform.rotation = Quaternion.LookRotation(lookVector);
    }

    public void Clear()
    {
        SetAlpha(false);
        if(MeshRenderer)
            MeshRenderer.enabled = true;
        ModelAnimator = null;
        DestroyImmediate(Model);
        Model = null;
    }

    public void SetAlpha(bool alpha)
    {
        if (MeshRenderer == null)
            return;

        if (alpha)
        {
            MeshRenderer.material.SetOverrideTag("RenderType", "Transparent");
            MeshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            MeshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            MeshRenderer.material.SetInt("_ZWrite", 0);
            MeshRenderer.material.DisableKeyword("_ALPHATEST_ON");
            MeshRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            MeshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            MeshRenderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            MeshRenderer.material.SetOverrideTag("RenderType", "");
            MeshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            MeshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            MeshRenderer.material.SetInt("_ZWrite", 1);
            MeshRenderer.material.DisableKeyword("_ALPHATEST_ON");
            MeshRenderer.material.DisableKeyword("_ALPHABLEND_ON");
            MeshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            MeshRenderer.material.renderQueue = -1;
        }
    }
    public void OnDestroy()
    {
        Clear();
    }
}

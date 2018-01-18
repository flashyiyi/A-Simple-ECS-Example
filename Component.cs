using UnityEngine;

public class BaseComponent : DItem
{
    public Entity entity;
}

public class PositionComponent : BaseComponent
{
    public Vector2 value;
}

public class SizeComponent : BaseComponent
{
    public float value;
}

public class SpeedComponent : BaseComponent
{
    public Vector2 value;
    public float maxValue;
}

public class ColorComponent : BaseComponent
{
    public Color value = Color.white;
}

public class TeamComponent : BaseComponent
{
    public int id;
}

//与Unity组件的桥接
public class GameObjectComponent : BaseComponent
{
    public GameObject gameObject;
    public Transform transform;
    public SpriteRenderer spriteRenderer;
}

//临时特效型Component
public class EatingComponent : BaseComponent
{
    public GameObjectComponent go;
    public PositionComponent target;
    public Vector2 startOffest;
    public Vector2 endOffest;
    public float dur = 0.2f;
    public float endTime;

    //仅操作数据的方法可以存在
    public float GetLifePercent()
    {
        return 1f - (endTime - Time.time) / dur;
    }

    public void Start()
    {
        endTime = Time.time + dur;
    }

    public Vector2 GetCurPosition()
    {
        return target.value + Vector2.Lerp(startOffest, endOffest, GetLifePercent());
    }
}

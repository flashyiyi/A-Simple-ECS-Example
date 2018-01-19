using UnityEngine;
using UnityEngine.Rendering;

#region Component
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
#endregion

#region Entity
public class Entity : DItem
{
    public GameWorld world;
    public GameObjectComponent gameObject;
    public PositionComponent position;
    public SizeComponent size;
    public ColorComponent color;
    public TeamComponent team;
    public Entity()
    {
        gameObject = new GameObjectComponent() { entity = this };
        position = new PositionComponent() { entity = this };
        size = new SizeComponent() { entity = this };
        color = new ColorComponent() { entity = this };
        team = new TeamComponent() { entity = this };
    }
}

public class MoveAbleEntity : Entity
{
    public SpeedComponent speed;
    public MoveAbleEntity() : base()
    {
        speed = new SpeedComponent() { entity = this };
    }
}
#endregion

#region System
public class SystemBase
{
    public GameWorld world;
    public SystemBase(GameWorld world)
    {
        this.world = world;
    }
}

//移动
public class MoveSystem : SystemBase
{
    public MoveSystem(GameWorld world) : base(world) { }
    public void Add(SpeedComponent speed)
    {
        world.speeds.DelayAdd(speed);
    }

    public void Remove(SpeedComponent speed)
    {
        world.speeds.DelayRemove(speed);
    }

    public void Update(SpeedComponent speed, PositionComponent position, SizeComponent size)
    {
        position.value += speed.value * Time.deltaTime;
        if (position.value.x > world.screenRect.xMax - size.value)
        {
            position.value.x = world.screenRect.xMax - size.value;
            speed.value.x = 0f;
        }
        else if (position.value.x < world.screenRect.xMin + size.value)
        {
            position.value.x = world.screenRect.xMin + size.value;
            speed.value.x = 0f;
        }
        if (position.value.y > world.screenRect.yMax - size.value)
        {
            position.value.y = world.screenRect.yMax - size.value;
            speed.value.y = 0f;
        }
        else if (position.value.y < world.screenRect.yMin + size.value)
        {
            position.value.y = world.screenRect.yMin + size.value;
            speed.value.y = 0f;
        }
    }
}

//操控
public class InputSystem : SystemBase
{
    public InputSystem(GameWorld world) : base(world) { }
    public void Update(SpeedComponent speed, PositionComponent position)
    {
        Vector2 delta = (Vector2)world.mainCamera.ScreenToWorldPoint(Input.mousePosition) - position.value;
        speed.value = Vector2.ClampMagnitude(speed.value + delta.normalized * Time.deltaTime, speed.maxValue);
    }
}

//吞食逻辑
public class EatSystem : SystemBase
{
    public EatSystem(GameWorld world) : base(world) { }
    public void Update(Entity source, Entity target)
    {
        float sizeSum = source.size.value + target.size.value + 0.05f;
        if ((source.position.value - target.position.value).sqrMagnitude < sizeSum * sizeSum)
        {
            source.size.value = Mathf.Sqrt(source.size.value * source.size.value + target.size.value * target.size.value);
            Eat(source, target);
        }
    }
    public void Eat(Entity e, Entity food)
    {
        world.eatingSystem.CreateFrom(food.gameObject, food.position, e.position);

        world.entitySystem.RemoveEntity(food);
        world.entitySystem.AddRandomEntity();
    }
}

//圆推挤
public class CirclePushSystem : SystemBase
{
    public CirclePushSystem(GameWorld world) : base(world) { }
    public void Update(PositionComponent pos1, SizeComponent size1, PositionComponent pos2, SizeComponent size2)
    {
        Vector2 center = Vector2.Lerp(pos1.value, pos2.value, size1.value / (size1.value + size2.value));
        Vector2 offest = pos1.value - center;
        float offestSqrMagnitude = offest.sqrMagnitude;
        float sqrRadius = size1.value * size1.value;
        if (offestSqrMagnitude < sqrRadius)
        {
            float offestMagnitude = Mathf.Sqrt(offestSqrMagnitude);
            if (offestMagnitude == 0)
                offestMagnitude = 0.01f;
            float pushMul = Mathf.Min(size1.value - offestMagnitude, (1 - offestMagnitude / size1.value) * Time.deltaTime * 10f);
            pos1.value += offest.normalized * pushMul;
        }
    }
}

//吞食动画
public class EatingSystem : SystemBase
{
    public EatingSystem(GameWorld world) : base(world) { }
    public void Update(EatingComponent e)
    {
        e.go.transform.position = e.GetCurPosition();
        if (Time.time >= e.endTime)
        {
            world.eatings.DelayRemove(e);
            world.gameObjectSystem.Remove(e.go);
        }
    }

    public void CreateFrom(GameObjectComponent gameObject, PositionComponent source, PositionComponent target)
    {
        gameObject.entity.gameObject = null;//解除和原entity的关系

        EatingComponent comp = new EatingComponent();
        comp.go = gameObject;
        comp.target = target;
        comp.startOffest = source.value - target.value;
        comp.endOffest = Vector2.Lerp(source.value, target.value, 0.5f) - target.value;
        comp.Start();
        world.eatings.DelayAdd(comp);
    }
}

//和Unity显示部分的桥接
public class GameObjectSystem : SystemBase
{
    public GameObjectSystem(GameWorld world) : base(world) { }
    public void Add(GameObjectComponent e, PositionComponent position, SizeComponent size, ColorComponent color)
    {
        e.gameObject = new GameObject("Entity");
        e.transform = e.gameObject.transform;
        e.transform.localScale = Vector2.one * 0.001f;
        e.spriteRenderer = e.gameObject.AddComponent<SpriteRenderer>();
        e.spriteRenderer.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Update(e, position, size, color);
    }

    public void Remove(GameObjectComponent go)
    {
        GameObject.Destroy(go.gameObject);
        go.transform = null;
        go.gameObject = null;
        go.spriteRenderer = null;
    }

    public void Update(GameObjectComponent go, PositionComponent position, SizeComponent size, ColorComponent color)
    {
        go.transform.position = position.value;
        go.transform.localScale = Vector2.one * Mathf.MoveTowards(go.transform.localScale.x, size.value * 11f, Mathf.Max(0.01f, Mathf.Abs(go.transform.localScale.x - size.value)) * 10f * Time.deltaTime);
        go.spriteRenderer.color = color.value;
    }

    public void SetToTop(GameObjectComponent go)
    {
        go.gameObject.AddComponent<SortingGroup>().sortingOrder = 1;
    }
}

//增删物体和场景初始化
public class EntitySystem : SystemBase
{
    public EntitySystem(GameWorld world) : base(world) { }

    public void AddEntity(Entity e)
    {
        world.entitys.DelayAdd(e);
        world.gameObjectSystem.Add(e.gameObject, e.position, e.size, e.color);
    }

    public void RemoveEntity(Entity e)
    {
        world.entitys.DelayRemove(e);
        if (e.gameObject != null)
            world.gameObjectSystem.Remove(e.gameObject);
    }

    public void AddRandomEntity()
    {
        Entity e = new Entity();
        e.size.value = 0.025f;
        e.team.id = 0;
        e.position.value = new Vector2(Random.Range(world.screenRect.xMin + e.size.value, world.screenRect.xMax - e.size.value), Random.Range(world.screenRect.yMin + e.size.value, world.screenRect.yMax - e.size.value));
        AddEntity(e);
    }

    public void AddMoveAbleEnity(MoveAbleEntity e)
    {
        this.AddEntity(e);
        world.playerEntitys.Add(e);

        world.moveSystem.Add(e.speed);
        world.gameObjectSystem.SetToTop(e.gameObject);
    }

    public void InitScene()
    {
        for (int i = 0; i < 50; i++)
        {
            AddRandomEntity();
        }

        for (int i = 0; i < 2; i++)
        {
            MoveAbleEntity playerEntity = new MoveAbleEntity();
            playerEntity.position.value = Vector2.zero;
            playerEntity.size.value = 0.05f;
            playerEntity.color.value = Color.yellow;
            playerEntity.speed.maxValue = 1f;
            playerEntity.team.id = 1;
            playerEntity.position.value = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            AddMoveAbleEnity(playerEntity);
        }
    }
}
#endregion

public class GameWorld : MonoBehaviour
{
    public DList<Entity> entitys;
    public DList<SpeedComponent> speeds;
    public DList<MoveAbleEntity> playerEntitys;

    public DList<EatingComponent> eatings;

    public EntitySystem entitySystem;
    public MoveSystem moveSystem;
    public GameObjectSystem gameObjectSystem;
    public InputSystem inputSystem;
    public CirclePushSystem circlePushSystem;
    public EatSystem eatSystem;
    public EatingSystem eatingSystem;

    public Camera mainCamera;
    public Rect screenRect;

    void Start ()
    {
        if (Camera.main == null)
        {
            GameObject go = new GameObject("Camera");
            mainCamera = go.AddComponent<Camera>();
        }
        else
        {
            mainCamera = Camera.main;
        }
        mainCamera.clearFlags = CameraClearFlags.Color;
        mainCamera.backgroundColor = Color.black;
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 1f;
        mainCamera.nearClipPlane = 0f;

        screenRect = Rect.MinMaxRect(-mainCamera.aspect, -1f, mainCamera.aspect, 1f);

        entitys = new DList<Entity>();
        playerEntitys = new DList<MoveAbleEntity>();
        speeds = new DList<SpeedComponent>();
        eatings = new DList<EatingComponent>();

        entitySystem = new EntitySystem(this);
        moveSystem = new MoveSystem(this);
        gameObjectSystem = new GameObjectSystem(this);

        inputSystem = new InputSystem(this);

        eatSystem = new EatSystem(this);
        eatingSystem = new EatingSystem(this);
        circlePushSystem = new CirclePushSystem(this);

        entitySystem.InitScene();
        ApplyDelayCommands();
    }
    
    public void ApplyDelayCommands()
    {
        entitys.ApplyDelayCommands();
        playerEntitys.ApplyDelayCommands();
        speeds.ApplyDelayCommands();
        eatings.ApplyDelayCommands();
    }

    void Update ()
    {
        //遍历所有Entity并执行所有相关System
        foreach (Entity item in entitys)
        {
            if (item.destroyed)
                continue;

            gameObjectSystem.Update(item.gameObject, item.position, item.size,item.color);
        }
        //多对多关系
        foreach (MoveAbleEntity player in playerEntitys)
        {
            if (player.destroyed)
                continue;

            inputSystem.Update(player.speed, player.position);
            foreach (Entity item in entitys)
            {
                if (item == player || item.destroyed)
                    continue;

                if (item.team.id == 0) //是食物，执行吃逻辑
                    eatSystem.Update(player, item);
                else if (item.team.id == 1) //是玩家控制角色，执行圆推挤逻辑
                    circlePushSystem.Update(player.position, player.size, item.position, item.size);
            }
        }
        //单独遍历某些Component
        foreach (SpeedComponent speed in speeds)
        {
            if (speed.destroyed)
                continue;

            Entity enity = speed.entity;
            moveSystem.Update(speed, enity.position, enity.size);
        }
        //和Entity无关的Component
        foreach (EatingComponent item in eatings)
        {
            if (item.destroyed)
                continue;

            eatingSystem.Update(item);
        }

        ApplyDelayCommands();
    }
}

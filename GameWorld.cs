using UnityEngine;
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

            inputSystem.Update(player);
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

            MoveAbleEntity moveEnity = speed.entity as MoveAbleEntity;
            moveSystem.Update(speed, moveEnity.position, moveEnity.size);
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

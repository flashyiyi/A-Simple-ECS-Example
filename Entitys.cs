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
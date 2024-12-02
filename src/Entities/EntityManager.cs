namespace Undergang.Entities;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Undergang.Game;

public class EntityManager(Node3D rootNode)
{
    private readonly Dictionary<int, Entity> _entities = [];
    private int _nextId = 0;
    private readonly Node3D _rootNode = rootNode;

    public int GetNextId()
    {
        return _nextId++;
    }

    public void AddEntity(Entity entity)
    {
        _entities[entity.Id] = entity;
    }

    public Dictionary<int, Entity> GetEntities()
    {
        return _entities;
    }

    public Node3D GetRootNode()
    {
        return _rootNode;
    }

    public void RemoveEntity(Entity entity)
    {
        // If it has a visual component, remove it from the scene
        if (entity.Has<RenderComponent>())
            entity.Get<RenderComponent>().Node3D.QueueFree();

        // Remove from entity manager
        _entities.Remove(entity.Id);
    }

    public List<Entity> GetHexGrid() =>
        _entities.Values
            .Where(e => e.Has<HexTileComponent>())
            .ToList();

    public Entity GetEntity(int id) => _entities[id];

    public List<Entity> GetEnemies() =>
        _entities.Values
            .Where(e => e.Has<UnitTypeComponent>() &&
                       e.Get<UnitTypeComponent>().UnitType == UnitType.Grunt)
            .ToList();

    public Entity GetPlayer() =>
        _entities.Values
            .FirstOrDefault(e => e.Has<UnitTypeComponent>() &&
                                e.Get<UnitTypeComponent>().UnitType == UnitType.Player);
}
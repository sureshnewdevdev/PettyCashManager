using PettyCashManager.Domain;

namespace PettyCashManager.Infrastructure;

public sealed class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly Dictionary<Guid, T> _store = new();

    public Result<T> Add(T entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (_store.ContainsKey(entity.Id))
            return Result<T>.Fail("Entity already exists", $"Id: {entity.Id}");

        _store[entity.Id] = entity;
        return Result<T>.Ok(entity, "Added");
    }

    public Result<T> Update(T entity)
    {
        if (entity.Id == Guid.Empty || !_store.ContainsKey(entity.Id))
            return Result<T>.Fail("Entity not found", $"Id: {entity.Id}");

        _store[entity.Id] = entity;
        return Result<T>.Ok(entity, "Updated");
    }

    public Result<bool> Remove(Guid id)
    {
        if (!_store.ContainsKey(id))
            return Result<bool>.Fail("Entity not found", $"Id: {id}");

        _store.Remove(id);
        return Result<bool>.Ok(true, "Removed");
    }

    public Result<T> GetById(Guid id)
    {
        if (!_store.TryGetValue(id, out var entity))
            return Result<T>.Fail("Entity not found", $"Id: {id}");

        return Result<T>.Ok(entity, "Found");
    }

    public Result<List<T>> GetAll()
    {
        return Result<List<T>>.Ok(_store.Values.ToList(), "All");
    }
}

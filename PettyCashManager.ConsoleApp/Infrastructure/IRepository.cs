using PettyCashManager.Domain;

namespace PettyCashManager.Infrastructure;

public interface IRepository<T> where T : class, IEntity
{
    Result<T> Add(T entity);
    Result<T> Update(T entity);
    Result<bool> Remove(Guid id);
    Result<T> GetById(Guid id);
    Result<List<T>> GetAll();
}

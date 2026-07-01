namespace AccessTrust.Web.Repositories;

public interface IMongoRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task CreateAsync(T entity);
    Task<bool> ReplaceAsync(string id, T entity);
    Task<bool> DeleteAsync(string id);
}
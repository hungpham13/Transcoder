using Transcoder.Persistence;
using ErrorOr;
using Transcoder.Model;

namespace Transcoder.Services.Storage;

public class DatabaseService<T> : IDatabaseService<T> where T : BaseEntity
{
    private readonly TranscoderDbContext _dbContext;
    private readonly ILogger _logger;
    
    public DatabaseService(TranscoderDbContext dbContext, ILogger<DatabaseService<T>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public ErrorOr<Created> CreateData(T t)
    {
        try
        {
            _dbContext.Add(t);
            _dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.LogInformation("Create new data failed");
            return Error.Failure(e.Message);
        }
        return Result.Created;
    }

    public ErrorOr<T> GetData(Guid id)
    {
         var result =  _dbContext.Set<T>().AsQueryable().FirstOrDefault(m=>m.Id== id);
         if (result == null)
         {
             return Error.NotFound($"Could not find {typeof(T).Name} with id {id}");
         }
         return result;
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbContext.Set<T>().AsQueryable();
    }
    
    public ErrorOr<Updated> UpdateData(T t)
    {
        try
        {
            _dbContext.Update(t);
            _dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            _logger.LogInformation("Update data failed, error: " + e.Message );
            return Error.Failure(e.Message);
        }
        return Result.Updated;
    }

    public object RemoveData(Guid id)
    {
        _dbContext.Remove(GetData(id));
        _dbContext.SaveChanges(); 
        return true;
    }
}
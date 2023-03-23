using Transcoder.Model;
using ErrorOr;


namespace Transcoder.Services.Storage;

public interface IDatabaseService<T> where T: BaseEntity
{
    IQueryable<T> GetQueryable();
    ErrorOr<Created> CreateData(T t);
    ErrorOr<T> GetData(Guid id);
    ErrorOr<Updated> UpdateData(T t);
    object RemoveData(Guid id);
}
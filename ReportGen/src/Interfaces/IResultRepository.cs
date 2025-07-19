using ReportGen.Models;

namespace ReportGen.Interfaces
{
    public interface IResultRepository
    {
        Task<IEnumerable<Result>> GetResultsByParametersAsync(ResultFilterRepositoryDto parameters);
        Task<IEnumerable<Value>> GetLatestValuesAsync(string fileName);
    }
}
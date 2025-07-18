using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Interfaces;
using ReportGen.Models;

namespace ReportGen.Repositories
{
    public class ResultRepository : IResultRepository
    {
        private readonly ReportGenDbContext _context;

        public ResultRepository(ReportGenDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Value>> GetLatestValuesAsync()
        {
            return await _context.Values.OrderBy(v => v.StartDateTime).Take(10).ToListAsync();
        }

        public async Task<IEnumerable<Result>> GetResultsByParametersAsync(ResultFilterRepositoryDto parameters)
        {
            IQueryable<Result> query = _context.Results;

            if (!string.IsNullOrEmpty(parameters.FileName))
            {
                query = query.Where(r => r.FileName == parameters.FileName);
            }

            if (parameters.MinMinimumDateTime.HasValue)
            {
                query = query.Where(r => r.MinimumDateTime >= parameters.MinMinimumDateTime.Value);
            }
            if (parameters.MaxMinimumDateTime.HasValue)
            {
                query = query.Where(r => r.MinimumDateTime <= parameters.MaxMinimumDateTime.Value);
            }

            if (parameters.MinAvgExecutionTime.HasValue)
            {
                query = query.Where(r => r.AvgExecutionTime >= parameters.MinAvgExecutionTime.Value);
            }
            if (parameters.MaxAvgExecutionTime.HasValue)
            {
                query = query.Where(r => r.AvgExecutionTime <= parameters.MaxAvgExecutionTime.Value);
            }

            if (parameters.MinAvgStoreValue.HasValue)
            {
                query = query.Where(r => r.AvgStoreValue >= parameters.MinAvgStoreValue.Value);
            }
            if (parameters.MaxAvgStoreValue.HasValue)
            {
                query = query.Where(r => r.AvgStoreValue <= parameters.MaxAvgStoreValue.Value);
            }

            if (parameters.MinDeltaTimeS.HasValue)
            {
                query = query.Where(r => r.DeltaTimeS >= parameters.MinDeltaTimeS.Value);
            }
            if (parameters.MaxDeltaTimeS.HasValue)
            {
                query = query.Where(r => r.DeltaTimeS <= parameters.MaxDeltaTimeS.Value);
            }

            return await query.ToListAsync();
        }
    }
}
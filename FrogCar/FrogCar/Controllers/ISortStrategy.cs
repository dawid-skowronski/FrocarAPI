using System;
using System.Linq;
using System.Threading.Tasks;
using FrogCar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public interface ISortStrategy
{
    IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending);
}

public class PriceSortStrategy : ISortStrategy
{
    public IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending)
    {
        return ascending ? query.OrderBy(c => c.RentalPricePerDay) : query.OrderByDescending(c => c.RentalPricePerDay);
    }
}

public class BrandSortStrategy : ISortStrategy
{
    public IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending)
    {
        return ascending ? query.OrderBy(c => c.Brand) : query.OrderByDescending(c => c.Brand);
    }
}

public class EngineSortStrategy : ISortStrategy
{
    public IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending)
    {
        return ascending ? query.OrderBy(c => c.EngineCapacity) : query.OrderByDescending(c => c.EngineCapacity);
    }
}

public class SeatsSortStrategy : ISortStrategy
{
    public IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending)
    {
        return ascending ? query.OrderBy(c => c.Seats) : query.OrderByDescending(c => c.Seats);
    }
}

public class DefaultSortStrategy : ISortStrategy
{
    public IQueryable<CarListing> ApplySort(IQueryable<CarListing> query, bool ascending)
    {
        return query.OrderBy(c => c.Id);
    }
}
public class SortStrategyContext
{
    private readonly Dictionary<string, ISortStrategy> _strategies;

    public SortStrategyContext()
    {
        _strategies = new Dictionary<string, ISortStrategy>
        {
            { "price", new PriceSortStrategy() },
            { "brand", new BrandSortStrategy() },
            { "engine", new EngineSortStrategy() },
            { "seats", new SeatsSortStrategy() }
        };
    }

    public ISortStrategy GetStrategy(string sortBy)
    {
        sortBy = sortBy?.ToLower() ?? string.Empty;
        return _strategies.TryGetValue(sortBy, out var strategy) ? strategy : new DefaultSortStrategy();
    }
}
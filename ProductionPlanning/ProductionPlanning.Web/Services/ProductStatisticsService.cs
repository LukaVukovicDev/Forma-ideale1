using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Services;

public class ProductStatisticsService
{
    private static readonly TimeZoneInfo BelgradeTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "Central European Standard Time"
                : "Europe/Belgrade");

    private readonly ApplicationDbContext _dbContext;

    public ProductStatisticsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductProductionStatisticsViewModel> GetStatisticsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BelgradeTimeZone)
                .DateTime);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var chartStart = today.AddDays(-29);

        var monthlyPlans = _dbContext.PlannedProductions
            .AsNoTracking()
            .Where(plan =>
                plan.ProductId == productId &&
                plan.PlannedDate >= monthStart &&
                plan.PlannedDate < nextMonthStart);

        var plannedThisMonth = await monthlyPlans
            .SumAsync(
                plan => (long?)plan.PlannedQuantity,
                cancellationToken) ?? 0;

        var producedForThisMonthPlans = await _dbContext.ProductionRecords
            .AsNoTracking()
            .Where(record =>
                record.PlannedProduction.ProductId == productId &&
                record.PlannedProduction.PlannedDate >= monthStart &&
                record.PlannedProduction.PlannedDate < nextMonthStart)
            .SumAsync(
                record => (long?)record.Quantity,
                cancellationToken) ?? 0;

        var plannedByDate = await _dbContext.PlannedProductions
            .AsNoTracking()
            .Where(plan =>
                plan.ProductId == productId &&
                plan.PlannedDate >= chartStart &&
                plan.PlannedDate <= today)
            .GroupBy(plan => plan.PlannedDate)
            .Select(group => new
            {
                Date = group.Key,
                Quantity = group.Sum(plan => (long)plan.PlannedQuantity)
            })
            .ToListAsync(cancellationToken);

        var producedByPlanDate = await _dbContext.ProductionRecords
            .AsNoTracking()
            .Where(record =>
                record.PlannedProduction.ProductId == productId &&
                record.PlannedProduction.PlannedDate >= chartStart &&
                record.PlannedProduction.PlannedDate <= today)
            .GroupBy(record => record.PlannedProduction.PlannedDate)
            .Select(group => new
            {
                Date = group.Key,
                Quantity = group.Sum(record => (long)record.Quantity)
            })
            .ToListAsync(cancellationToken);

        var plansByDate = plannedByDate.ToDictionary(item => item.Date);
        var producedByPlanDateLookup = producedByPlanDate
            .ToDictionary(item => item.Date, item => item.Quantity);
        var planDailyValues = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var date = chartStart.AddDays(offset);
                return new ProductPlanDailyStatisticsViewModel
                {
                    Date = date,
                    PlannedQuantity = plansByDate.TryGetValue(date, out var plan)
                        ? plan.Quantity
                        : 0,
                    ProducedQuantity =
                        producedByPlanDateLookup.GetValueOrDefault(date)
                };
            })
            .ToArray();

        var dayRanges = Enumerable.Range(0, 30)
            .Select(offset => CreateDayRange(chartStart.AddDays(offset), offset))
            .ToArray();
        var recordedByDay = await GetRecordedTotalsByLocalDayAsync(
            productId,
            dayRanges,
            cancellationToken);
        var recordedDailyValues = dayRanges
            .Select(day => new ProductRecordedDailyStatisticsViewModel
            {
                Date = day.Date,
                Quantity = recordedByDay.GetValueOrDefault(day.Offset)
            })
            .ToArray();

        var recentPlans = await _dbContext.PlannedProductions
            .AsNoTracking()
            .Where(plan => plan.ProductId == productId)
            .OrderByDescending(plan => plan.PlannedDate)
            .ThenByDescending(plan => plan.Id)
            .Take(10)
            .Select(plan => new ProductPlanSummaryViewModel
            {
                PlannedDate = plan.PlannedDate,
                MachineCode = plan.Machine.SifraMasine,
                MachineName = plan.Machine.Naziv,
                PlannedQuantity = plan.PlannedQuantity,
                ProducedQuantity = plan.ProductionRecords
                    .Sum(record => (long?)record.Quantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        return new ProductProductionStatisticsViewModel
        {
            PlannedThisMonth = plannedThisMonth,
            ProducedForThisMonthPlans = producedForThisMonthPlans,
            PlanDailyValues = planDailyValues,
            RecordedDailyValues = recordedDailyValues,
            RecentPlans = recentPlans
        };
    }

    private async Task<Dictionary<int, long>> GetRecordedTotalsByLocalDayAsync(
        int productId,
        IReadOnlyList<RecordedDayRange> dayRanges,
        CancellationToken cancellationToken)
    {
        var firstRange = dayRanges[0];
        var lastRange = dayRanges[^1];
        var dayOffsetSelector = CreateDayOffsetSelector(dayRanges);

        return await _dbContext.ProductionRecords
            .AsNoTracking()
            .Where(record =>
                record.PlannedProduction.ProductId == productId &&
                record.RecordedAtUtc >= firstRange.StartUtc &&
                record.RecordedAtUtc < lastRange.EndUtc)
            .GroupBy(dayOffsetSelector)
            .Select(group => new
            {
                DayOffset = group.Key,
                Quantity = group.Sum(record => (long)record.Quantity)
            })
            .ToDictionaryAsync(
                result => result.DayOffset,
                result => result.Quantity,
                cancellationToken);
    }

    private static Expression<Func<ProductionRecord, int>>
        CreateDayOffsetSelector(IReadOnlyList<RecordedDayRange> dayRanges)
    {
        var record = Expression.Parameter(typeof(ProductionRecord), "record");
        var recordedAt = Expression.Property(
            record,
            nameof(ProductionRecord.RecordedAtUtc));
        Expression body = Expression.Constant(-1);

        foreach (var day in dayRanges.Reverse())
        {
            var isInRange = Expression.AndAlso(
                Expression.GreaterThanOrEqual(
                    recordedAt,
                    Expression.Constant(day.StartUtc)),
                Expression.LessThan(
                    recordedAt,
                    Expression.Constant(day.EndUtc)));

            body = Expression.Condition(
                isInRange,
                Expression.Constant(day.Offset),
                body);
        }

        return Expression.Lambda<Func<ProductionRecord, int>>(body, record);
    }

    private static RecordedDayRange CreateDayRange(DateOnly date, int offset)
    {
        var localStart = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(
            date.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);

        return new RecordedDayRange(
            date,
            offset,
            TimeZoneInfo.ConvertTimeToUtc(localStart, BelgradeTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, BelgradeTimeZone));
    }

    private sealed record RecordedDayRange(
        DateOnly Date,
        int Offset,
        DateTime StartUtc,
        DateTime EndUtc);
}

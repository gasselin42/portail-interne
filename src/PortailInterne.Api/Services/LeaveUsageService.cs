using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public class LeaveUsageService
{
	private readonly AppDbContext _db;

	public LeaveUsageService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<Dictionary<int, LeaveBalance.LeaveUsage>> UsageForAsync(IEnumerable<int> employeeIds)
	{
		var annualDays = await _db.Employees
			.Where(e => employeeIds.Contains(e.Id))
			.ToDictionaryAsync(e => e.Id, e => e.AnnualLeaveDays);

		var balanceLeaves = await LoadBalanceLeavesAsync(employeeIds);

		return annualDays
			.SelectMany(p => LeaveBalance.ComputeUsage(p.Value, balanceLeaves.GetValueOrDefault(p.Key, [])))
			.ToDictionary(p => p.Key, p => p.Value);
	}

	/// <summary>Complète WorkingDays et UnpaidDates des congés qui consomment le solde.</summary>
	public async Task AddUsageAsync(List<LeaveRequestResponse> leaves)
	{
		var employeeIds = leaves.Select(l => l.EmployeeId).Distinct();
		var usage = await UsageForAsync(employeeIds);

		foreach (var leave in leaves)
		{
			if (usage.TryGetValue(leave.Id, out var u))
			{
				leave.WorkingDays = u.WorkingDays;
				leave.UnpaidDates = u.UnpaidDates;
			}
		}
	}

	/// <summary>Congés qui consomment le solde (Congé, en attente ou approuvé), regroupés par employé.</summary>
	public async Task<Dictionary<int, List<LeaveBalance.BalanceLeave>>> LoadBalanceLeavesAsync(IEnumerable<int> employeeIds)
	{
		return (await _db.LeaveRequests
				.AsNoTracking()
				.Where(l => employeeIds.Contains(l.EmployeeId)
						&& l.Type == LeaveType.Conge
						&& (l.Status == LeaveStatus.EnAttente || l.Status == LeaveStatus.Approuve))
				.ToListAsync())
			.GroupBy(l => l.EmployeeId)
			.ToDictionary(
				g => g.Key,
				g => g.Select(l => new LeaveBalance.BalanceLeave(l.Id, l.StartDate, l.EndDate, l.CreatedAt)).ToList()
			);
	}
}
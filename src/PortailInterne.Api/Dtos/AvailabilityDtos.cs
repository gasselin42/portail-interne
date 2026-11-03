namespace PortailInterne.Api.Dtos;

public enum DayStatus
{
	Working,
	NotWorking,
	Undefined
}

public enum BusyKind
{
	Busy,
	FlexibleBusy,
	Absent
}

public class AvailabilityDay
{
	public DateOnly Day { get; set; }
	public DayStatus Status { get; set; }
	public DateTime? StartAt { get; set; }
	public DateTime? EndAt { get; set; }
}

public class BusySlot
{
	public DateTime StartAt { get; set; }
	public DateTime EndAt { get; set; }
	public BusyKind Kind { get; set; }
}

public class EmployeeAvailability
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public List<AvailabilityDay> Days { get; set; } = [];
	public List<BusySlot> Busy { get; set; } = [];
}
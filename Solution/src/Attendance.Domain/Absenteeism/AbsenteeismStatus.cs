namespace Attendance.Domain.Absenteeism;

public sealed record AbsenteeismStatus(SchoolYear SchoolYear, int Absences, int Threshold, bool IsChronic);

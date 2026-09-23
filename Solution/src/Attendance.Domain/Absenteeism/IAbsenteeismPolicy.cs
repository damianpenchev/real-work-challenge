namespace Attendance.Domain.Absenteeism;

public interface IAbsenteeismPolicy
{
    AbsenteeismStatus Evaluate(SchoolYear schoolYear, int absences, int? schoolThreshold);
}

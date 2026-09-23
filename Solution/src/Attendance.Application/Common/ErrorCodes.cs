namespace Attendance.Application.Common;

public static class ErrorCodes
{
    public const string BatchEmpty = "BATCH_EMPTY";
    public const string BatchTooLarge = "BATCH_TOO_LARGE";
    public const string SchoolNotFound = "SCHOOL_NOT_FOUND";
    public const string FutureDate = "FUTURE_DATE";
    public const string DuplicateStudent = "DUPLICATE_STUDENT_IN_BATCH";
    public const string StudentNotInSchool = "STUDENT_NOT_IN_SCHOOL";
    public const string StudentInactive = "STUDENT_INACTIVE";
    public const string UnknownCode = "UNKNOWN_ATTENDANCE_CODE";
    public const string MinutesLateOutOfRange = "MINUTES_LATE_OUT_OF_RANGE";
    public const string NotesTooLong = "NOTES_TOO_LONG";
    public const string StudentNotFound = "STUDENT_NOT_FOUND";
    public const string UnknownSchoolYear = "UNKNOWN_SCHOOL_YEAR";
}

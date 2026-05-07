namespace HMS.Entities.Exceptions
{
    public abstract class HmsException : Exception
    {
        protected HmsException(string message) : base(message) { }
    }

    public class NotFoundException : HmsException
    {
        public NotFoundException(string entityName, object id)
            : base($"{entityName} with id '{id}' was not found.") { }
        public NotFoundException(string message) : base(message) { }
    }

    public class ForbiddenException : HmsException
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(message) { }
    }

    public class BusinessRuleException : HmsException
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    public class RecordLockedException : HmsException
    {
        public RecordLockedException()
            : base("Patient record is locked. Patient must provide their access code.") { }
    }

    public class DuplicateException : HmsException
    {
        public DuplicateException(string message) : base(message) { }
    }

    public class AuthException : HmsException
    {
        public AuthException(string message) : base(message) { }
    }
}

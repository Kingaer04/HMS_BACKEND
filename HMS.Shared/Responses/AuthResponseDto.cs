namespace HMS.Shared.Responses
{
    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public UserInfoDto? User { get; set; }
        public IEnumerable<string>? Errors { get; set; }
    }

    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? HospitalId { get; set; }
        public string? HospitalName { get; set; }
    }

    public class NhisVerificationResponseDto
    {
        public bool IsValid { get; set; }
        public string? HospitalName { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> Success(T data, string? message = null) =>
            new() { IsSuccess = true, Data = data, Message = message };

        public static ApiResponse<T> Failure(string message, IEnumerable<string>? errors = null) =>
            new() { IsSuccess = false, Message = message, Errors = errors };
    }
}

namespace Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Email { get; }
        string? Rol { get; }
        string? AreaId { get; }
    }
}

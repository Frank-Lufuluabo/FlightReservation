using Grpc.Core;
using IdentityApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityApi.Infrastructure.Repository
{
    public interface IAppUser
    {
        Task<bool> CreateAsync(AppUser user);
        Task<bool> PasswordMatchAsync(AppUser user, string plainPassword);
        Task<AppUser> GetByEmail(string email);
        Task<AppUser> GetById(string userId);
    }

    public class AppUserManagement(UserManager<AppUser> userManager) : IAppUser
    {
        public async Task<bool> CreateAsync(AppUser user)
        {
            var result = await userManager.CreateAsync(user, user.PasswordHash!);
            if (result.Succeeded) return true;
            string error = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new RpcException(new Status(StatusCode.FailedPrecondition, error));
        }

        public async Task<AppUser?> GetByEmail(string email) => await userManager.FindByEmailAsync(email);

        public Task<AppUser?> GetById(string userId) => userManager.FindByIdAsync(userId);

        public async Task<bool> PasswordMatchAsync(AppUser user, string plainPassword)
        {
            bool match = await userManager.CheckPasswordAsync(user, plainPassword);
            return match;
        }
    }}

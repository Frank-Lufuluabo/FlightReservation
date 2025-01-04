using FluentValidation;
using Grpc.Core;
using IdentityApi.Domain;
using IdentityApi.Infrastructure.Repository;
using Mapster;
using MapsterMapper;
using MediatR;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IdentityApi.Features.LoginAccount
{
    public record Request(string Email, string Password);
    public record Response(string NewJwtToken, string RefreshToken);

    public class Validation : AbstractValidator<Request>
    {
        public Validation()
        {
            RuleFor(m => m.Email).NotEmpty().EmailAddress();
            RuleFor(m => m.Password).NotEmpty();
        }
    }

    public record Command(Request Login) : IRequest<Response>;
    internal static class LoginAccountMapperConfig
    {
        public static void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Request, AppUser>()
                .Map(d => d.PasswordHash, s => s.Password);
        }
    }

    public class Handler(IUnitOfWork unitOfWork, IValidator<Request> validator, IMapper mapper) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request.Login, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", errors)));
            }
            var _user = await unitOfWork.AppUser.GetByEmail(request.Login.Email) ??
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid credentials"));
            var result = await unitOfWork.AppUser.PasswordMatchAsync(_user, request.Login.Password);
            if (!result)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid email/password"));

            var claims = await unitOfWork.Claim.GetClaimsAsync(_user);
            string jwtToken = unitOfWork.JwtToken.GenerateToken(claims);
            string refreshToken = unitOfWork.RefreshToken.GenerateToken();
            var userToken = await unitOfWork.RefreshToken.GetRefreshTokenByIdAsync(_user.Id);
            var refreshTokenModel = new RefreshToken
            {
                UserId = _user!.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(12)
            };
            if (userToken == null)
            {
                unitOfWork.RefreshToken.AddToken(refreshTokenModel);
                await unitOfWork.SaveChangesAsync();
                return new Response(jwtToken, refreshToken);
            }
            bool isRefreshTokenValid = await unitOfWork.RefreshToken.IsTokenValid(userToken.Token!);
            if(!isRefreshTokenValid)
            {
                refreshTokenModel.Id = userToken.Id;
                unitOfWork.RefreshToken.UpdateToken(refreshTokenModel);
                await unitOfWork.SaveChangesAsync();
                return new Response(jwtToken, refreshToken);
            }
            return new Response(jwtToken, userToken.Token!);
        }
    }
 }


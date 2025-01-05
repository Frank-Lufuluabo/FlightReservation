using FluentValidation;
using Grpc.Core;
using IdentityApi.Domain;
using IdentityApi.Infrastructure.Repository;
using MediatR;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IdentityApi.Features.GetNewToken
{
    public record Request(string RefreshToken);
    public record Response(string NewJwtToken, string NewRefreshToken);

    public class Validation : AbstractValidator<Request>
    {
        public Validation()
        {
            RuleFor(m => m.RefreshToken).NotEmpty();
        }
    }

    public record Command(Request Request) : IRequest<Response>;

    public class Handler(IUnitOfWork unitOfWork, IValidator<Request> validator) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var error = validationResult.Errors.Select(x => x.ErrorMessage).FirstOrDefault();
                throw new RpcException(new Status(StatusCode.InvalidArgument, error!));
            }

            var _refreshToken = await unitOfWork.RefreshToken.GetRefreshTokenAsync(request.Request.RefreshToken) ??
                 throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid refresh token provided"));

            bool isRefreshTokenValid = await unitOfWork.RefreshToken.IsTokenValid(request.Request.RefreshToken);
            var _user = await unitOfWork.AppUser.GetById(_refreshToken.UserId!);
            var claims = await unitOfWork.Claim.GetClaimsAsync(_user!);
            string jwtToken = unitOfWork.JwtToken.GenerateToken(claims);
            if (!isRefreshTokenValid)
            {
                string newRefreshToken = unitOfWork.RefreshToken.GenerateToken();
                var _newRefreshTokenModel = new RefreshToken
                {
                    Id = _refreshToken.Id,
                    UserId = _user!.Id,
                    Token = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(12)
                };
                unitOfWork.RefreshToken.UpdateToken(_newRefreshTokenModel);
                await unitOfWork.SaveChangesAsync();
                return new Response(jwtToken, newRefreshToken);
            }
            return new Response(jwtToken, _refreshToken.Token);
        }
    }

    internal class GetNewTokenService(ISender sender) : GetNewTokenServiceProto.GetNewTokenProtoBase
    {
        public override async Task<NewTokenResponseProto> GetNewTokenProto(NewTokenRequestProto request, ServerCallContext context)
        {
            var mapRequest = request.Adapt<Request>();
            var result = await sender.Send(new Command(mapRequest));
            var mapResult = result.Adapt<NewTokenResponseProto>();
            return mapResult;
        }
    }
}

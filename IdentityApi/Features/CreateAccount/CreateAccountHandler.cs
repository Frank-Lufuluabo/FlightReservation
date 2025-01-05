﻿using FluentValidation;
using Grpc.Core;
using IdentityApi.Domain;
using IdentityApi.Infrastructure.Repository;
using Mapster;
using MapsterMapper;
using MediatR;
using Shared.Authentication;
using System.Security.Claims;


namespace IdentityApi.Features.CreateAccount
{
    public class Request
    {
        public string? Fullname { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class Validation : AbstractValidator<Request>
    {
        public Validation()
        {
            RuleFor(m => m.Email).NotEmpty().EmailAddress();
            RuleFor(m => m.Password).NotEmpty()
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[@$!%?&])[A-Za-z\d@$!%*?&]{8,}$")
                .WithMessage("Password must contain at least one uppercase letter, one lower case letter, and one special character");
            RuleFor(m => m.ConfirmPassword).NotEmpty().Equal(m => m.Password).WithMessage("Password do not match");
        }
    }
    internal static class CreateAccountMapperConfig
    {
        public static void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Request, AppUser>()
                .Map(d => d.PasswordHash, s => s.Password)
                .Map(d => d.UserName, s => s.Email);
        }
    }

    internal record Command(Request Account) : IRequest<bool>;

    internal class Handler(IUnitOfWork unitOfWork, IValidator<Request> validator, IMapper mapper) : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request.Account, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", errors)));
            }

            var mapData = mapper.From(request.Account).AdaptToType<AppUser>();
            await unitOfWork.AppUser.CreateAsync(mapData);
            var _user = await unitOfWork.AppUser.GetByEmail(mapData.Email!);
            List<Claim> claims =
                [
                   new(ClaimTypes.Role, Roles.User),
                   new(ClaimTypes.Email, _user! .Email!),
                   new(ClaimTypes.Name, _user.Fullname!),
                   new(PolicyNames.Key, PolicyNames.UserPolicy),
                   new(Permissions.CanCreate, false.ToString()),
                   new(Permissions.CanUpdate, false.ToString()),
                   new(Permissions.CanDelete, false.ToString()),
                   new(Permissions.CanUpdate, false.ToString())
                ];
            await unitOfWork.Claim.AssignClaims(_user, claims);
            return true;
        }
    }
    internal class CreateAccountService(ISender sender) : CreateAccountServiceProto.CreateAccountServiceProtoBase
    {
        public override async Task<CreateAccountResponseProto> CreateAccountProto(CreateAccountRequestProto request, ServerCallContext context)
        {
            var mapRequest = request.Adapt<Request>();
            var result = await sender.Send(new Command(mapRequest));
            return new CreateAccountResponseProto { Success = result };
        }
    }
}

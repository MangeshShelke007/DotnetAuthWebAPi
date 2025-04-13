using AutoMapper;
using DotnetAuthWebAPi.Domain.Contract;
using DotnetAuthWebAPi.Domain.Entities;

namespace DotnetAuthWebAPi.Infrastructure.Mapping
{
    public class MappingProfile:Profile
    {
        public MappingProfile() {
            CreateMap<ApplicationUser, UserResponse>();
            CreateMap<ApplicationUser, CurrentUserResponse>();
            CreateMap<UserRegisterRequest, ApplicationUser>();
        }
    }
}

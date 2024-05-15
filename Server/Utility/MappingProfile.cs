using AutoMapper;
using Common.Models.Dtos;
using Server.Models;

namespace Server.Utility;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region User Mapping

        CreateMap<User, UserDto>();

        #endregion
    }
}
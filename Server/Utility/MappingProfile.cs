using AutoMapper;
using Common.Models.Dtos;
using Common.Models.Dtos.DecisionElements;
using Server.Models;
using Server.Models.DecisionElements;

namespace Server.Utility;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region User Mapping

        CreateMap<User, UserDto>();

        #endregion

        #region DecisionElement Mapping

        CreateMap<DecisionMatrix, DecisionMatrixDto>()
           .ForMember(dest => dest.UserEmail, 
                opt => opt.MapFrom(src => src.User.Email));

        #endregion
    }
}
using AutoMapper;
using Common.DataStructures;
using Common.DataStructures.Dtos;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Server.Models;
using Server.Models.DecisionElements;
using Server.Models.DecisionElements.Stats;

namespace Server.Utility;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region User Mapping

        CreateMap<User, UserDto>();

        #endregion

        #region DecisionMatrix Mapping

        CreateMap<DecisionMatrix, DecisionMatrixDto>()
           .ForMember(dest => dest.UserEmail, 
                opt => opt.MapFrom(src => src.User.Email));

        CreateMap<DecisionMatrixDto, DecisionMatrix>();

        CreateMap<DecisionMatrixMetadata, DecisionMatrix>();

        #endregion
        
        #region DecisionMatrixStats Mapping

        CreateMap<DecisionMatrixStats, DecisionMatrixStatsDto>().ReverseMap();

        #endregion
    }
}
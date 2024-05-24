using Common.DataStructures;
using Common.DataStructures.Dtos;
using Common.DataStructures.Dtos.DecisionElements;
using Common.DataStructures.Dtos.DecisionElements.Stats;
using Server.Models;
using Server.Models.DecisionElements;
using Server.Models.DecisionElements.Stats;

namespace Server.Utility;

public static class MappingExtensionMethods
{
    #region User Mapping

    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Guid = user.Guid,
            Email = user.Email,
            SecondaryEmail = user.SecondaryEmail,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    #endregion

    #region Decision Matrix Mapping

    public static DecisionMatrixDto ToDto(this DecisionMatrix matrix)
    {
        return new DecisionMatrixDto
        {
            Name = matrix.Name,
            Guid = matrix.Guid,
            CreationTime = matrix.CreationTime,
            LastUpdated = matrix.LastUpdated,
            UserEmail = matrix.User.Email,
            RowCount = matrix.NumRows,
            ColumnCount = matrix.NumColumns,
            Features = matrix.Features
        };
    }
    
    public static DecisionMatrix ToModel(this DecisionMatrixDto matrixDto)
    {
        return new DecisionMatrix
        {
            Name = matrixDto.Name,
            Guid = matrixDto.Guid,
            CreationTime = matrixDto.CreationTime,
            LastUpdated = matrixDto.LastUpdated,
            NumRows = matrixDto.RowCount,
            NumColumns = matrixDto.ColumnCount,
            Features = matrixDto.Features,
            Filepath = "" // Must be set by the caller
        };
    }

    public static DecisionMatrix ToModel(this DecisionMatrixMetadata matrixMetadata)
    {
        return new DecisionMatrix
        {
            Name = matrixMetadata.Name,
            Guid = matrixMetadata.Guid,
            CreationTime = matrixMetadata.CreationTime,
            LastUpdated = matrixMetadata.LastUpdated,
            NumRows = matrixMetadata.RowCount,
            NumColumns = matrixMetadata.ColumnCount,
            Features = matrixMetadata.Features,
            Filepath = "" // Must be set by the caller
        };
    }

    #endregion

    #region Decision Matrix Stats Mapping

    public static DecisionMatrixStatsDto ToDto(this DecisionMatrixStats stats)
    {
        return new DecisionMatrixStatsDto
        {
            Guid = stats.Guid,
            ElementGuid = stats.Matrix.Guid,
            ParticipantEmail = stats.ParticipantEmail,
            StartTime = stats.StartTime,
            ElapsedMilliseconds = stats.ElapsedMilliseconds,
            RowCount = stats.RowCount,
            ColumnCount = stats.ColumnCount,
            Decision = stats.Decision
        };
    }
    
    public static DecisionMatrixStats ToModel(this DecisionMatrixStatsDto statsDto)
    {
        return new DecisionMatrixStats
        {
            Guid = statsDto.Guid,
            ParticipantEmail = statsDto.ParticipantEmail,
            StartTime = statsDto.StartTime,
            ElapsedMilliseconds = statsDto.ElapsedMilliseconds,
            RowCount = statsDto.RowCount,
            ColumnCount = statsDto.ColumnCount,
            Decision = statsDto.Decision
        };
    }

    #endregion
    
}
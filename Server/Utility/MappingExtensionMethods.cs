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

    /// <summary>
    ///   Converts a UserDto object to a User object.
    /// </summary>
    /// <param name="user">The UserDto object to convert</param>
    /// <returns>The converted User object</returns>
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Guid = user.Guid,
            Email = user.Email,
            SecondaryEmail = user.SecondaryEmail,
            FirstName = user.FirstName,
            LastName = user.LastName,
            LockoutEnd = user.LockoutEnd
        };
    }

    #endregion

    #region Decision Matrix Mapping

    /// <summary>
    ///     Converts a DecisionMatrix object to a DecisionMatrixDto object.
    /// </summary>
    /// <param name="matrix">The DecisionMatrix object to convert</param>
    /// <returns>The converted DecisionMatrixDto object</returns>
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
    
    /// <summary>
    ///     Converts a DecisionMatrix object to a DecisionMatrixDto object. Note: Id, UserId, and Filepath are not set.
    /// </summary>
    /// <param name="matrixDto">The DecisionMatrixDto object to convert</param>
    /// <returns>The converted DecisionMatrix object</returns>
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

    /// <summary>
    ///     Converts a DecisionMatrixMetadata object to a DecisionMatrix object. Note: Id, UserId, and Filepath are not set.
    /// </summary>
    /// <param name="matrixMetadata">The DecisionMatrixMetadata object to convert</param>
    /// <returns>The converted DecisionMatrix object</returns>
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

    /// <summary>
    ///     Converts a DecisionMatrixStats object to a DecisionMatrixStatsDto object.
    /// </summary>
    /// <param name="stats">The DecisionMatrixStats object to convert</param>
    /// <returns>The converted DecisionMatrixStatsDto object</returns>
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
    
    /// <summary>
    ///     Converts a DecisionMatrixStatsDto object to a DecisionMatrixStats object. Note: Id, MatrixId, ParticipantId, and Filepath are not set.
    /// </summary>
    /// <param name="statsDto">The DecisionMatrixStatsDto object to convert</param>
    /// <returns>The converted DecisionMatrixStats object</returns>
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

    /// <summary>
    ///     Converts a DecisionMatrixStatsData object to a DecisionMatrixStats object. Note: Id, MatrixId, ParticipantId, and Filepath are not set.
    /// </summary>
    /// <param name="statsData">The DecisionMatrixStatsData object to convert</param>
    /// <returns>The converted DecisionMatrixStats object</returns>
    public static DecisionMatrixStats ToModel(this DecisionMatrixStatsData statsData)
    {
        return new DecisionMatrixStats
        {
            Guid = statsData.Guid,
            //ElementGuid = statsData.ElementGuid,
            ParticipantEmail = statsData.ParticipantEmail,
            StartTime = statsData.StartTime,
            ElapsedMilliseconds = statsData.ElapsedMilliseconds,
            RowCount = statsData.RowCount,
            ColumnCount = statsData.ColumnCount,
            Decision = statsData.Decision
        };
    }

    #endregion
    
}
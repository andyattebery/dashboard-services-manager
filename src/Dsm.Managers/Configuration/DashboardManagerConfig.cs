using System.ComponentModel.DataAnnotations;
using Dsm.Managers.DashboardManagers;

namespace Dsm.Managers.Configuration;

public sealed class DashboardManagerConfig
{
    [EnumDataType(typeof(DashboardManagerType))]
    public required DashboardManagerType DashboardManagerType { get; set; }
    public required string DashboardConfigFilePath { get; set; }
    public bool EnableStatusMonitoring { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Dsm.Managers.DashboardManagers;

namespace Dsm.Managers.Configuration;
public sealed class ManagerOptions
{
    public required string DashboardConfigFilePath { get; set; }
    [EnumDataType(typeof(DashboardManagerType))]
    public required DashboardManagerType DashboardManagerType { get; set; }
    public List<string> IgnoredServiceNames { get; set; } = new List<string>();
}
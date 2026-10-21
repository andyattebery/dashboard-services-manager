using Dsm.Managers.Configuration;
using Dsm.Managers.Services;
using Dsm.Shared.Models;
using Microsoft.Extensions.Options;

namespace Dsm.Managers;

public class DashboardQueryService
{
    private readonly ManagerOptions _managerOptions;
    private readonly ProviderServiceStore _providerServiceStore;
    private readonly ServiceMerger _serviceMerger;

    public DashboardQueryService(
        IOptions<ManagerOptions> managerOptions,
        ProviderServiceStore providerServiceStore,
        ServiceMerger serviceMerger)
    {
        _managerOptions = managerOptions.Value;
        _providerServiceStore = providerServiceStore;
        _serviceMerger = serviceMerger;
    }

    // What DSM knows: every provider source's copies, merged the way a POST writes them to the
    // dashboards, with icons still unresolved. Hand-written dashboard entries aren't DSM's, so they
    // aren't included.
    public async Task<List<Service>> ListServices()
    {
        await _providerServiceStore.EnsureLoaded();
        return _serviceMerger.Merge(
            _providerServiceStore.Copies,
            _managerOptions.IgnoredServiceNames ?? new List<string>());
    }
}

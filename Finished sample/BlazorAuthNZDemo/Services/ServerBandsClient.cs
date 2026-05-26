using BlazorAuthNZDemo.Client.Models;
using BlazorAuthNZDemo.Client.Services;

namespace BlazorAuthNZDemo.Services;

public class ServerBandsClient(BandsRepository repository) : IBandsClient
{
    public Task<List<Band>> GetBandsAsync()
    {
        return Task.FromResult(repository.GetBands().ToList());
    }
}


using BlazorAuthNZDemo.Client.Models;

namespace BlazorAuthNZDemo.Client.Services;

public interface IBandsClient
{
    Task<List<Band>> GetBandsAsync();
}

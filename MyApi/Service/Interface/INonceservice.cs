using System;

namespace MyApi.Service.Interface;

public interface INonceservice
{
    Task<bool> IsReplayAsync(string nonce);
    Task StoreNonceAsync(string nonce, TimeSpan expiry);
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApi.data;
using MyApi.models.entities;
using MyApi.Repository.Interface;
namespace MyApi.Repository.Implementation;

public class Refreshtokenrepository: Irefreshtokenrepository
{
    private readonly Dbcontext dbcontext;
    private readonly ILogger<Refreshtokenrepository> _logger;

    public Refreshtokenrepository(Dbcontext dbcontext, ILogger<Refreshtokenrepository> logger)
    {
        this.dbcontext = dbcontext;
        _logger = logger;
    }

    public async Task Add(Refreshtoken token)
    {
        _logger.LogInformation("Persisting refresh token for employee (employeeId={EmployeeId}, expires={Expires}).", token.EmployeeId, token.Expires);
        dbcontext.RefreshTokens.Add(token);
        await dbcontext.SaveChangesAsync();
    }

    public async Task<Refreshtoken?> GetToken(string token)
    {
        _logger.LogInformation("Looking up active refresh token in repository.");
        return await dbcontext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked);
    }
}

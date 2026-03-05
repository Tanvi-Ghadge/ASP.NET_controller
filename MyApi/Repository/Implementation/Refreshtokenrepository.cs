using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models.entities;
using MyApi.Repository.Interface;
namespace MyApi.Repository.Implementation;

public class Refreshtokenrepository: Irefreshtokenrepository
{
    private readonly Dbcontext dbcontext;

    public Refreshtokenrepository(Dbcontext dbcontext)
    {
        this.dbcontext = dbcontext;
    }

    public async Task Add(Refreshtoken token)
    {
        dbcontext.RefreshTokens.Add(token);
        await dbcontext.SaveChangesAsync();
    }

    public async Task<Refreshtoken?> GetToken(string token)
    {
        return await dbcontext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked);
    }
}

using System.Data;

namespace FichaCosto.Repositories.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
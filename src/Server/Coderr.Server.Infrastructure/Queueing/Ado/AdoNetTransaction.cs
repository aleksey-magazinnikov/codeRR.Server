using System.Data;

namespace codeRR.Server.Infrastructure.Queueing.Ado
{
    public sealed class AdoNetTransaction : IQueueTransaction
    {
        private readonly IDbConnection _con;

        public AdoNetTransaction(IDbConnection con)
        {
            _con = con;
            Transaction = con.BeginTransaction();
        }

        public IDbTransaction Transaction { get; }

        public void Dispose()
        {
            Transaction.Dispose();
            _con.Dispose();
        }

        public void Rollback()
        {
            Transaction.Rollback();
        }

        public void Commit()
        {
            Transaction.Commit();
        }
    }
}
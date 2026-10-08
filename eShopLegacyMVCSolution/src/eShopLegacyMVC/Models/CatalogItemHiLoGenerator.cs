using Microsoft.EntityFrameworkCore;
using System.Data;

namespace eShopLegacyMVC.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int sequenceId = -1;
        private int remainningLoIds = 0;
        private readonly object sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (sequenceLock)
            {
                if (remainningLoIds == 0)
                {
                    sequenceId = GetNextHiValue(db);
                    remainningLoIds = HiLoIncrement - 1;
                    return sequenceId;
                }
                else
                {
                    remainningLoIds--;
                    return ++sequenceId;
                }
            }
        }

        private static int GetNextHiValue(CatalogDBContext db)
        {
            var conn = db.Database.GetDbConnection();
            bool wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) conn.Open();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT NEXT VALUE FOR catalog_hilo";
                var result = cmd.ExecuteScalar();
                return (int)(long)result!;
            }
            finally
            {
                if (!wasOpen) conn.Close();
            }
        }
    }
}

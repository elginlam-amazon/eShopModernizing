using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace eShopLegacyMVC.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int sequenceId = -1;
        private int remainningLoIds = 0;
        private object sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (sequenceLock)
            {
                if (remainningLoIds == 0)
                {
                    // NEXT VALUE FOR cannot be composed into a sub-query, so the result is materialized client-side
                    var rawQuery = db.Database.SqlQueryRaw<Int64>("SELECT NEXT VALUE FOR catalog_hilo AS Value;");
                    sequenceId = (int)rawQuery.AsEnumerable().Single();
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
    }
}

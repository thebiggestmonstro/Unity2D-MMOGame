using Server.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Utils
{
    public static class Extensions
    {
        public static bool SaveChangesEx(this AppDbContext db)
        {
            try
            { 
                db.SaveChanges();
                return true;
            }
            catch 
            {
                return false;
            }
        }
    }
}

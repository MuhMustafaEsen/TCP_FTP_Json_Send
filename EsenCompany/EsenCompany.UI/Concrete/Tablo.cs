using EsenCompany.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EsenCompany.UI.Concrete
{
    public class Tablo
    {
        public static List<KontrolClass> tablo;
        public static void TabloDoldur()
        {
            Random rnd = new Random();
            int sayi = rnd.Next(4, 9);
            for (int i = 0; i < sayi; i++)
            {
                KontrolClass kontrolClass = new KontrolClass() { ID = Guid.NewGuid().ToString(), Deger = Convert.ToBoolean(rnd.Next(2)) };
                tablo.Add(kontrolClass);
            }
        }
    }
}

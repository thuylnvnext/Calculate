using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlTypes;

namespace MyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //decimal xx = 31;
            //double inttime = 0;
            //if(xx % 30 == 0)
            //    inttime = (int)((decimal)(xx) / 30);
            //else
            //    inttime = ((int)((decimal)(xx) / 30)) + 1;

            //decimal x = Math.Round((decimal)(inttime * 30 / 60), 1);

            //decimal xx = 0x5a0;

            //Decimal XX = CalculateMonthWork.EvaluateMonthWork(
            //        "NPTV",
            //        324,
            //        Convert.ToDateTime("11/17/2012"),
            //        Convert.ToDateTime("11/17/2012"),
            //        false,
            //        519
            //    );
            SqlDecimal result = 0;
            SqlString text = "";
            CalculateDayWork.EvaluateDayWork(
                    "OT195",
                    153,
                    Convert.ToDateTime("4/18/2013 12:00:00 AM"),
                    "C2.1", //ShiftID hoặc AbsenceID
                    Convert.ToDateTime("4/18/2013 15:37:16 PM"),
                    Convert.ToDateTime("4/19/2013 02:11:30 AM"),
                    "",
                    0,
                    true,
                    1320,
                    360,
                    519,
                    out result,
                    out text
                );
            Decimal X = 1M;
        }
    }
}

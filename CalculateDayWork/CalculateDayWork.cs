using Microsoft.SqlServer.Server;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.InteropServices;

public class CalculateDayWork
{
    private static string absenceID;
    private static DataTable attendanceItem;
    private char ch;
    private char chrValue;
    private const string CONNECTIONSTRING = "context connection=true";
    private static SqlConnection conn;
    //private const string CONNECTIONSTRING = @"data source=222.255.29.210;user id=sa;password=admin@123`;initial catalog=hrm_shg_ais;Persist Security Info=True";
    //private static SqlConnection conn = new SqlConnection(CONNECTIONSTRING);
    private static int employeeID;
    private string expression;
    private static Func[] funcTb = new Func[NUMBEROFFUNCTIONS];
    private string identLexeme;
    private int index = 0;
    private bool isExpression = true;
    private Token lookAhead;
    private bool negate;
    private static int nightFrom = 1320; //22h
    private static int nightTo = 360; //6h
    private bool not;
    private const int NUMBEROFFUNCTIONS = 26;
    private decimal numValue;
    private string result;
    private bool retract;
    private DataRow sdr;
    private static DataTable shiftDetail;
    private static string shiftID;
    private int sPID;
    private static string text;
    private static DateTime timeIn;
    private static DateTime timeOut;
    private static decimal unit;
    private static DateTime workingDay;

    private static int plusNightFrom = 1320; //22h
    private static int plusNightTo = 1440; //0h

    public CalculateDayWork(string expression, int sPID)
    {
        this.expression = expression;
        this.ch = ' ';
        this.sPID = sPID;
    }

    private void AddOperator()
    {
        this.NextToken();
    }

    private static void ConvertToMinute(string shiftID, int sPID)
    {
        SqlCommand selectCommand = new SqlCommand("ShiftDetail_ConvertToMinute", conn) {
            CommandType = CommandType.StoredProcedure
        };
        selectCommand.Parameters.Add("@ShiftID", SqlDbType.VarChar, 10).Value = shiftID;
        selectCommand.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        selectCommand.Parameters.Add("@SPID", SqlDbType.Int, 4).Value = sPID;
        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
        if (shiftDetail == null)
        {
            shiftDetail = new DataTable();
        }
        adapter.Fill(shiftDetail);
        if (shiftDetail.PrimaryKey.Length == 0)
        {
            shiftDetail.PrimaryKey = new DataColumn[] { shiftDetail.Columns["SPID"], shiftDetail.Columns["ShiftID"] };
        }
    }

    private DataTable GetDataBreakOutBreakIn()
    {
        DataTable tbl = new DataTable();
        SqlCommand selectCommand = new SqlCommand("BreakOutBreakIn_GetByByEmployeeID", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        selectCommand.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        selectCommand.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
        adapter.Fill(tbl);
        return tbl;
    }

    private decimal DayWork()
    {
        decimal num = 0M;
        decimal num10 = 480M;
        decimal num3 = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
        decimal num7 = ((0x5a0 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute;
        this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
        if (this.sdr != null)
        {
            decimal num11;
            decimal num12;
            decimal num13;
            int num14;
            decimal num2 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);
            decimal num4 = Convert.ToDecimal(this.sdr["MinuteBreakOut"]);
            decimal num5 = Convert.ToDecimal(this.sdr["MinuteBreakIn"]);
            decimal num6 = Convert.ToDecimal(this.sdr["MinuteTimeOut"]);
            decimal num8 = Convert.ToDecimal(this.sdr["LateComing"]);
            decimal num9 = Convert.ToDecimal(this.sdr["EarlyReturning"]);
            num10 = Convert.ToDecimal(this.sdr["Total"]);
            if (num3 < (num2 + num8))
            {
                num11 = num2;
            }
            else
            {
                num11 = num3;
            }
            if ((num11 > num4) && (num11 < num5))
            {
                num11 = num5;
            }
            else
            {
                num14 = ((int) (num11 - num2)) % 60;
                if ((num14 > 0) && (num14 < 30))
                {
                    num11 = (num11 - num14) + 30M;
                }
                else if ((num14 >= 30) && (num14 < 60))
                {
                    num11 = (num11 - num14) + 60M;
                }
            }
            if (num7 > (num6 - num9))
            {
                num12 = num6;
            }
            else
            {
                num12 = num7;
            }
            if ((num12 > num4) && (num12 < num5))
            {
                num12 = num4;
            }
            else
            {
                num14 = ((int) (num6 - num12)) % 60;
                if ((num14 > 0) && (num14 < 30))
                {
                    num11 -= num14;
                }
                else if ((num14 >= 30) && (num14 < 60))
                {
                    num11 = (num11 - num14) + 30M;
                }
            }
            if ((num11 > num4) || (num12 < num5))
            {
                num13 = 0M;
            }
            else
            {
                num13 = num5 - num4;
            }
            if (num12 <= 1320M)
            {
                num = (num12 - num11) - num13;
            }
            else if ((num12 > 1320M) && (num12 <= 1800M))
            {
                if ((num4 < 1320M) && (num5 < 1320M))
                {
                    num = (1320M - ((num11 < 1320M) ? num11 : 1320M)) - num13;
                }
                else if ((num4 < 1320M) && (num5 > 1320M))
                {
                    num = (1320M - ((num11 < 1320M) ? num11 : 1320M)) - ((num13 != 0M) ? (1320M - num4) : 0M);
                }
                else
                {
                    num = 1320M - ((num11 < 1320M) ? num11 : 1320M);
                }
            }
            else if (num11 < 1320M)
            {
                if ((num4 < 1320M) && (num5 < 1320M))
                {
                    num = (1320M - num11) - num13;
                }
                else if ((num4 < 1320M) && (num5 > 1320M))
                {
                    num = (1320M - num11) - ((num13 != 0M) ? (1320M - num4) : 0M);
                }
                else
                {
                    num = 1320M - num11;
                }
                if ((num4 < 1800M) && (num5 < 1800M))
                {
                    num += num12 - 1800M;
                }
                else if ((num4 < 1800M) && (num5 > 1800M))
                {
                    num += (num12 - 1800M) - ((num13 != 0M) ? (num5 - 1800M) : 0M);
                }
                else
                {
                    num += num12 - num13;
                }
            }
            else if ((num4 < 1800M) && (num5 < 1800M))
            {
                num = num12 - 1800M;
            }
            else if ((num4 < 1800M) && (num5 > 1800M))
            {
                num = (num12 - 1800M) - ((num13 != 0M) ? (num5 - 1800M) : 0M);
            }
            else
            {
                num = num12 - num13;
            }
        }
        if (num >= num10)
        {
            text = num10.ToString();
        }
        else if (num > 0M)
        {
            text = num.ToString();
        }
        return (num / num10);
    }

    private decimal MinBreakOutBreakIn()
    {
        //Ngày lễ, chủ nhật, thứ bẩy nghỉ luân phiên không tính
        decimal whatDay = this.WhatIsDay();
        //0: Ngày lễ, 1: Chủ nhật
        //if (whatDay == 0 || whatDay == 1)
        //    return 0;
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return 0;

        sdr = shiftDetail.Rows.Find(new object[] { sPID, shiftID });
        if (sdr != null)
        {
            decimal minuteShiftTimeIn, minuteShiftBreakOut, minuteShiftBreakIn, minuteShiftTimeOut;
            minuteShiftTimeIn = Convert.ToDecimal(sdr["MinuteTimeIn"]); //Giờ vào
            minuteShiftBreakOut = Convert.ToDecimal(sdr["MinuteBreakOut"]); //Giờ ra giữa giờ
            minuteShiftBreakIn = Convert.ToDecimal(sdr["MinuteBreakIn"]); //Giờ vào giữa giờ
            minuteShiftTimeOut = Convert.ToDecimal(sdr["MinuteTimeOut"]); //Giờ ra

            //Lấy dữ liệu
            DataTable tbl = GetDataBreakOutBreakIn();
            if (tbl != null)
            {
                if (tbl.Rows.Count > 0)
                {
                    decimal minuteBreakOutIn = 0; 
                    foreach (DataRow row in tbl.Rows)
                    {
                        decimal minuteBreakOut, minuteBreakIn;
                        minuteBreakOut = Convert.ToDecimal(row["BreakOut"]); //Giờ vào
                        minuteBreakIn = Convert.ToDecimal(row["BreakIn"]); //Giờ ra giữa giờ

                        if (minuteShiftBreakOut > 0 && minuteShiftBreakIn > 0)
                        {
                            //Ca có khoảng thời gian giữa giờ
                            if (minuteBreakOut < minuteShiftBreakOut)
                            {
                                if (minuteBreakIn < minuteShiftBreakIn)
                                    minuteBreakOutIn += (minuteBreakIn > minuteShiftBreakOut ? minuteShiftBreakOut : minuteBreakIn) - minuteBreakOut;
                                else
                                    //2 đoạn
                                    minuteBreakOutIn += (minuteShiftBreakOut - minuteBreakOut) + ((minuteBreakIn > minuteShiftTimeOut ? minuteShiftTimeOut : minuteBreakIn) - minuteShiftBreakIn);
                            }
                            else if (minuteBreakOut >= minuteShiftBreakOut && minuteBreakOut < minuteShiftTimeOut)
                            {
                                if (minuteBreakIn > minuteShiftBreakIn)
                                    minuteBreakOutIn += (minuteBreakIn > minuteShiftTimeOut ? minuteShiftTimeOut : minuteBreakIn) - (minuteBreakOut > minuteShiftBreakIn ? minuteBreakOut : minuteShiftBreakIn);
                            }
                        }
                        else
                        { 
                            //Ca không có khoảng thời gian giữa giờ
                            minuteBreakOutIn += (minuteBreakIn > minuteShiftTimeOut ? minuteShiftTimeOut : minuteBreakIn) - (minuteBreakOut > minuteShiftTimeIn ? minuteBreakOut : minuteShiftTimeIn);
                        }
                    }

                    if (minuteBreakOutIn <= 0)
                        return 0M;

                    double inttime = 0;
                    if (minuteBreakOutIn % 30 == 0)
                        inttime = (int)((decimal)(minuteBreakOutIn) / 30);
                    else
                        inttime = ((int)((decimal)(minuteBreakOutIn) / 30)) + 1;

                    return Math.Round((decimal)(inttime * 30 / 60), 1);
                }
            }
        }
        return 0M;
    }

    private decimal EarlyReturning()
    {
        //Ngày lễ, chủ nhật, thứ bẩy nghỉ luân phiên không tính
        decimal whatDay = this.WhatIsDay();
        //0: Ngày lễ, 1: Chủ nhật
        if (whatDay == 0 || whatDay == 1)
            return 0;
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return 0;

        decimal result = 0, absenceHalf = IsAbsenceHalf(), minuteTimeOut, minuteShiftTimeOut, minuteShiftBreakOut, minuteEarlyReturning;
        sdr = shiftDetail.Rows.Find(new object[] { sPID, shiftID });
        if (sdr != null)
        {
            minuteTimeOut = GetTimeOut();
            minuteShiftBreakOut = Convert.ToDecimal(sdr["MinuteBreakOut"]);
            minuteShiftTimeOut = Convert.ToDecimal(sdr["MinuteTimeOut"]);
            minuteEarlyReturning = Convert.ToDecimal(sdr["EarlyReturning"]);
            if (absenceHalf == 1 || absenceHalf == 0m || absenceHalf == 0.5m)
            {
                if (minuteTimeOut + minuteEarlyReturning < minuteShiftTimeOut) result = minuteShiftTimeOut - minuteTimeOut;
            }
            else
            {
                if (minuteTimeOut + minuteEarlyReturning < minuteShiftBreakOut) result = minuteShiftBreakOut - minuteTimeOut;
            }
        }

        double inttime = 0;
        if (result % 30 == 0)
            inttime = (int)((decimal)(result) / 30);
        else
            inttime = ((int)((decimal)(result) / 30)) + 1;

        return Math.Round((decimal)(inttime * 30 / 60), 1);
    }

    public decimal Eval()
    {
        this.NextToken();
        this.Expression();
        if ((this.lookAhead == Token.Err) || (this.lookAhead != Token.Null))
        {
            this.isExpression = false;
        }
        return (this.isExpression ? Convert.ToDecimal(this.result) : 0M);
    }

    private object EvaluateAttendanceItemID(string formula)
    {
        this.identLexeme = string.Empty;
        if (conn == null)
        {
            conn = new SqlConnection("context connection=true");
            conn.Open();
        }
        if (this.IsNumeric(formula))
        {
            return formula;
        }
        CalculateDayWork work = new CalculateDayWork(formula, this.sPID);
        return work.Eval();
    }

    [SqlProcedure]
    public static void EvaluateDayWork(string formula, int _employeeID, DateTime _workingDay, string _shiftID, DateTime _timeIn, DateTime _timeOut, string _absenceID, decimal _unit, bool convertToMinute, int _nightFrom, int _nightTo, int sPID, out SqlDecimal result, out SqlString _text)
    {
        if (conn == null)
        {
            conn = new SqlConnection("context connection=true");
            conn.Open();
        }
        if (conn.State == ConnectionState.Closed)
        {
            conn.Open();
        }
        employeeID = _employeeID;
        workingDay = _workingDay;
        shiftID = _shiftID;
        timeIn = _timeIn;
        timeOut = _timeOut;
        absenceID = _absenceID;
        unit = _unit;
        nightFrom = _nightFrom;
        nightTo = _nightTo;
        if (funcTb[0].erep == Token.Ident)
        {
            InitFunction();
        }
        if (attendanceItem == null)
        {
            InitAttendanceItem();
        }
        if (convertToMinute)
        {
            ConvertToMinute(shiftID, sPID);
        }
        result = 0L;
        _text = string.Empty;
        text = string.Empty;
        result = new CalculateDayWork(formula, sPID).Eval();
        if (text == string.Empty)
        {
            _text = result.ToString();
        }
        else
        {
            _text = text;
        }
    }

    private void Expression()
    {
        Operator nULL = Operator.NULL;
        this.Term();
        string result = this.result;
        while ((((this.lookAhead == Token.Plus) || (this.lookAhead == Token.Minus)) || (this.lookAhead == Token.And)) || (this.lookAhead == Token.Or))
        {
            if (this.lookAhead == Token.Plus)
            {
                nULL = Operator.PLUS;
            }
            else if (this.lookAhead == Token.Minus)
            {
                nULL = Operator.MINUS;
            }
            else if (this.lookAhead == Token.And)
            {
                nULL = Operator.AND;
            }
            else
            {
                nULL = Operator.OR;
            }
            this.AddOperator();
            this.Term();
            switch (nULL)
            {
                case Operator.PLUS:
                    if (this.IsNumeric(result) && this.IsNumeric(this.result))
                    {
                        decimal num = Convert.ToDecimal(result) + Convert.ToDecimal(this.result);
                        result = num.ToString();
                    }
                    else
                    {
                        result = result + this.result;
                    }
                    break;

                case Operator.MINUS:
                    result = (Convert.ToDecimal(result) - Convert.ToDecimal(this.result)).ToString();
                    break;

                case Operator.AND:
                    if (this.IsNumeric(result))
                    {
                        result = ((Convert.ToDecimal(result) == 1M) && (Convert.ToDecimal(this.result) == 1M)) ? "1" : "0";
                    }
                    else
                    {
                        result = ((result == "1") && (this.result == "1")) ? "1" : "0";
                    }
                    break;

                default:
                    if (this.IsNumeric(result))
                    {
                        result = ((Convert.ToDecimal(result) == 1M) || (Convert.ToDecimal(this.result) == 1M)) ? "1" : "0";
                    }
                    else
                    {
                        result = ((result == "1") || (this.result == "1")) ? "1" : "0";
                    }
                    break;
            }
        }
        this.result = result;
        if (((((this.lookAhead == Token.Equal) || (this.lookAhead == Token.NotEqual)) || ((this.lookAhead == Token.GreaterThan) || (this.lookAhead == Token.GreaterThanOrEqual))) || (this.lookAhead == Token.LessThan)) || (this.lookAhead == Token.LessThanOrEqual))
        {
            string s = this.result;
            switch (this.lookAhead)
            {
                case Token.Equal:
                    nULL = Operator.EQUAL;
                    break;

                case Token.NotEqual:
                    nULL = Operator.NOTEQUAL;
                    break;

                case Token.GreaterThan:
                    nULL = Operator.GREATERTHAN;
                    break;

                case Token.GreaterThanOrEqual:
                    nULL = Operator.GREATERTHANOREQUAL;
                    break;

                case Token.LessThan:
                    nULL = Operator.LESSTHAN;
                    break;

                case Token.LessThanOrEqual:
                    nULL = Operator.LESSTHANOREQUAL;
                    break;
            }
            this.NextToken();
            this.Expression();
            switch (nULL)
            {
                case Operator.EQUAL:
                    if (!this.IsNumeric(s) || !this.IsNumeric(this.result))
                    {
                        this.result = (s == this.result) ? "1" : "0";
                        return;
                    }
                    this.result = (Convert.ToDecimal(s) == Convert.ToDecimal(this.result)) ? "1" : "0";
                    return;

                case Operator.NOTEQUAL:
                    this.result = (s != this.result) ? "1" : "0";
                    return;

                case Operator.GREATERTHAN:
                    this.result = (Convert.ToDecimal(s) > Convert.ToDecimal(this.result)) ? "1" : "0";
                    return;

                case Operator.GREATERTHANOREQUAL:
                    this.result = (Convert.ToDecimal(s) >= Convert.ToDecimal(this.result)) ? "1" : "0";
                    return;

                case Operator.LESSTHAN:
                    this.result = (Convert.ToDecimal(s) < Convert.ToDecimal(this.result)) ? "1" : "0";
                    return;

                case Operator.LESSTHANOREQUAL:
                    this.result = (Convert.ToDecimal(s) <= Convert.ToDecimal(this.result)) ? "1" : "0";
                    return;
            }
        }
    }

    private void Factor()
    {
        bool negate = false;
        bool not = false;
        if ((this.lookAhead == Token.Negate) || (this.lookAhead == Token.Not))
        {
            this.UnaryOperator();
            negate = this.negate;
            not = this.not;
        }
        this.Operand();
        if (negate)
        {
            this.result = (-Convert.ToDecimal(result)).ToString();
        }
        if (not)
        {
            this.result = (Convert.ToDecimal(this.result) != 0M) ? "0" : "1";
        }
    }

    private string GetFormluaFromAttendanceItemID(string attendanceItemID)
    {
        string str = string.Empty;
        DataRow row = attendanceItem.Rows.Find(attendanceItemID);
        if (row != null)
        {
            str = row["Value"].ToString();
        }
        return str;
    }

    private decimal GetLateEarly(byte type)
    {
        SqlCommand command = new SqlCommand("RegisterLateEarly_GetValueByEmployeeID", conn) {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        command.Parameters.Add("@Type", SqlDbType.TinyInt, 1).Value = type;
        return Convert.ToDecimal(command.ExecuteScalar());
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private decimal GetMinuteBreakIn()
    {
        SqlCommand command = new SqlCommand("BreakOutBreakIn_GetBreakInForLateComing", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private decimal GetTimeIn()
    {
        return (((1440 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute);
    }

    private decimal GetTimeOut()
    {
        return (((1440 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute);
    }

    private decimal GetValueByAbsenceID()
    {
        decimal unit = 0M;
        if (this.identLexeme == absenceID)
        {
            unit = CalculateDayWork.unit;
        }
        return unit;
    }

    private void Identifier()
    {
        this.identLexeme = string.Empty;
        do
        {
            this.identLexeme = this.identLexeme + this.ch.ToString();
            this.NextCharacter();
        }
        while (char.IsLetter(this.ch) || char.IsDigit(this.ch));
        this.lookAhead = this.LookUpFunction(this.identLexeme);
        if (this.lookAhead == Token.Ident)
        {
            this.lookAhead = this.LookUpOperator(this.identLexeme);
            if (this.lookAhead == Token.Ident)
            {
                this.lookAhead = Token.AttendanceItemID;
            }
        }
        this.retract = true;
    }

    private static void InitAttendanceItem()
    {
        SqlDataAdapter adapter = new SqlDataAdapter(new SqlCommand("AttendanceItem_GetContent", conn) { CommandType = CommandType.StoredProcedure });
        attendanceItem = new DataTable();
        adapter.Fill(attendanceItem);
        attendanceItem.PrimaryKey = new DataColumn[] { attendanceItem.Columns["ID"] };
    }

    private static void InitFunction()
    {
        funcTb[0].erep = Token.Abs;
        funcTb[0].irep = "ABS";
        funcTb[1].erep = Token.Sqrt;
        funcTb[1].irep = "SQRT";
        funcTb[2].erep = Token.LuaChon;
        funcTb[2].irep = "LUACHON";
        funcTb[3].erep = Token.LamTron;
        funcTb[3].irep = "LAMTRON";
        funcTb[4].erep = Token.LaNgay;
        funcTb[4].irep = "LANGAY";
        funcTb[5].erep = Token.GioVao;
        funcTb[5].irep = "GIOVAO";
        funcTb[6].erep = Token.GioRa;
        funcTb[6].irep = "GIORA";
        funcTb[7].erep = Token.NgayNghi;
        funcTb[7].irep = "NGAYNGHI";
        funcTb[8].erep = Token.CaLamViec;
        funcTb[8].irep = "CALAMVIEC";
        funcTb[9].erep = Token.NghiNuaNgay;
        funcTb[9].irep = "NGHINUANGAY";
        funcTb[10].erep = Token.CongNgay;
        funcTb[10].irep = "CONGNGAY";
        funcTb[11].erep = Token.CongDem;
        funcTb[11].irep = "CONGDEM";
        funcTb[12].erep = Token.LamThem;
        funcTb[12].irep = "LAMTHEM";
        funcTb[13].erep = Token.DiMuon;
        funcTb[13].irep = "DIMUON";
        funcTb[14].erep = Token.VeSom;
        funcTb[14].irep = "VESOM";
        funcTb[15].erep = Token.NgayCong;
        funcTb[15].irep = "NGAYCONG";
        funcTb[16].erep = Token.LaNhanVien;
        funcTb[16].irep = "LANHANVIEN";
        funcTb[17].erep = Token.NghiVaLam;
        funcTb[17].irep = "NGHIVALAM";
        funcTb[18].erep = Token.SoLanDiMuon;
        funcTb[18].irep = "SOLANDIMUON";
        funcTb[19].erep = Token.SoLanKDT;
        funcTb[19].irep = "SOLANKDT";
        funcTb[20].erep = Token.SoGioPhuCapDem;
        funcTb[20].irep = "SOGIOPHUCAPDEM";
        funcTb[21].erep = Token.SoGio;
        funcTb[21].irep = "SOGIO";
        funcTb[22].erep = Token.LamThuBay;
        funcTb[22].irep = "LAMTHUBAY";
        funcTb[23].erep = Token.KiemTraDiLam;
        funcTb[23].irep = "KIEMTRADILAM";
        funcTb[24].erep = Token.RaNgoai;
        funcTb[24].irep = "RANGOAI";
        funcTb[25].erep = Token.PhuCap;
        funcTb[25].irep = "PHUCAP"; 
    }

    private decimal IsAbsenceHalf()
    {
        SqlCommand command = new SqlCommand("MonthAttendance_IsAbsenceHalf", conn) {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        return Convert.ToDecimal(command.ExecuteScalar());
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private decimal GetSoLanDT()
    {
        SqlCommand command = new SqlCommand("RawData_CountByEmployeeIDAndWorkingDay", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public bool IsNumeric(string s)
    {
        decimal num;
        return decimal.TryParse(Convert.ToString(s), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out num);
    }

    private decimal LateComing()
    {
        //Ngày lễ, chủ nhật, thứ bẩy nghỉ luân phiên không tính
        decimal whatDay = this.WhatIsDay();
        //0: Ngày lễ, 1: Chủ nhật
        if (whatDay == 0 || whatDay == 1)
            return 0;
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return 0;

        decimal result = 0, absenceHalf = IsAbsenceHalf(), minuteTimeIn, minuteShiftTimeIn, minuteShiftBreakIn, minuteShiftTimeOut;
        sdr = shiftDetail.Rows.Find(new object[] { sPID, shiftID });
        if (sdr != null)
        {
            minuteTimeIn = GetTimeIn();
            minuteShiftTimeIn = Convert.ToDecimal(sdr["MinuteTimeIn"]); // Giờ vào của ca
            minuteShiftBreakIn = Convert.ToDecimal(sdr["MinuteBreakIn"]);
            minuteShiftTimeOut = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra của ca
            if (minuteTimeIn > minuteShiftTimeIn && minuteTimeIn > minuteShiftTimeOut)
                return 0M;
            if (absenceHalf == 1 || absenceHalf == 0m || absenceHalf == -0.5m)
            {
                if (minuteTimeIn > minuteShiftTimeIn) 
                    result = minuteTimeIn - minuteShiftTimeIn;
            }
            else
            {
                if (minuteTimeIn > minuteShiftBreakIn) 
                    result = minuteTimeIn - minuteShiftBreakIn;
            }
        }
        double inttime = 0;
        if (result % 30 == 0)
            inttime = (int)((decimal)(result) / 30);
        else
            inttime = ((int)((decimal)(result) / 30)) + 1;

        return Math.Round((decimal)(inttime * 30 / 60), 1);
    }

    private Token LookUpAttendanceItemID(string identLexeme)
    {
        return ((attendanceItem.Rows.Find(identLexeme) != null) ? Token.AttendanceItemID : Token.Ident);
    }

    private Token LookUpFunction(string identLexeme)
    {
        for (int i = 0; i < NUMBEROFFUNCTIONS; i++)
        {
            if (string.Compare(identLexeme, funcTb[i].irep, true) == 0)
            {
                return funcTb[i].erep;
            }
        }
        return Token.Ident;
    }

    private Token LookUpLetter(string indentLexeme)
    {
        if (indentLexeme.Length == 1)
        {
            this.chrValue = Convert.ToChar(this.identLexeme);
            return Token.Char;
        }
        return Token.Ident;
    }

    private Token LookUpOperator(string indentLexeme)
    {
        switch (indentLexeme.ToUpper())
        {
            case "PHUDINH":
                return Token.Not;

            case "VA":
                return Token.And;

            case "HOAC":
                return Token.Or;
        }
        return Token.Ident;
    }

    private void MultiplyOperator()
    {
        if (this.lookAhead == Token.Exponent)
        {
            this.NextToken();
        }
        else if (this.lookAhead == Token.Times)
        {
            this.NextToken();
        }
        else if (this.lookAhead == Token.Division)
        {
            this.NextToken();
        }
        else if (this.lookAhead == Token.Modulus)
        {
            this.NextToken();
        }
    }

    private void NextCharacter()
    {
        if (this.index < this.expression.Length)
        {
            this.ch = Convert.ToChar(this.expression.Substring(this.index++, 1));
        }
        else
        {
            this.ch = '\0';
        }
    }

    private void NextToken()
    {
        this.retract = false;
        while (char.IsWhiteSpace(this.ch))
        {
            this.NextCharacter();
        }
        switch (this.ch)
        {
            case '^':
                this.lookAhead = Token.Exponent;
                break;

            case '|':
                this.lookAhead = Token.Modulus;
                break;

            case '\'':
                this.String();
                break;

            case '(':
                this.lookAhead = Token.Open;
                break;

            case ')':
                this.lookAhead = Token.Close;
                break;

            case '*':
                this.lookAhead = Token.Times;
                break;

            case '+':
                this.lookAhead = Token.Plus;
                break;

            case ',':
                this.lookAhead = Token.Comma;
                break;

            case '-':
                if ((this.index - 1) != 0)
                {
                    if (Convert.ToChar(this.expression.Substring(this.index - 2, 1)) == '(')
                    {
                        this.lookAhead = Token.Negate;
                    }
                    else
                    {
                        this.lookAhead = Token.Minus;
                    }
                }
                else
                {
                    this.lookAhead = Token.Negate;
                }
                break;

            case '/':
                this.lookAhead = Token.Division;
                break;

            case '<':
                this.NextCharacter();
                if (this.ch != '>')
                {
                    if (this.ch == '=')
                    {
                        this.lookAhead = Token.LessThanOrEqual;
                    }
                    else
                    {
                        this.lookAhead = Token.LessThan;
                        this.retract = true;
                    }
                }
                else
                {
                    this.lookAhead = Token.NotEqual;
                }
                break;

            case '=':
                this.lookAhead = Token.Equal;
                break;

            case '>':
                this.NextCharacter();
                if (this.ch != '=')
                {
                    this.lookAhead = Token.GreaterThan;
                    this.retract = true;
                }
                else
                {
                    this.lookAhead = Token.GreaterThanOrEqual;
                }
                break;

            case '\0':
                this.lookAhead = Token.Null;
                return;

            default:
                if (char.IsLetter(this.ch))
                {
                    this.Identifier();
                }
                else if (char.IsDigit(this.ch))
                {
                    this.Number();
                }
                break;
        }
        if (!this.retract)
        {
            this.NextCharacter();
        }
    }
    /// <summary>
    /// Kiểm tra đi muộn
    /// </summary>
    /// <returns></returns>
    private bool LaDiMuon()
    {
        decimal numIn = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
        this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });

        if (this.sdr != null)
        {
            decimal numMinuteTimeIn = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);
            decimal numMinuteBreakIn = Convert.ToDecimal(this.sdr["MinuteBreakIn"]);
            decimal numLateComing = Convert.ToDecimal(this.sdr["LateComing"]);

            //Đi muộn 5 phút
            if ((numIn + numLateComing) - numMinuteTimeIn > 5M)
                return true;

            //Đi muộn nghỉ giữa ca
            decimal numBreakIn = GetMinuteBreakIn();
            if (numBreakIn > 0M)
            {
                if ((numBreakIn + numLateComing) - numMinuteBreakIn > 5M)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Đếm đi muộn
    /// </summary>
    /// <returns></returns>
    private decimal SoLanDiMuon()
    {
        Decimal numRegisterAbsence = 0M;
        bool outCheck = false;
        numRegisterAbsence = GetNghiVaLam(out outCheck);

        decimal numIn = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
        this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });

        decimal numFirst = 0M;
        decimal numMid = 0M;

        if (this.sdr != null)
        {
            decimal numMinuteTimeIn = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);
            decimal numMinuteBreakIn = Convert.ToDecimal(this.sdr["MinuteBreakIn"]);
            decimal numLateComing = Convert.ToDecimal(this.sdr["LateComing"]);

            //Đăng ký nghỉ buổi sáng
            if (numRegisterAbsence == 0.5M)
                numMinuteTimeIn = numMinuteBreakIn;

            //Đi muộn 5 phút
            if ((numIn + numLateComing) - numMinuteTimeIn > 0M && (numIn + numLateComing) - numMinuteTimeIn <= 5M)
                numFirst = 1M;

            //Đi muộn nghỉ giữa ca
            decimal numBreakIn = GetMinuteBreakIn();
            if (numBreakIn > 0M)
            {
                if ((numBreakIn + numLateComing) - numMinuteBreakIn > 0M && (numBreakIn + numLateComing) - numMinuteBreakIn <= 5M)
                    numMid = 1M;
            }
        }

        return numFirst + numMid;
    }
    /// <summary>
    /// Đếm không quẹt thẻ
    /// </summary>
    /// <returns></returns>
    private decimal SoLanKDT()
    {
        decimal num = GetSoLanDT();
        if(num == 1)
            return 1M;
        else
            return 0M;
    }

    private decimal NightWork()
    {
        decimal num = 0M;
        decimal num10 = 480M;
        decimal num3 = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
        decimal num7 = ((0x5a0 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute;
        this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
        if (this.sdr != null)
        {
            decimal num11;
            decimal num12;
            decimal num13;
            decimal num2 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);
            decimal num4 = Convert.ToDecimal(this.sdr["MinuteBreakOut"]);
            decimal num5 = Convert.ToDecimal(this.sdr["MinuteBreakIn"]);
            decimal num6 = Convert.ToDecimal(this.sdr["MinuteTimeOut"]);
            decimal num8 = Convert.ToDecimal(this.sdr["LateComing"]);
            decimal num9 = Convert.ToDecimal(this.sdr["EarlyReturning"]);
            num10 = Convert.ToDecimal(this.sdr["Total"]);
            if (num3 < (num2 + num8))
            {
                num11 = num2;
            }
            else
            {
                num11 = num3;
            }
            if ((num11 > num4) && (num11 < num5))
            {
                num11 = num5;
            }
            int num14 = ((int) num11) % 60;
            if ((num14 > 0) && (num14 < 30))
            {
                num11 = (num11 - num14) + 30M;
            }
            else if ((num14 >= 30) && (num14 < 60))
            {
                num11 = (num11 - num14) + 60M;
            }
            if (num7 > (num6 - num9))
            {
                num12 = num6;
            }
            else
            {
                num12 = num7;
            }
            if ((num12 > num4) && (num12 < num5))
            {
                num12 = num4;
            }
            num14 = ((int) num12) % 60;
            if ((num14 > 0) && (num14 < 30))
            {
                num11 -= num14;
            }
            else if ((num14 >= 30) && (num14 < 60))
            {
                num11 = (num11 - num14) + 30M;
            }
            if ((num11 > num4) || (num12 < num5))
            {
                num13 = 0M;
            }
            else
            {
                num13 = num5 - num4;
            }
            if ((num12 > 1320M) && (num12 <= 1800M))
            {
                if ((num4 < 1320M) && (num5 < 1320M))
                {
                    num = num12 - ((num11 < 1320M) ? 1320M : num11);
                }
                else if ((num4 < 1320M) && (num5 > 1320M))
                {
                    num = (num12 - ((num11 < 1320M) ? 1320M : num11)) - ((num13 != 0M) ? (num5 - 1320M) : 0M);
                }
                else
                {
                    num = (num12 - ((num11 > 1320M) ? num11 : 1320M)) - num13;
                }
            }
            else if (num12 > 1800M)
            {
                if ((num4 < 1320M) && (num5 < 1320M))
                {
                    num = 1800M - ((num11 < 1320M) ? 1320M : num11);
                }
                else if ((num4 < 1320M) && (num5 > 1320M))
                {
                    num = (1800M - ((num11 < 1320M) ? 1320M : num11)) - ((num13 != 0M) ? (num5 - 1320M) : 0M);
                }
                else
                {
                    num = (1800M - ((num11 > 1320M) ? num11 : 1320M)) - num13;
                }
            }
        }
        if (num >= num10)
        {
            text = num10.ToString();
        }
        else if (num > 0M)
        {
            text = num.ToString();
        }
        return (num / num10);
    }

    private void Number()
    {
        string str = string.Empty;
        int num = 0;
        this.numValue = 0M;
        do
        {
            if (this.ch == '.')
            {
                num++;
            }
            str = str + this.ch.ToString();
            this.NextCharacter();
        }
        while ((char.IsDigit(this.ch) || (this.ch == '.')) && (num < 2));
        this.numValue = Convert.ToDecimal(str);
        this.lookAhead = Token.Num;
        this.retract = true;
    }

    private void Operand()
    {
        switch (this.lookAhead)
        {
            case Token.Ident:
                this.NextToken();
                return;

            case Token.AttendanceItemID:
                this.result = this.EvaluateAttendanceItemID(this.GetFormluaFromAttendanceItemID(this.identLexeme)).ToString();
                this.NextToken();
                return;

            case Token.Num:
                this.result = this.numValue.ToString();
                this.NextToken();
                return;

            case Token.String:
                this.result = this.identLexeme;
                this.NextToken();
                return;

            case Token.Abs:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = Math.Abs(Convert.ToDecimal(this.result)).ToString();
                return;

            case Token.Sqrt:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = Math.Sqrt(Convert.ToDouble(this.result)).ToString();
                return;

            case Token.LuaChon:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                int num = Convert.ToInt32(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                string result = this.result;
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                string str2 = this.result;
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = (num != 0) ? result : str2;
                return;
            }
            case Token.LamTron:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                decimal d = Convert.ToDecimal(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                int decimals = Convert.ToInt32(this.result);
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = Math.Round(d, decimals).ToString();
                return;
            }
            case Token.LaNgay:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.WhatIsDay().ToString();
                return;

            case Token.GioVao:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.GetTimeIn().ToString();
                return;

            case Token.GioRa:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.GetTimeOut().ToString();
                return;

            case Token.NgayNghi:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.GetValueByAbsenceID().ToString();
                return;

            case Token.CaLamViec:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = shiftID;
                return;

            case Token.NghiNuaNgay:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = Math.Abs(this.IsAbsenceHalf()).ToString();
                return;

            case Token.CongNgay:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.DayWork().ToString();
                return;

            case Token.CongDem:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.NightWork().ToString();
                return;

            case Token.LamThem:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                byte dayType = Convert.ToByte(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                byte duration = Convert.ToByte(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                byte day = Convert.ToByte(this.result);
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.Overtime(dayType, duration, day).ToString();
                return;
            }
            case Token.LaNhanVien:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.WhatEmployeeType().ToString();
                return;
            }
            case Token.NghiVaLam:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                bool outCheck = false;
                //Đăng ký nghỉ cả ngày và vẫn đi làm thì tính không đăng ký nghỉ
                Decimal X = GetNghiVaLam(out outCheck);
                if (X < 0M)
                    X = 0M - X;
                this.result = X.ToString();
                return;
            }
            case Token.SoLanDiMuon:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = SoLanDiMuon().ToString();
                return;
            }
            case Token.SoLanKDT:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = SoLanKDT().ToString();
                return;
            }
            case Token.SoGioPhuCapDem:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = TimePlus30().ToString();
                return;
            }
            case Token.SoGio:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = HourOverTime().ToString();
                return;
            }
            case Token.LamThuBay:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                bool check = IsRegisterDayOff();
                this.result = check ? "0" : "1";
                return;
            }
            case Token.KiemTraDiLam:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = KiemTraDiLam();
                return;
            }
            case Token.RaNgoai:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = MinBreakOutBreakIn().ToString();
                return;
            }

            case Token.PhuCap:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                int type = Convert.ToInt32(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                int min = Convert.ToInt32(this.result);
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = PhuCap(type, min).ToString();
                return;
            }

            case Token.DiMuon:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.LateComing().ToString();
                return;

            case Token.VeSom:
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.EarlyReturning().ToString();
                return;

            case Token.NgayCong:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Operand();
                int minute = Convert.ToInt32(this.result);
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.WorkingTime(minute).ToString();
                return;
            }
            case Token.Open:
                this.NextToken();
                this.Expression();
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                return;
        }
        this.lookAhead = Token.Err;
    }
    
    private string KiemTraDiLam()
    {
        decimal whatDay = this.WhatIsDay();
        decimal minuteTimeIn = this.GetTimeIn();
        decimal minuteTimeOut = this.GetTimeOut();
        //0: Ngày lễ, 1: Chủ nhật
        if (whatDay == 0 || whatDay == 1)
            return "";
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return "";
        DateTime now = DateTime.Now;
        TimeSpan sp = workingDay - now;
        int diff = sp.Days;
        if (minuteTimeIn == 0 && minuteTimeOut == 0 && diff <= 0)
            return "X";
        return "";
    }

    /// <summary>
    /// Tổng số giờ làm thêm
    /// </summary>
    /// <returns></returns>
    private decimal HourOverTime()
    {
        //Ca 3 không tính OT
        if (IsShiftNoOT()) return 0M;
        //
        decimal minuteTimeIn = this.GetTimeIn();
        decimal minuteTimeOut = this.GetTimeOut();
        decimal whatDay = this.WhatIsDay();

        if ((minuteTimeIn > 0) && (minuteTimeOut > 0))
        {
            if (whatDay == 0 || whatDay == 1 || (whatDay == 3 && IsRegisterDayOff()))
            {
                decimal result = 0;
                result = minuteTimeOut - minuteTimeIn;
                //
                double inttime = (int)((decimal)(result) / 30);
                return Math.Round((decimal)(inttime * 30 / 60), 1);
            }

            this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
            if (this.sdr != null)
            {
                decimal result = 0;
                decimal shiftInfo1 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]); // Giờ vào của ca
                decimal shiftInfo8 = Convert.ToDecimal(this.sdr["PreviousOvertime"]); // Ngưỡng tính làm thêm trước ca
                decimal shiftInfo4 = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra của ca
                decimal shiftInfo9 = Convert.ToDecimal(this.sdr["NextOvertime"]); // Ngưỡng tính làm thêm sau ca
                if (minuteTimeOut < shiftInfo4 + shiftInfo9)
                    return 0M;
                result = minuteTimeOut - shiftInfo4 - shiftInfo9;
                //Làm qua đêm
                if (result > 1440)
                    result = result - 1440;
                //
                double inttime = (int)((decimal)(result) / 30);
                return Math.Round((decimal)(inttime * 30 / 60), 1);
            }
        }
        return 0M;
    }
    /// <summary>
    /// Tính số giờ phụ cấp làm ca đêm
    /// </summary>
    /// <returns></returns>
    private decimal TimePlus30()
    {
        //if (IsShiftNoOT()) return 0M;
        decimal whatDay = this.WhatIsDay();
        //Ngày lễ hoặc chủ nhật thì không tính
        //0: Ngày lễ, 1: Chủ nhật
        if (whatDay == 0 || whatDay == 1)
            return 0M;
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return 0M;

        //Thực hiện tính
        decimal minuteTimeIn = this.GetTimeIn();
        decimal minuteTimeOut = this.GetTimeOut();
        
        //Từ 22h > đến 6h sáng
        if (minuteTimeIn > 0 && minuteTimeOut > 0)
        {
            this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
            if (this.sdr != null)
            {
                decimal result = 0M, retsultStart = 0M, retsultEnd = 0M;
                decimal shiftInfo1 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]); // Giờ vào của ca
                decimal shiftInfo4 = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra của ca

                if (minuteTimeIn >= nightTo)
                {
                    int plusNightFromK = plusNightFrom;
                    plusNightFromK = (int) shiftInfo1 < plusNightFromK ? plusNightFromK : (int)shiftInfo1;
                    //Ca3
                    if (shiftInfo1 == 0)
                        plusNightFromK = 1440;
                    retsultStart = minuteTimeIn > plusNightFromK ? minuteTimeIn : plusNightFromK;
                    //C3
                    if (minuteTimeOut > 1440)
                        if (shiftInfo4 < 1440)
                            shiftInfo4 = shiftInfo4 + 1440;
                    retsultEnd = minuteTimeOut > (shiftInfo4 < (nightTo + 1440) ? shiftInfo4 : (nightTo + 1440)) ? (shiftInfo4 < (nightTo + 1440) ? shiftInfo4 : (nightTo + 1440)) : minuteTimeOut;
                }
                else
                {
                    retsultStart = minuteTimeIn;
                    retsultEnd = minuteTimeOut > (shiftInfo4 < nightTo ? shiftInfo4 : nightTo) ? (shiftInfo4 < nightTo ? shiftInfo4 : nightTo) : minuteTimeOut;
                }
                result = retsultEnd - retsultStart;
                if (result < 0) result = 0;

                double inttime = (int)((decimal)(result) / 30);
                return Math.Round((decimal)(inttime * 30 / 60), 1);
            }
            else
                return 0M;
        }
        else
            return 0M;
        
    }

    private decimal PhuCap(int type, int min)
    {
        if (IsShiftNoPC()) return 0M;
        decimal whatDay = this.WhatIsDay();
        //Ngày lễ hoặc chủ nhật thì không tính
        //0: Ngày lễ, 1: Chủ nhật
        if (whatDay == 0 || whatDay == 1)
            return 0M;
        //Nếu là thứ 7 mà ca luân phiên được nghỉ cũng không tính
        if (whatDay == 3 && IsRegisterDayOff())
            return 0M;

        //Thực hiện tính
        decimal minuteTimeIn = this.GetTimeIn();
        decimal minuteTimeOut = this.GetTimeOut();

        if (minuteTimeIn > 0 && minuteTimeOut > 0)
        {
            this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
            if (this.sdr != null)
            {
                decimal result = 0M;
                decimal shiftInfo1 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]); // Giờ vào của ca
                result = minuteTimeOut - shiftInfo1;
                if (result < 0) result = 0;
                if (result > 1440) result = result - 1440;
                //if(type == 1)
                //    return result >= min ? 1M : 0M;
                //else if(type == 2)
                //    return result < min ? 1M : 0M;
                return result >= min ? 1M : 0.5M;
            }
        }
        return 0M;
    }

    //dayTpe: 0: Ngày lễ 1: Chủ nhật 2: Ngày thường 3: Thứ bẩy
    //duration: 0: Tất cả 1: Trước ca 2: Sau ca
    //day: 0: Ngày 1: Đêm
    private decimal Overtime(byte dayType, byte duration, byte day)
    {
        //Nếu ca 3 thì không tính OT
        if (IsShiftNoOT()) return 0M;
        decimal minuteTimeIn = this.GetTimeIn();
        decimal minuteTimeOut = this.GetTimeOut();
        decimal result = 0;
        decimal value = 0;
        decimal whatDay = this.WhatIsDay();
        decimal startTime = 0;
        decimal endTime = 0;
        //Đăng ký nghỉ thứ bẩy
        bool isRegisterDayOff = IsRegisterDayOff();
        if (minuteTimeIn > 0 && minuteTimeOut > 0)
        {
            this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
            if (this.sdr != null)
            {
                decimal shiftInfo1 = Convert.ToDecimal(this.sdr["MinuteTimeIn"]); // Giờ vào của ca
                decimal shiftInfo8 = Convert.ToDecimal(this.sdr["PreviousOvertime"]); // Ngưỡng tính làm thêm trước ca
                decimal shiftInfo4 = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra của ca
                decimal shiftInfo9 = Convert.ToDecimal(this.sdr["NextOvertime"]); // Ngưỡng tính làm thêm sau ca

                //Kiểm tra có làm kế tiếp sang ngày lễ hay không
                bool chkNext0 = WhatIsNextDay() == 0 ? true : false;

                if ((dayType == 0 && chkNext0 == true && whatDay != 0) || (dayType == 2 && whatDay == 2) || (dayType == 3 && whatDay == 3 && !isRegisterDayOff) || (dayType == 1 && whatDay == 3 && !isRegisterDayOff))
                {
                    //Ngày thường (bao gồm thứ bẩy không đăng ký nghỉ)
                    bool check = true;
                    decimal result0 = 0;
                    decimal result1 = 0;
                    decimal result2 = 0;
                    decimal result21 = 0;
                    if (day == 0)
                    {
                        //Ngày
                        if (minuteTimeIn < (shiftInfo1 - shiftInfo8))
                        {
                            startTime = minuteTimeIn < nightTo ? nightTo : minuteTimeIn;
                            endTime = shiftInfo1 < nightTo ? nightTo : shiftInfo1;
                            result1 = ((shiftInfo1 < nightTo) ? nightTo : shiftInfo1) - ((minuteTimeIn < nightTo) ? nightTo : minuteTimeIn);
                        }
                        if (minuteTimeOut > (shiftInfo4 + shiftInfo9))
                        {
                            if (minuteTimeOut <= nightFrom)
                            {
                                result2 = minuteTimeOut - shiftInfo4;
                            }
                            else if ((minuteTimeOut > nightFrom) && (minuteTimeOut <= (nightTo + 1440)))
                            {
                                //
                                if (minuteTimeOut >= nightFrom + shiftInfo9)
                                    check = false;
                                result2 += nightFrom - ((shiftInfo4 > nightFrom) ? nightFrom : shiftInfo4);
                                if (chkNext0)
                                {
                                    if (dayType == 0)
                                    {
                                        //Ngày lễ
                                        result0 = minuteTimeOut - 1440;
                                        if (result0 < 0) result0 = 0;
                                    }
                                }
                            }
                            else
                            {
                                //Nếu ngày hôm sau là ngày lễ chỉ tính đến 00h00, phần còn lại là nghỉ lễ
                                //Kiểm tra
                                if (!chkNext0)
                                {
                                    //Làm qua đêm
                                    //Tách nếu ngày thứ bẩy làm qua đêm sang chủ nhật
                                    //200%
                                    if (dayType == 1)
                                    {
                                        //Chủ nhật
                                        result21 = (nightFrom - ((shiftInfo4 > nightFrom) ? nightFrom : shiftInfo4)) + (minuteTimeOut - (nightTo + 1440));
                                        if (result21 < 0) result21 = 0;
                                    }
                                    else
                                    {
                                        //
                                        if (dayType == 3)
                                            //Thứ bẩy
                                            result2 += 0;
                                        else
                                            //Trường hợp khác
                                            result2 += (nightFrom - ((shiftInfo4 > nightFrom) ? nightFrom : shiftInfo4)) + (minuteTimeOut - (nightTo + 1440));
                                    }
                                }
                                else
                                { 
                                    //Ngày kế tiếp là ngày nghỉ lễ
                                    if (dayType == 0)
                                    {
                                        //Ngày lễ
                                        result0 = minuteTimeOut - 1440;
                                        if (result0 < 0) result0 = 0;
                                    }
                                    else if (dayType == 1)
                                    {
                                        //Chủ nhật
                                        result21 = (nightFrom - ((shiftInfo4 > nightFrom) ? nightFrom : shiftInfo4));
                                        if (result21 < 0) result21 = 0;
                                    }
                                    else
                                    {
                                        //
                                        if (dayType == 3)
                                            //Thứ bẩy
                                            result2 += 0;
                                        else
                                            //Trường hợp khác
                                            result2 += (nightFrom - ((shiftInfo4 > nightFrom) ? nightFrom : shiftInfo4));
                                    }
                                }
                            }
                        }
                        if (dayType == 0)
                        {
                            result = result0;
                        }
                        else if (dayType == 1)
                        {
                            result = result21;
                        }
                        else
                        {
                            if (duration == 0)
                            {
                                result = result1 + result2;
                            }
                            else if (duration == 1)
                            {
                                result = result1;
                            }
                            else
                            {
                                result = result2;
                            }
                        }
                    }
                    else
                    {
                        //Check (đêm + ngày)
                        if (minuteTimeOut >= nightTo + 1440 + shiftInfo9)
                            check = false;
                        //
                        //Đêm
                        if (minuteTimeIn < (shiftInfo1 - shiftInfo8))
                        {
                            result1 = ((shiftInfo1 > nightTo) ? nightTo : shiftInfo1) - ((minuteTimeIn > nightTo) ? nightTo : minuteTimeIn);
                        }
                        if ((minuteTimeOut > (shiftInfo4 + shiftInfo9)) && (minuteTimeOut >= nightFrom))
                        {
                            //Tách nếu ngày thứ bẩy làm qua đêm sang chủ nhật
                            if (!chkNext0)
                            {
                                if (dayType == 2)
                                {
                                    result2 = ((minuteTimeOut > (nightTo + 1440)) ? (nightTo + 1440) : minuteTimeOut) - ((shiftInfo4 < nightFrom) ? nightFrom : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                }
                                else if (dayType == 3)
                                {
                                    result2 = ((minuteTimeOut > 1440) ? 1440 : minuteTimeOut) - ((shiftInfo4 < nightFrom) ? nightFrom : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                    //result2 = 0;
                                }
                                else if (dayType == 1)
                                {
                                    //Chỉ tính đến 6h
                                    //260%
                                    check = false;
                                    result2 = ((minuteTimeOut > (nightTo + 1440)) ? (nightTo + 1440) : minuteTimeOut) - ((shiftInfo4 < 1440) ? 1440 : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                }
                            }
                            else
                            {
                                //Chỉ tính đến 0h00
                                decimal minuteTimeOutOther = 0;
                                decimal nightToOther = 0;
                                if (minuteTimeOut < 1440)
                                    minuteTimeOutOther = minuteTimeOut;
                                else
                                    minuteTimeOutOther = 1440;

                                if (dayType == 2)
                                {
                                    result2 = ((minuteTimeOutOther > (nightToOther + 1440)) ? (nightToOther + 1440) : minuteTimeOutOther) - ((shiftInfo4 < nightFrom) ? nightFrom : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                }
                                else if (dayType == 3)
                                {
                                    result2 = ((minuteTimeOutOther > 1440) ? 1440 : minuteTimeOutOther) - ((shiftInfo4 < nightFrom) ? nightFrom : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                    //result2 = 0;
                                }
                                else if (dayType == 1)
                                {
                                    //Chỉ tính đến 6h
                                    //260%
                                    check = false;
                                    result2 = ((minuteTimeOutOther > (nightToOther + 1440)) ? (nightToOther + 1440) : minuteTimeOutOther) - ((shiftInfo4 < 1440) ? 1440 : shiftInfo4);
                                    if (result2 < 0) result2 = 0;
                                }
                            }
                        }
                        if (dayType == 1)
                        {
                            result = result2;
                        }
                        else
                        {
                            if (duration == 0)
                            {
                                result = result1 + result2;
                            }
                            else if (duration == 1)
                            {
                                result = result1;
                            }
                            else
                            {
                                result = result2;
                            }
                        }
                    }
                    //Trừ ngưỡng thời gian
                    if (check)
                    {
                        if (result < shiftInfo9)
                            return 0M;
                        result = result - shiftInfo9;
                    }
                }
                //0: Ngày lễ 1: Chủ nhật 3: Thứ bẩy (đăng ký nghỉ)
                if (((dayType == 0 && whatDay == 0) || (dayType == 1 && whatDay == 1) || (dayType == 3 && whatDay == 3 && isRegisterDayOff)))
                {
                    decimal minuteShiftTimeIn, minuteShiftBreakOut, minuteShiftBreakIn, minuteShiftTimeOut, breakTime, minuteShiftNextOvertime;
                    minuteShiftTimeIn = Convert.ToDecimal(sdr["MinuteTimeIn"]); //Giờ vào
                    minuteShiftBreakOut = Convert.ToDecimal(sdr["MinuteBreakOut"]); //Giờ ra giữa giờ
                    minuteShiftBreakIn = Convert.ToDecimal(sdr["MinuteBreakIn"]); //Giờ vào giữa giờ
                    minuteShiftTimeOut = Convert.ToDecimal(sdr["MinuteTimeOut"]); //Giờ ra
                    minuteShiftNextOvertime = Convert.ToDecimal(this.sdr["NextOvertime"]); // Ngưỡng tính làm thêm sau ca

                    //Không tính trước ca
                    if (minuteTimeIn <= minuteShiftTimeIn)
                        minuteTimeIn = minuteShiftTimeIn;
                    ///////////////////////////////////////
                    ///TÍNH NGHỈ GIỮA GIỜ
                    ///////////////////////////////////////
                    if (minuteTimeIn > minuteShiftBreakOut && minuteTimeIn < minuteShiftBreakIn)
                        minuteTimeIn = minuteShiftBreakIn;
                    //minuteTimeIn = RoundTimeIn(minuteTimeIn, minuteShiftTimeIn);
                    if (minuteTimeOut > minuteShiftBreakOut && minuteTimeOut < minuteShiftBreakIn)
                        minuteTimeOut = minuteShiftBreakOut;

                    //minuteTimeOut = RoundTimeOut(minuteTimeOut, minuteShiftTimeOut);

                    if (minuteTimeIn > minuteShiftBreakOut || minuteTimeOut < minuteShiftBreakIn)
                        breakTime = 0;
                    else
                        breakTime = minuteShiftBreakIn - minuteShiftBreakOut;
                    ///////////////////////////////////////

                    if (day == 0)
                    {
                        //Ngày
                        //Giờ ra < 22h
                        if (minuteTimeOut <= nightFrom)
                            result = minuteTimeOut - minuteTimeIn - breakTime;
                        else
                            //22h < giờ ra < 6h
                            if (minuteTimeOut > nightFrom && minuteTimeOut <= nightTo + 1440)
                                //Ra giữa giờ < 22h && Vào giữa giờ < 22h
                                if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn < nightFrom)
                                {
                                    result = nightFrom - (minuteTimeIn < nightFrom ? minuteTimeIn : nightFrom) - breakTime;
                                    if (result < 0) result = 0;
                                }
                                else
                                    //Ra giữa giờ < 22h && Vào giữa giờ > 22h
                                    if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn > nightFrom)
                                    {
                                        result = nightFrom - (minuteTimeIn < nightFrom ? minuteTimeIn : nightFrom) - (breakTime != 0 ? nightFrom - minuteShiftBreakOut : 0);
                                        if (result < 0) result = 0;
                                    }
                                    else
                                        result = nightFrom - (minuteTimeIn < nightFrom ? minuteTimeIn : nightFrom);
                            else
                                //Giờ vào < 22h
                                if (minuteTimeIn < nightFrom)
                                {
                                    if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn < nightFrom)
                                        result = nightFrom - minuteTimeIn - breakTime;
                                    else
                                        if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn > nightFrom)
                                            result = nightFrom - minuteTimeIn - (breakTime != 0 ? nightFrom - minuteShiftBreakOut : 0);
                                        else
                                            result = nightFrom - minuteTimeIn;
                                    if (minuteShiftBreakOut < nightTo + 1440 && minuteShiftBreakIn < nightTo + 1440)
                                        result += minuteTimeOut - (nightTo + 1440);
                                    else
                                        if (minuteShiftBreakOut < nightTo + 1440 && minuteShiftBreakIn > nightTo + 1440)
                                            result += minuteTimeOut - (nightTo + 1440) - (breakTime != 0 ? minuteShiftBreakIn - (nightTo + 1440) : 0);
                                        else
                                            result += minuteTimeOut - breakTime;
                                }
                                else
                                    if (minuteShiftBreakOut < nightTo + 1440 && minuteShiftBreakIn < nightTo + 1440)
                                        result = minuteTimeOut - (nightTo + 1440);
                                    else
                                        if (minuteShiftBreakOut < nightTo + 1440 && minuteShiftBreakIn > nightTo + 1440)
                                            result = minuteTimeOut - (nightTo + 1440) - (breakTime != 0 ? minuteShiftBreakIn - (nightTo + 1440) : 0);
                                        else
                                            result = minuteTimeOut - breakTime;
                    }
                    else
                    {
                        //Đêm
                        if (minuteTimeIn >= nightFrom)
                            result = minuteTimeOut - minuteTimeIn - breakTime;
                        else
                            if (minuteTimeOut >= nightFrom && minuteTimeOut <= nightTo + 1440)
                                if (minuteShiftTimeIn < nightFrom)
                                    result = (minuteTimeOut > nightTo + 1440 ? nightTo + 1440 : minuteTimeOut) - nightFrom - breakTime;
                                else
                                    if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn > nightFrom)
                                        result = (minuteTimeOut > nightTo + 1440 ? nightTo + 1440 : minuteTimeOut) - nightFrom - (breakTime != 0 ? minuteShiftBreakIn - nightFrom : 0);
                                    else
                                        result = (minuteTimeOut > nightTo + 1440 ? nightTo + 1440 : minuteTimeOut) - nightFrom;
                            else
                                if (minuteTimeOut > nightTo + 1440)
                                {
                                    if (minuteShiftBreakOut > nightFrom && minuteShiftBreakIn > nightFrom)
                                        result = nightTo + 1440 - nightFrom - breakTime;
                                    else
                                        if (minuteShiftBreakOut < nightFrom && minuteShiftBreakIn > nightFrom)
                                            result = nightTo + 1440 - nightFrom - (breakTime != 0 ? minuteShiftBreakIn - nightFrom : 0);
                                        else
                                            result = nightTo + 1440 - nightFrom;
                                }
                    }
                    //if (result < shiftInfo9)
                    //    return 0M;
                    //result = result - shiftInfo9;
                    //if (((((dayType == 3) && (whatDay == 3)) || ((dayType == 1) && (whatDay == 1))) || ((dayType == 0) && (whatDay == 0))) && (day == 0))
                    //{
                    //    //Ngày lễ, chủ nhật, nghỉ luân phiên
                    //    decimal breakTime;
                    //    decimal minuteShiftTimeIn = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);// Giờ vào của ca
                    //    decimal minuteShiftBreakOut = Convert.ToDecimal(this.sdr["MinuteBreakOut"]); // Ra giữa giờ
                    //    decimal minuteShiftBreakIn = Convert.ToDecimal(this.sdr["MinuteBreakIn"]); // Vào giữa giờ
                    //    decimal minuteShiftTimeOut = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra của ca
                    //    if ((minuteTimeIn > minuteShiftBreakOut) && (minuteTimeIn < minuteShiftBreakIn))
                    //        minuteTimeIn = minuteShiftBreakIn;
                    //    if ((minuteTimeOut > minuteShiftBreakOut) && (minuteTimeOut < minuteShiftBreakIn))
                    //        minuteTimeOut = minuteShiftBreakOut;
                    //    if ((minuteTimeIn > minuteShiftBreakOut) || (minuteTimeOut < minuteShiftBreakIn))
                    //        breakTime = 0;
                    //    else
                    //        breakTime = minuteShiftBreakIn - minuteShiftBreakOut;

                    //    if (minuteTimeOut <= nightFrom)
                    //    {
                    //        result = (minuteTimeOut - minuteTimeIn) - breakTime;
                    //    }
                    //    else if ((minuteTimeOut > nightFrom) && (minuteTimeOut <= (nightTo + 1440)))
                    //    {
                    //        if ((minuteShiftBreakOut < nightFrom) && (minuteShiftBreakIn < nightFrom))
                    //        {
                    //            result = (nightFrom - ((minuteTimeIn < nightFrom) ? minuteTimeIn : nightFrom)) - breakTime;
                    //        }
                    //        else if ((minuteShiftBreakOut < nightFrom) && (minuteShiftBreakIn > nightFrom))
                    //        {
                    //            result = (nightFrom - ((minuteTimeIn < nightFrom) ? minuteTimeIn : nightFrom)) - ((breakTime != 0) ? (nightFrom - minuteShiftBreakOut) : 0M);
                    //        }
                    //        else
                    //        {
                    //            result = nightFrom - ((minuteTimeIn < nightFrom) ? minuteTimeIn : nightFrom);
                    //        }
                    //    }
                    //    else if (minuteTimeIn < nightFrom)
                    //    {
                    //        if ((minuteShiftBreakOut < nightFrom) && (minuteShiftBreakIn < nightFrom))
                    //        {
                    //            result = (nightFrom - minuteTimeIn) - breakTime;
                    //        }
                    //        else if ((minuteShiftBreakOut < nightFrom) && (minuteShiftBreakIn > nightFrom))
                    //        {
                    //            result = (nightFrom - minuteTimeIn) - ((breakTime != 0) ? (nightFrom - minuteShiftBreakOut) : 0);
                    //        }
                    //        else
                    //        {
                    //            result = nightFrom - minuteTimeIn;
                    //        }
                    //        if ((minuteShiftBreakOut < (nightTo + 1440)) && (minuteShiftBreakIn < (nightTo + 1440)))
                    //        {
                    //            result += minuteTimeOut - (nightTo + 1440);
                    //        }
                    //        else if ((minuteShiftBreakOut < (nightTo + 1440)) && (minuteShiftBreakIn > (nightTo + 1440)))
                    //        {
                    //            result += (minuteTimeOut - (nightTo + 1440)) - ((breakTime != 0) ? (minuteShiftBreakIn - (nightTo + 1440)) : 0M);
                    //        }
                    //        else
                    //        {
                    //            result += minuteTimeOut - breakTime;
                    //        }
                    //    }
                    //    else if ((minuteShiftBreakOut < (nightTo + 1440)) && (minuteShiftBreakIn < (nightTo + 1440)))
                    //    {
                    //        result = minuteTimeOut - (nightTo + 1440);
                    //    }
                    //    else if ((minuteShiftBreakOut < (nightTo + 1440)) && (minuteShiftBreakIn > (nightTo + 1440)))
                    //    {
                    //        result = (minuteTimeOut - (nightTo + 1440)) - ((breakTime != 0) ? (minuteShiftBreakIn - (nightTo + 1440)) : 0M);
                    //    }
                    //    else
                    //    {
                    //        result = minuteTimeOut - breakTime;
                    //    }
                }
                
                //
                SqlCommand command = new SqlCommand("RegisterOvertime_GetValueByEmployeeID", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
                command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
                value = Convert.ToDecimal(command.ExecuteScalar());
                if ((result > (60 * value)) && (value >= 0))
                {
                    result = value * 60;
                }
            }
        }
        double inttime = (int)((decimal)(result) / 30);
        return Math.Round((decimal)(inttime * 30 / 60), 1);
        //return Math.Round((decimal)(result / 60M), 1);
    }

    [SqlProcedure]
    public static void ReleaseCalculateDayWork(int sPID)
    {
        for (int i = 0; i < shiftDetail.Rows.Count; i++)
        {
            if (((int) shiftDetail.Rows[i]["SPID"]) == sPID)
            {
                shiftDetail.Rows[i].Delete();
            }
        }
        shiftDetail.AcceptChanges();
        if (shiftDetail.Rows.Count == 0)
        {
            attendanceItem = null;
            shiftDetail = null;
        }
    }

    private decimal RoundNextOvertime(int value)
    {
        SqlCommand command = new SqlCommand("TimeInTimeOut_RoundNextOvertime", conn) {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@Value", SqlDbType.Int, 4).Value = value;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private decimal RoundTimeIn(decimal value, decimal minuteShiftTimeIn)
    {
        decimal num;
        decimal num2 = 0M;
        if (value == minuteShiftTimeIn)
        {
            return value;
        }
        if (value < minuteShiftTimeIn)
        {
            num = (minuteShiftTimeIn - value) % 60M;
            if (num == 0M)
            {
                num2 = value;
            }
            else if (num <= 30M)
            {
                num2 = value + num;
            }
            else
            {
                num2 = (value + num) - 30M;
            }
            return num2;
        }
        num = (((int) (value / 60M)) * 60) + (minuteShiftTimeIn % 60M);
        if ((value - num) <= 30M)
        {
            num2 = num + 30M;
        }
        else
        {
            num2 = num + 60M;
        }
        return num2;
    }

    private decimal RoundTimeOut(decimal value, decimal minuteShiftTimeOut)
    {
        decimal num;
        decimal num2 = 0M;
        if (value == minuteShiftTimeOut)
        {
            return value;
        }
        if (value > minuteShiftTimeOut)
        {
            num = (value - minuteShiftTimeOut) % 60M;
            if (num < 30M)
            {
                num2 = value - num;
            }
            else
            {
                num2 = (value - num) + 30M;
            }
            return num2;
        }
        num = ((((int) (value / 60M)) + 1) * 60) + (minuteShiftTimeOut % 60M);
        if ((num - value) >= 30M)
        {
            num2 = num - 60M;
        }
        else
        {
            num2 = num;
        }
        return num2;
    }

    private void String()
    {
        this.identLexeme = string.Empty;
        this.NextCharacter();
        while (this.ch != '\'')
        {
            this.identLexeme = this.identLexeme + this.ch.ToString();
            this.NextCharacter();
        }
        this.lookAhead = Token.String;
    }

    private void Term()
    {
        Operator eXPONENT = Operator.EXPONENT;
        this.Factor();
        string result = this.result;
        while ((((this.lookAhead == Token.Exponent) || (this.lookAhead == Token.Times)) || (this.lookAhead == Token.Division)) || (this.lookAhead == Token.Modulus))
        {
            switch (this.lookAhead)
            {
                case Token.Times:
                    eXPONENT = Operator.TIMES;
                    break;

                case Token.Division:
                    eXPONENT = Operator.DIVISION;
                    break;

                case Token.Modulus:
                    eXPONENT = Operator.MODULUS;
                    break;

                case Token.Exponent:
                    eXPONENT = Operator.EXPONENT;
                    break;
            }
            this.MultiplyOperator();
            this.Factor();
            switch (eXPONENT)
            {
                case Operator.EXPONENT:
                    result = Math.Pow(Convert.ToDouble(result), Convert.ToDouble(this.result)).ToString();
                    goto Label_012E;

                case Operator.TIMES:
                {
                    decimal num2 = Convert.ToDecimal(result) * Convert.ToDecimal(this.result);
                    result = num2.ToString();
                    goto Label_012E;
                }
                case Operator.DIVISION:
                    if (!(Convert.ToDecimal(this.result) == 0M))
                    {
                        break;
                    }
                    result = "0";
                    goto Label_012E;

                case Operator.MODULUS:
                    result = (Convert.ToDecimal(result) % Convert.ToDecimal(this.result)).ToString();
                    goto Label_012E;

                default:
                    goto Label_012E;
            }
            result = (Convert.ToDecimal(result) / Convert.ToDecimal(this.result)).ToString();
        Label_012E:;
        }
        this.result = result;
    }

    private void UnaryOperator()
    {
        if ((this.lookAhead == Token.Negate) || (this.lookAhead == Token.Not))
        {
            if (this.lookAhead == Token.Negate)
            {
                this.negate = true;
            }
            if (this.lookAhead == Token.Not)
            {
                this.not = true;
            }
            this.NextToken();
        }
    }

    /// <summary>
    /// Kiểm tra xem có phải đăng ký làm luân phiên thứ bẩy không
    /// Đăng ký là nghỉ
    /// Không đăng ký thì đi làm
    /// </summary>
    /// <returns></returns>
    private bool IsRegisterDayOff()
    {
        SqlCommand command = new SqlCommand("RegisterDayOffGroup_GetByEmployeeIDAndWorkingDay", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        return Convert.ToBoolean(command.ExecuteScalar());
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private bool IsShiftNoPC()
    {
        SqlCommand command = new SqlCommand("IsShiftNoPC", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@ShiftID", SqlDbType.VarChar, 10).Value = shiftID;
        return Convert.ToBoolean(command.ExecuteScalar());
    }

    /// <summary>
    /// Kiểm tra xem ca có tính OT không
    /// </summary>
    /// <returns></returns>
    private bool IsShiftNoOT()
    {
        SqlCommand command = new SqlCommand("IsShiftNoOT", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@ShiftID", SqlDbType.VarChar, 10).Value = shiftID;
        return Convert.ToBoolean(command.ExecuteScalar());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private decimal WhatIsDay()
    {
        SqlCommand command = new SqlCommand("WhatIsDay", conn) {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private decimal WhatIsNextDay()
    {
        SqlCommand command = new SqlCommand("WhatIsDay", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay.AddDays(1);
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    /// <summary>
    /// Kiểm tra xem có phải là ca chế độ không
    /// </summary>
    /// <param name="ShiftID"></param>
    /// <returns></returns>
    private bool IsShiftRelated(String ShiftID)
    {
        SqlCommand command = new SqlCommand("Shift_CheckIsRelated", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@ID", SqlDbType.NVarChar, 10).Value = ShiftID;
        return Convert.ToBoolean(command.ExecuteScalar());
    }

    //0: Thử việc 1: Chính thức
    private decimal WhatEmployeeType()
    {
        SqlCommand command = new SqlCommand("WhatEmployeeType", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private decimal GetNghiVaLam(out bool bolTinhLuong)
    {
        SqlCommand command = new SqlCommand("RegisterAbsence_GetByEmployeeIDAndWorkingDay", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        decimal ValReturn = 0M;
        bolTinhLuong = false;
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                ValReturn = Convert.ToDecimal(reader.GetValue(0));
                bolTinhLuong = Convert.ToBoolean(reader.GetValue(1));
            }
        }
        return ValReturn;
    }

    //private decimal WorkingTime(int minute)
    //{
    //    decimal num = 0M;
    //    decimal num13 = 480M;
    //    decimal num3 = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
    //    decimal num7 = ((0x5a0 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute;

    //    //DateTime now = DateTime.Now;
    //    //TimeSpan ts = now - workingDay;
    //    //if (num3 == 0 && num7 == 0 && ts.Days >= 0)
    //    //    return -1M;
        
    //    this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });
    //    if (this.sdr != null)
    //    {
    //        decimal num10;
    //        decimal num11;
    //        decimal num12;
    //        decimal minuteShiftTimeIn = Convert.ToDecimal(this.sdr["MinuteTimeIn"]);
    //        decimal num4 = Convert.ToDecimal(this.sdr["MinuteBreakOut"]);
    //        decimal num5 = Convert.ToDecimal(this.sdr["MinuteBreakIn"]);
    //        decimal minuteShiftTimeOut = Convert.ToDecimal(this.sdr["MinuteTimeOut"]);
    //        decimal num8 = Convert.ToDecimal(this.sdr["LateComing"]);
    //        decimal num9 = Convert.ToDecimal(this.sdr["EarlyReturning"]);
    //        num13 = Convert.ToDecimal(this.sdr["Total"]);
    //        if (num3 < (minuteShiftTimeIn + num8))
    //        {
    //            num10 = minuteShiftTimeIn;
    //        }
    //        else
    //        {
    //            num10 = num3;
    //        }
    //        if ((num10 > num4) && (num10 < num5))
    //        {
    //            num10 = num5;
    //        }
    //        num10 = this.RoundTimeIn(num10, minuteShiftTimeIn);
    //        if (num7 > (minuteShiftTimeOut - num9))
    //        {
    //            num11 = minuteShiftTimeOut;
    //        }
    //        else
    //        {
    //            num11 = num7;
    //        }
    //        if ((num11 > num4) && (num11 < num5))
    //        {
    //            num11 = num4;
    //        }
    //        num11 = this.RoundTimeOut(num11, minuteShiftTimeOut);
    //        //hack
    //        if (num11 < num10)
    //            num11 = num11 + 1440;
    //        //
    //        if ((num10 > num4) || (num11 < num5))
    //        {
    //            num12 = 0M;
    //        }
    //        else
    //        {
    //            num12 = num5 - num4;
    //        }
    //        num = (num11 - num10) - num12;
    //    }
    //    if (num >= (num13 - minute))
    //    {
    //        return 1M;
    //    }
    //    if ((num < (num13 - minute)) && (num >= ((num13 / 2M) - minute)))
    //    {
    //        //return 0.5M;
    //        return 1M;
    //    }
    //    return 0M;
    //}

    private decimal WorkingTime(int minute)
    {
        //Cứ có h vào và h ra tính là 01 công. Vì nếu người đó đi sớm về muộn đã bị trừ lương trong phần off work.
        decimal num3 = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute;
        decimal num7 = ((0x5a0 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute;
        if (num3 > 0 && num7 > 0)
            return 1M;
        else
            return 0M;
    }


    //private decimal WorkingTime(int minute)
    //{
    //    //Kiểm tra nếu đăng ký nghỉ cả ngày (có công) thì không tính công nữa (mặc dù vẫn đi làm)
    //    Decimal numRegisterAbsence = 0M;
    //    bool outCheck = false;
    //    numRegisterAbsence = GetNghiVaLam(out outCheck);

    //    if (numRegisterAbsence < 0M)
    //        numRegisterAbsence = 0M - numRegisterAbsence;

    //    if (numRegisterAbsence == 1M && outCheck)
    //        return 0M;

    //    //Kiểm tra nếu đi muộn > 5 phút tính trong đăng ký nghỉ
    //    //Nếu không đăng ký nghỉ thì tính bình thường
    //    if (LaDiMuon() && numRegisterAbsence > 0M)
    //        return 1 - numRegisterAbsence;

    //    decimal num = 0M;
    //    decimal num13 = 480M; //Số phút tính công
    //    decimal num3 = ((0x5a0 * timeIn.Subtract(workingDay).Days) + (60 * timeIn.Hour)) + timeIn.Minute; //Giờ vào
    //    decimal num7 = ((0x5a0 * timeOut.Subtract(workingDay).Days) + (60 * timeOut.Hour)) + timeOut.Minute; //Giờ ra
    //    this.sdr = shiftDetail.Rows.Find(new object[] { this.sPID, shiftID });


    //    bool bolShiftRelated = IsShiftRelated(shiftID);
    //    bool bolFirst = false;
    //    bool bolMid = false;
    //    bool bolEnd = false;

    //    if (this.sdr != null)
    //    {
    //        decimal num10; //Vào
    //        decimal num11; //Ra
    //        decimal num12; //Thời gian nghỉ giữa giờ
    //        decimal minuteShiftTimeIn = Convert.ToDecimal(this.sdr["MinuteTimeIn"]); // Giờ vào
    //        decimal num4 = Convert.ToDecimal(this.sdr["MinuteBreakOut"]); // Ra giữa giờ
    //        decimal num5 = Convert.ToDecimal(this.sdr["MinuteBreakIn"]); // Vào giữa giờ
    //        decimal minuteShiftTimeOut = Convert.ToDecimal(this.sdr["MinuteTimeOut"]); // Giờ ra
    //        decimal num8 = Convert.ToDecimal(this.sdr["LateComing"]); //Đi muộn
    //        decimal num9 = Convert.ToDecimal(this.sdr["EarlyReturning"]); // Về sớm
    //        num13 = Convert.ToDecimal(this.sdr["Total"]); //Số phút tính công

    //        //Giờ vào
    //        if (num3 < (minuteShiftTimeIn + num8))
    //        {
    //            num10 = minuteShiftTimeIn;
    //            bolFirst = true;
    //        }
    //        else
    //        {
    //            num10 = num3;
    //        }
    //        //Quẹt trong thời gian giữa giờ lấy thời gian là Vào giữa giờ
    //        if ((num10 > num4) && (num10 < num5))
    //        {
    //            if (bolShiftRelated)
    //            {
    //                num10 = num3;
    //            }
    //            else
    //            {
    //                num10 = num5;
    //            }
    //            bolMid = true;
    //        }
    //        num10 = this.RoundTimeIn(num10, minuteShiftTimeIn);

    //        //Giờ ra
    //        if (num7 > (minuteShiftTimeOut - num9))
    //        {
    //            num11 = minuteShiftTimeOut;
    //            bolEnd = true;
    //        }
    //        else
    //        {
    //            num11 = num7;
    //        }

    //        //Quẹt ra vào thời gian giữa giờ tính thời gian là thời gian nghỉ giữa giờ
    //        if ((num11 > num4) && (num11 < num5))
    //        {
    //            if (bolShiftRelated)
    //                num11 = num7;
    //            else
    //                num11 = num4;
    //        }

    //        num11 = this.RoundTimeOut(num11, minuteShiftTimeOut);

    //        //Tính thời gian nghỉ giữa ca
    //        //Là ca ưu đãi thì tính cả thời gian nghỉ giữa ca
    //        if (bolShiftRelated)
    //        {
    //            num12 = 0M;
    //        }
    //        else
    //        {
    //            if ((num10 > num4) || (num11 < num5))
    //            {
    //                num12 = 0M;
    //            }
    //            else
    //            {
    //                num12 = num5 - num4;
    //            }
    //        }

    //        //Thời gian làm việc
    //        num = (num11 - num10) - num12;
    //    }

    //    Decimal numReturn = 0M;

    //    //Ca chế độ
    //    if ((bolShiftRelated && bolFirst && bolMid) || (bolShiftRelated && bolMid && bolEnd))
    //        return 0.5M;

    //    if (num >= (num13 - minute))
    //    {
    //        numReturn = 1M;
    //    }
    //    else if (num >= minute)
    //    {
    //        double inttime = (int)((decimal)(num) / 30);
    //        numReturn =  Math.Round((Math.Round((decimal)(inttime * 30 / 60) * 60, 1)) / num13 , 4);
    //    }
    //    //if ((num < (num13 - minute)) && (num >= ((num13 / 2M) - minute)))
    //    //{
    //    //    return 0.5M;
    //    //}

    //    //Làm không đủ công và đăng ký nghỉ phép thì tính trong đăng ký nghỉ
    //    if (numReturn > 0M && numReturn < 1M && numRegisterAbsence > 0M)
    //    {
    //        //if (numReturn >= 1 - numRegisterAbsence)
    //        numReturn = 1 - numRegisterAbsence;
    //    }
    //    //Trả về công
    //    return numReturn;
    //}

    [StructLayout(LayoutKind.Sequential)]
    private struct Func
    {
        public CalculateDayWork.Token erep;
        public string irep;
    }

    private enum Operator
    {
        EXPONENT,
        PLUS,
        MINUS,
        TIMES,
        DIVISION,
        MODULUS,
        AND,
        OR,
        EQUAL,
        NOTEQUAL,
        GREATERTHAN,
        GREATERTHANOREQUAL,
        LESSTHAN,
        LESSTHANOREQUAL,
        NULL
    }

    private enum Token
    {
        Ident,
        AttendanceItemID,
        Num,
        Char,
        String,
        Int,
        Negate,
        Abs,
        Sqrt,
        LuaChon,
        LamTron,
        LaNgay,
        GioVao,
        GioRa,
        NgayNghi,
        CaLamViec,
        NghiNuaNgay,
        CongNgay,
        CongDem,
        LamThem,
        LaNhanVien,
        NghiVaLam,
        SoLanDiMuon,
        SoLanKDT,
        DiMuon,
        VeSom,
        NgayCong,
        SoGioPhuCapDem,
        PhuCap,
        SoGio,
        LamThuBay,
        KiemTraDiLam,
        RaNgoai,
        Open,
        Close,
        Plus,
        Minus,
        Times,
        Division,
        Modulus,
        Exponent,
        Comma,
        Not,
        And,
        Or,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Err,
        Null
    }
}


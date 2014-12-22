using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.InteropServices;

public class GetDayWork
{
    private static DataTable attendanceItem;
    private char ch;
    private char chrValue;
    private static SqlConnection conn;
    private const string CONNECTIONSTRING = "context connection=true";
    private static int employeeID;
    private string expression;
    private static Func[] funcTb = new Func[5];
    private string identLexeme;
    private int index = 0;
    private bool isExpression = true;
    private Token lookAhead;
    private static DataTable monthAttendance;
    private bool negate;
    private bool not;
    private const int NUMBEROFFUNCTIONS = 5;
    private decimal numValue;
    private string result;
    private bool retract;
    private int sPID;
    private static ArrayList sPIDs = new ArrayList();
    private static DateTime workingDay;

    public GetDayWork(string expression, int sPID)
    {
        this.expression = expression;
        this.ch = ' ';
        this.sPID = sPID;
    }

    private void AddOperator()
    {
        this.NextToken();
    }

    [SqlFunction(DataAccess=DataAccessKind.Read, SystemDataAccess=SystemDataAccessKind.Read)]
    public static string DayWork(string formula, int _employeeID, DateTime _workingDay, int sPID)
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
        if (funcTb[0].erep == Token.Ident)
        {
            InitFunction();
        }
        if (attendanceItem == null)
        {
            InitAttendanceItem();
        }
        if (!sPIDs.Contains(sPID))
        {
            InitMonthAttendance(sPID);
            sPIDs.Add(sPID);
        }
        GetDayWork work = new GetDayWork(formula, sPID);
        return work.Eval();
    }

    public string Eval()
    {
        this.NextToken();
        this.Expression();
        if ((this.lookAhead == Token.Err) || (this.lookAhead != Token.Null))
        {
            this.isExpression = false;
        }
        return (this.isExpression ? this.result : string.Empty);
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
        GetDayWork work = new GetDayWork(formula, this.sPID);
        return work.Eval();
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
        string textByAttendanceItemID = string.Empty;
        if (attendanceItem.Rows.Find(attendanceItemID) != null)
        {
            textByAttendanceItemID = this.GetTextByAttendanceItemID(attendanceItemID);
        }
        return textByAttendanceItemID;
    }

    private string GetTextByAttendanceItemID(string attendanceItemID)
    {
        DataRow row = monthAttendance.Rows.Find(new object[] { this.sPID, employeeID, workingDay, attendanceItemID });
        if (row != null)
        {
            if (row["Text"].ToString() == string.Empty)
            {
                return string.Empty;
            }
            if (this.IsNumeric(row["Text"].ToString()))
            {
                return Convert.ToDecimal(row["Text"]).ToString("#,##0.####;;-");
            }
            return ("'" + row["Text"].ToString() + "'");
        }
        return string.Empty;
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
        funcTb[4].erep = Token.CongTinhLuong;
        funcTb[4].irep = "CONGTINHLUONG";
    }

    private static void InitMonthAttendance(int sPID)
    {
        SqlCommand selectCommand = new SqlCommand("MonthAttendance__GetContent", conn) {
            CommandType = CommandType.StoredProcedure
        };
        selectCommand.Parameters.Add("@SPID", SqlDbType.Int, 4).Value = sPID;
        SqlDataAdapter adapter = new SqlDataAdapter(selectCommand);
        if (monthAttendance == null)
        {
            monthAttendance = new DataTable();
        }
        adapter.Fill(monthAttendance);
        if (monthAttendance.PrimaryKey.Length == 0)
        {
            monthAttendance.PrimaryKey = new DataColumn[] { monthAttendance.Columns["SPID"], monthAttendance.Columns["EmployeeID"], monthAttendance.Columns["WorkingDay"], monthAttendance.Columns["AttendanceItemID"] };
        }
    }

    public bool IsNumeric(string s)
    {
        decimal num;
        return decimal.TryParse(Convert.ToString(s), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out num);
    }

    private Token LookUpAttendanceItemID(string identLexeme)
    {
        return ((attendanceItem.Rows.Find(identLexeme) != null) ? Token.AttendanceItemID : Token.Ident);
    }

    private Token LookUpFunction(string identLexeme)
    {
        for (int i = 0; i < 5; i++)
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
            case Token.CongTinhLuong:
            {
                this.NextToken();
                if (this.lookAhead == Token.Open)
                {
                    this.NextToken();
                }
                this.Expression();
                byte closingDay = Convert.ToByte(this.result);
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                string symbol = this.result;
                if (this.lookAhead == Token.Comma)
                {
                    this.NextToken();
                }
                this.Expression();
                string attendanceItemID = this.result;
                if (this.lookAhead == Token.Close)
                {
                    this.NextToken();
                }
                this.result = this.WorkingSymbol(closingDay, symbol, attendanceItemID);
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

    [SqlProcedure]
    public static void ReleaseDayWork(int sPID)
    {
        for (int i = 0; i < monthAttendance.Rows.Count; i++)
        {
            if (((int) monthAttendance.Rows[i]["SPID"]) == sPID)
            {
                monthAttendance.Rows[i].Delete();
            }
        }
        monthAttendance.AcceptChanges();
        sPIDs.Remove(sPID);
        if (sPIDs.Count == 0)
        {
            attendanceItem = null;
            monthAttendance = null;
        }
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

    private decimal WhatIsDay()
    {
        SqlCommand command = new SqlCommand("WhatIsDay", conn) {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@WorkingDay", SqlDbType.DateTime, 8).Value = workingDay;
        command.Parameters.Add("@EmployeeID", SqlDbType.Int, 4).Value = employeeID;
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    private string WorkingSymbol(byte closingDay, string symbol, string attendanceItemID)
    {
        string str = string.Empty;
        decimal num = this.WhatIsDay();
        if ((((DateTime.Today.Day > closingDay) && ((num == 2M) || (num == 3M))) && (workingDay.Day > closingDay)) && (monthAttendance.Rows.Find(new object[] { this.sPID, employeeID, workingDay, attendanceItemID }) == null))
        {
            str = (num == 2M) ? symbol : (symbol + "/2");
        }
        return str;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Func
    {
        public GetDayWork.Token erep;
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
        CongTinhLuong,
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


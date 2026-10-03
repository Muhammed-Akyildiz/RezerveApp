using System;
using System.Data;
using Npgsql;
using Dapper;

class Program
{
    static void Main()
    {
        string connStr = "Host=ep-delicate-lake-b4zjf6i4-pooler.c-6.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_d8nZRTfiwlj1;SSL Mode=Require;Trust Server Certificate=true;";
        using (var connection = new NpgsqlConnection(connStr))
        {
            var es = connection.Query("SELECT * FROM \"EmployeeServices\" WHERE \"EmployeeId\"=11;");
            foreach(var e in es) { Console.WriteLine("EmpSrv: Emp=" + e.EmployeeId + " Srv=" + e.ServiceId); }
        }
    }
}

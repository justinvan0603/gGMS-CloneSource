using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
//Add MySql Library
using MySql.Data.MySqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace testCloneOnLinux.Data
{
    public class DatabaseConnect
    {
        private MySqlConnection connection;
        private string connectionString;
        //private IHostingEnvironment
        ////Constructor
        //public DatabaseConnect()
        //{
        //    var builder = new ConfigurationBuilder()
        //        .SetBasePath(_contentRootPath)
        //        .AddJsonFile("appsettings.json")
        //        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

        //    this.connectionString = ConfigurationManager.AppSettings["connectionStrings"];
        //    //connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
        //    this.connection = new MySqlConnection(connectionString);
        //}
        public DatabaseConnect(string connectString)
        {
            this.connection = new MySqlConnection(connectString);
            
        }
        //open connection to database
        public ObjectResult OpenConnection()
        {
            try
            {
                connection.Open();
                ObjectResult rs = new ObjectResult();
                rs.isSucceeded = true;
                rs.ErrorMessage = "";
                return rs;
            }
            catch (MySqlException ex)
            {
                ObjectResult rs = new ObjectResult();
                rs.isSucceeded = false;
                rs.ErrorMessage = ex.Message;
                //When handling errors, you can your application's response based on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                //switch (ex.Number)
                //{
                //    case 0:
                //        MessageBox.Show("Cannot connect to server.  Contact administrator");
                //        break;

                //    case 1045:
                //        MessageBox.Show("Invalid username/password, please try again");
                //        break;
                //}
                return rs;
            }
        }

        //Close connection
        public bool CloseConnection()
        {
            try
            {
                connection.Close();
                
                return true;
            }
            catch (MySqlException ex)
            {
               // MessageBox.Show(ex.Message);
                return false;
            }
        }
        public async Task<ObjectResult> ExecuteCommand(string command)
        {
            try
            {

                MySqlCommand mySQLCmd = new MySqlCommand(command, connection);
                int row_affected = await mySQLCmd.ExecuteNonQueryAsync();
                if (row_affected < 0)
                {
                    ObjectResult result = new ObjectResult();
                    result.isSucceeded = true;
                    result.ErrorMessage = "Có lỗi xảy ra khi thực thi. Row affected = " +row_affected.ToString();
                    return result;
                    
                }
                ObjectResult rs = new ObjectResult();
                rs.isSucceeded = true;
                rs.ErrorMessage = "";
                return rs;



            }
            catch(Exception ex)
            {
                ObjectResult rs = new ObjectResult();
                rs.isSucceeded = false;
                rs.ErrorMessage = ex.Message;
                //this.CloseConnection();
                return rs;
            }
        }

        ////Insert statement
        //public void Insert(string query)
        //{
        //    //string query = "INSERT INTO tableinfo (name, age) VALUES('John Smith', '33')";

        //    //open connection
        //    if (this.OpenConnection() == true)
        //    {
        //        //create command and assign the query and connection from the constructor
        //        MySqlCommand cmd = new MySqlCommand(query, connection);
                
        //        //Execute command
        //        cmd.ExecuteNonQuery();

        //        //close connection
        //        this.CloseConnection();
        //    }
        //}

        ////Update statement
        //public void Update(string query)
        //{
        //    //string query = "UPDATE tableinfo SET name='Joe', age='22' WHERE name='John Smith'";

        //    //Open connection
        //    if (this.OpenConnection() == true)
        //    {
        //        //create mysql command
        //        MySqlCommand cmd = new MySqlCommand();
        //        //Assign the query using CommandText
        //        cmd.CommandText = query;
        //        //Assign the connection using Connection
        //        cmd.Connection = connection;

        //        //Execute query
        //        cmd.ExecuteNonQuery();

        //        //close connection
        //        this.CloseConnection();
        //    }
        //}

        //Delete statement
        //public void Delete(string query)
        //{
        //    //string query = "DELETE FROM tableinfo WHERE name='John Smith'";

        //    if (this.OpenConnection() == true)
        //    {
        //        MySqlCommand cmd = new MySqlCommand(query, connection);
        //        cmd.ExecuteNonQuery();
        //        this.CloseConnection();
        //    }
        //}

        //Select statement
        //public DataSet Select(string query)
        //{
        //    //string query = "SELECT * FROM wp_users";
        //    DataSet list = null;
        //    //Open connection
        //    if (this.OpenConnection() == true)
        //    {

        //        MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter(query, connection);
        //        DataSet dataset = new DataSet();
        //        mySqlDataAdapter.Fill(dataset);
        //        list = dataset;
        //        //close Connection
        //        this.CloseConnection();

        //        //return list to be displayed
        //        return list;
        //    }
        //    else
        //    {
        //        return list;
        //    }
        //}

        //Count statement
        //public int Count(string query)
        //{
        //    //string query = "SELECT Count(*) FROM tableinfo";
        //    int Count = -1;

        //    //Open Connection
        //    if (this.OpenConnection() == true)
        //    {
        //        //Create Mysql Command
        //        MySqlCommand cmd = new MySqlCommand(query, connection);

        //        //ExecuteScalar will return one value
        //        Count = int.Parse(cmd.ExecuteScalar()+"");
                
        //        //close Connection
        //        this.CloseConnection();

        //        return Count;
        //    }
        //    else
        //    {
        //        return Count;
        //    }
        //}

        //Backup
        //public void Backup(string path)
        //{
        //    try
        //    {
        //       // string constring = "server=localhost;user=root;pwd=qwerty;database=test;";
        //        ///string file = "C:\\backup.sql";
        //        if (this.OpenConnection() == true)
        //        {
        //            using (MySqlCommand cmd = new MySqlCommand())
        //            {
        //                using (MySqlBackup mb = new MySqlBackup(cmd))
        //                {
        //                    cmd.Connection = connection;
        //                    mb.ExportToFile(path);
        //                    this.CloseConnection();
        //                }
        //            }
        //        }
        //    }
        //    catch (IOException ex)
        //    {
        //        this.CloseConnection();
        //    }
        //}

        //Restore
        //public void Restore(string path)
        //{
        //    try
        //    {
        //        //string constring = "server=localhost;user=root;pwd=qwerty;database=test;";
        //        //string file = "C:\\backup.sql";

        //        if (this.OpenConnection() == true)
        //        {
        //            using (MySqlCommand cmd = new MySqlCommand())
        //            {
        //                using (MySqlBackup mb = new MySqlBackup(cmd))
        //                {
        //                    cmd.Connection = connection;
        //                    mb.ImportFromFile(path);
        //                    //MessageBox.Show("Import Thanh cong");
        //                    this.CloseConnection();
        //                }
        //            }
        //        }
        //    }
        //    catch (IOException ex)
        //    {
        //        this.CloseConnection();
        //    }
        //}
        //public void import(string path)
        //{
        //    try
        //    {
        //        //SERVER=localhost;DATABASE=testSEO;UID=root;PASSWORD=;
        //        List<string> lst = new List<string>();
        //        string[] arr = this.connectionString.Split(';');
        //        foreach(string item in arr)
        //        {
        //            if (item != "")
        //            {
        //                string[] temp = item.Split('=');
        //                lst.Add(temp[1]);
        //            }
        //        }
        //        StreamReader file = new StreamReader(path);
        //        string input = file.ReadToEnd();
        //        file.Close();

        //        ProcessStartInfo psi = new ProcessStartInfo();
        //        psi.FileName = "mysql";
        //        psi.RedirectStandardInput = true;
        //        psi.RedirectStandardOutput = false;
        //        psi.Arguments = string.Format(@"-u {0} -p {1} -h {2} {3}",
        //            lst[2], lst[3], lst[0], lst[1]);
        //        psi.UseShellExecute = false;


        //        Process process = Process.Start(psi);
        //        process.StandardInput.WriteLine(input);
        //        process.StandardInput.Close();
        //        process.WaitForExit();
        //        process.Close();
        //    }
        //    catch (IOException ex)
        //    {
                
        //    }
        //}
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using testCloneOnLinux.Data;
using testCloneOnLinux.Models;
using System.Diagnostics;
using SshNet;
using Renci.SshNet;
using MySql.Data.MySqlClient;
using MySql;

namespace testCloneOnLinux.Controllers
{
    [Produces("application/json")]
    [Route("api/GenerateSources")]
    public class GenerateSourcesController : Controller
    {
        [HttpGet("CloneProduct")]
        public async Task<Data.ObjectResult> CloneProduct(string fromLocation, string toDestination, string dbName, string mysqlUsername, string mysqlPassword, string scriptLocation)
        {
            using (var client = new SshClient("103.7.41.51", "root", "Gsoft@235"))
            {
                
                try
                {
                    client.Connect();



                    if (client.IsConnected)
                    {
                        ////Copy source tu folder goc fromLocation sang folder moi toDestination
                        //client.RunCommand($"mkdir '{toDestination}'");
                        client.RunCommand($"mkdir '{toDestination}' && cd '{fromLocation}' && cp * '{toDestination}'");
                        /*------------*/
                        //Tao Mysql User va DB, gan user quan ly DB vua tao
                        var portForwarded = new ForwardedPortLocal("127.0.0.1", 3306, "127.0.0.1", 3306);
                        client.AddForwardedPort(portForwarded);
                        portForwarded.Start();
                        DatabaseConnect mySqlConnector = new DatabaseConnect("Server=127.0.0.1; Port=3306; Uid=root; Pwd=JOpaqGH7N7xz;");
                        //string createUserandDBScript = $"CREATE DATABASE IF NOT EXISTS {dbName}; CREATE USER '{mysqlUsername}'@'localhost' IDENTIFIED BY '{mysqlPassword}'; GRANT ALL PRIVILEGES ON  {dbName}. * TO '{mysqlUsername}'@'localhost';FLUSH PRIVILEGES;";

                        string createDatabaseScript = $"CREATE DATABASE IF NOT EXISTS {dbName};";
                        string createUserScript = $"CREATE USER '{mysqlUsername}'@'localhost' IDENTIFIED BY '{mysqlPassword}';";
                        string grantPermission = $"GRANT ALL PRIVILEGES ON  {dbName}. * TO '{mysqlUsername}'@'localhost';";
                        string resetPermissionTable = $"FLUSH PRIVILEGES;";

                        Data.ObjectResult result = new Data.ObjectResult();
                        //khoi tao ket noi MySQL
                        result = mySqlConnector.OpenConnection();
                        if(result.isSucceeded == false)
                        {
                            return result;
                        }
                        
                        result = await mySqlConnector.ExecuteCommand(createDatabaseScript);
                        if (result.isSucceeded == false)
                        {
                          //  result.ErrorMessage = "Lỗi xảy ra khi tạo DB ";
                            return result;
                        }
                        result = await mySqlConnector.ExecuteCommand(createUserScript);
                        if (result.isSucceeded == false)
                        {
                           // result.ErrorMessage= "Không thể tạo User Mysql";
                            return result;
                        }
                        result = await mySqlConnector.ExecuteCommand(grantPermission);
                        if (result.isSucceeded == false)
                        {
                           // result.ErrorMessage = $"Không thể gán quyền user {mysqlUsername} cho Database {dbName}";
                            return result;
                        }
                        result = await mySqlConnector.ExecuteCommand(resetPermissionTable);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }
                        result = await mySqlConnector.ExecuteCommand($"USE {dbName};");
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }
                    

                        //execute file script vao DB vua tao
                        SshCommand getScriptFileCommand = client.CreateCommand($"cat '{scriptLocation}'");

                        string scriptContent = getScriptFileCommand.Execute();
                        if (!String.IsNullOrEmpty(scriptContent))
                        {
                            result = await mySqlConnector.ExecuteCommand(scriptContent);
                            if (result.isSucceeded == false)
                            {
                                return result;
                            }
                        }


                        //Tao Subdomain
                        string useCWP = "USE root_cwp;";
                        result = await mySqlConnector.ExecuteCommand(useCWP);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }

                        string insertSubdomain = $"Insert into subdomains (subdomain,domain,user,path,setup_time) values('subdomain','gwebsite.net','gwebsite','home/gwebsite/public_html/subdomain','{DateTime.Now}')";
                        result = await mySqlConnector.ExecuteCommand(insertSubdomain);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }

                        mySqlConnector.CloseConnection();
                        getScriptFileCommand.Dispose();
                        //hieu chinh file wp-config
                        string configFileLocation = toDestination + "/wp-config.php";
                        SshCommand getWordpressConfigFile = client.CreateCommand($"cat '{configFileLocation}'");
                        string wpconfigContent = getWordpressConfigFile.Execute();
                        if (!String.IsNullOrEmpty(wpconfigContent))
                        {
                            wpconfigContent = wpconfigContent.Replace("define('DB_NAME','');", $"define('DB_NAME','{dbName}');");
                            wpconfigContent = wpconfigContent.Replace("define('DB_USER','');", $"define('DB_USER','{mysqlUsername}');");
                            wpconfigContent = wpconfigContent.Replace("define('DB_PASSWORD','');", $"define('DB_PASSWORD','{mysqlPassword}');");
                            getWordpressConfigFile.Dispose();
                            using (SftpClient sftpClient = new SftpClient("103.7.41.51", 22, "root", "Gsoft@235"))
                            {
                                sftpClient.Connect();
                                if (sftpClient.IsConnected)
                                {
                                    try
                                    {
                                        using (MemoryStream memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(wpconfigContent)))
                                        {

                                            try
                                            {
                                                sftpClient.UploadFile(memStream, configFileLocation, true);
                                            }
                                            catch (Exception ex)
                                            {
                                                Data.ObjectResult rs = new Data.ObjectResult();
                                                rs.isSucceeded = false;
                                                rs.ErrorMessage = "Có lỗi xảy ra khi cập nhật lại file wp-config - " + ex.Message;
                                                return rs;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Data.ObjectResult rs = new Data.ObjectResult();
                                        rs.isSucceeded = false;
                                        rs.ErrorMessage = "Có lỗi xảy ra khi tạo file wp-config - " + ex.Message;
                                        return rs;
                                    }
                                }
                            }
                        }




                        client.Disconnect();
                    }
                    else
                    {
                        Data.ObjectResult objrs = new Data.ObjectResult();
                        objrs.isSucceeded = false;
                        objrs.ErrorMessage = "Kết nối SSH đến VPS thất bại";
                        return objrs;
                    }
                    Data.ObjectResult objResult = new Data.ObjectResult();
                    objResult.isSucceeded = true;
                    objResult.ErrorMessage = "Clone source thành công";

                    return objResult;
                }
                catch (Exception ex)
                {
                    Data.ObjectResult objrs = new Data.ObjectResult();
                    objrs.isSucceeded = false;
                    objrs.ErrorMessage = "Lỗi - " + ex.Message;
                    return objrs;
                }
                finally
                {
                    client.Disconnect();
                }
            }

        }

        [HttpPost]
        public async Task<Data.ObjectResult> Post([FromBody] MyModelGen modelGen)
        {
            using (var client = new SshClient("103.7.41.51", "root", "Gsoft@235"))
            {
                try
                {
                    client.Connect();


                    if (client.IsConnected)
                    {
                        ////Copy source tu folder goc fromLocation sang folder moi toDestination

                       // client.RunCommand($"mkdir '{modelGen.Destination}' && cd '{modelGen.Source}' && cp * '{modelGen.Destination}'");
                        client.RunCommand($"mkdir '{modelGen.Destination}' && cp -a '{modelGen.Source + "/."}' '{modelGen.Destination}'");
                        /*------------*/
                        //Khoi tao ket noi vao MySqL
                        var portForwarded = new ForwardedPortLocal("127.0.0.1", 0, "127.0.0.1", 3306);
                        client.AddForwardedPort(portForwarded);
                        portForwarded.Start();

                        //  DatabaseConnect mySqlConnector = new DatabaseConnect("Server=127.0.0.1; Port=3306; Uid=root; Pwd=JOpaqGH7N7xz;");

                        DatabaseConnect mySqlConnector = new DatabaseConnect(modelGen.MySQlConnectionString);
                       // string createUserandDBScript = $"CREATE DATABASE IF NOT EXISTS {modelGen.DatabaseName}; CREATE USER '{modelGen.DatabaseUser}'@'localhost' IDENTIFIED BY '{modelGen.Password}'; GRANT ALL PRIVILEGES ON  {modelGen.DatabaseName}. * TO '{modelGen.DatabaseUser}'@'localhost';FLUSH PRIVILEGES;";
                        Data.ObjectResult result = new Data.ObjectResult();
                        result = mySqlConnector.OpenConnection();
                        if (result.isSucceeded == false)
                        {
                            //result.ErrorMessage = "Lỗi không tạo được kết nối đến MySQL";
                            return result;
                        }


                        

                        string createDatabaseScript = $"CREATE DATABASE IF NOT EXISTS {modelGen.DatabaseName};";
                        string createUserScript = $"CREATE USER '{modelGen.DatabaseUser}'@'localhost' IDENTIFIED BY '{modelGen.Password}';";
                        string grantPermission = $"GRANT ALL PRIVILEGES ON  {modelGen.DatabaseName}.* TO '{modelGen.DatabaseUser}'@'localhost' IDENTIFIED BY '{modelGen.Password}';";
                        string resetPermissionTable = $"FLUSH PRIVILEGES;";



                        result = await mySqlConnector.ExecuteCommand(createDatabaseScript);
                        if (result.isSucceeded == false)
                        {
                            
                            return result;
                        }

                        result = await mySqlConnector.ExecuteCommand(createUserScript);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }

                        result = await mySqlConnector.ExecuteCommand(grantPermission);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }

                        result = await mySqlConnector.ExecuteCommand(resetPermissionTable);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }
                        //isSucceeded = await mySqlConnector.ExecuteCommand(createUserandDBScript);
                        //if (isSucceeded == false)
                        //{
                        //    return "Lỗi xảy ra khi tạo DB và User";
                        //}
                        //Import du lieu vao DB vua tao
                        if (!String.IsNullOrEmpty(modelGen.ScriptLocation))
                        {
                            result = await mySqlConnector.ExecuteCommand($"USE {modelGen.DatabaseName};");
                            if (result.isSucceeded == false)
                            {
                                return result;
                            }

                            //execute file script vao DB vua tao
                            SshCommand getScriptFileCommand = client.CreateCommand($"cat '{modelGen.ScriptLocation}'");
                            string scriptContent = getScriptFileCommand.Execute();
                            if (!String.IsNullOrEmpty(scriptContent))
                            {
                                result = await mySqlConnector.ExecuteCommand(scriptContent);
                                if (result.isSucceeded == false)
                                {
                                    return result;
                                }
                            }
                        }
                        string useCWP = "USE root_cwp;";
                        result = await mySqlConnector.ExecuteCommand(useCWP);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }

                        string insertSubdomain = $"Insert into subdomains (subdomain,domain,user,path,setup_time) values('{modelGen.Subdomain}','{modelGen.Domain}','gwebsite','{modelGen.Destination}','{DateTime.Now}')";
                        result = await mySqlConnector.ExecuteCommand(insertSubdomain);
                        if (result.isSucceeded == false)
                        {
                            return result;
                        }
                        //string switchToCWPDatabase = "USE cwp_root;";

                        // string createSubdommainScript = "";
                        //mySqlConnector.ExecuteCommand("");
                        //Dong ket noi MySQL
                        mySqlConnector.CloseConnection();
                        //hieu chinh file wp-config
                        
                        string configFileLocation = modelGen.Destination + "/wp-config.php";
                        SshCommand getWordpressConfigFile = client.CreateCommand($"cat '{configFileLocation}'");
                        string wpconfigContent = getWordpressConfigFile.Execute();
                        if (!String.IsNullOrEmpty(wpconfigContent))
                        {
                            wpconfigContent = wpconfigContent.Replace("define('DB_NAME','');", $"define('DB_NAME','{modelGen.DatabaseName}');");
                            wpconfigContent = wpconfigContent.Replace("define('DB_USER','');", $"define('DB_USER','{modelGen.DatabaseUser}');");
                            wpconfigContent = wpconfigContent.Replace("define('DB_PASSWORD','');", $"define('DB_PASSWORD','{modelGen.Password}');");

                            using (SftpClient sftpClient = new SftpClient("103.7.41.51", 22, "root", "Gsoft@235"))
                            {
                                sftpClient.Connect();
                                if (sftpClient.IsConnected)
                                {
                                    try
                                    {
                                        using (MemoryStream memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(wpconfigContent)))
                                        {

                                            try
                                            {
                                                sftpClient.UploadFile(memStream, configFileLocation, true);
                                            }
                                            catch (Exception ex)
                                            {
                                                Data.ObjectResult objRS = new Data.ObjectResult();
                                                objRS.isSucceeded = false;
                                                objRS.ErrorMessage = "Có lỗi xảy ra khi cập nhật lại file wp-config";
                                                return objRS;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Data.ObjectResult objRS = new Data.ObjectResult();
                                        objRS.isSucceeded = false;
                                        objRS.ErrorMessage = "Có lỗi xảy ra khi tạo file wp-config - " + ex.Message;
                                        return objRS;
                                    }
                                }
                            }
                        }
                        ////Tao MYSQL DB va MYSQL USER -> grant full quyen cua user cho database
                        //var portForwarded = new ForwardedPortLocal("127.0.0.1", 3306, "127.0.0.1", 3306);
                        //client.AddForwardedPort(portForwarded);
                        //portForwarded.Start();
                        //using (MySqlConnection con = new MySqlConnection("Server=127.0.0.1; Port=3306; Uid=root; Pwd=JOpaqGH7N7xz;"))
                        //{
                        //    con.Open();
                        //    MySqlCommand mySQLCmd = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS {dbName}; CREATE USER '{mysqlUsername}'@'localhost' IDENTIFIED BY '{mysqlPassword}'; GRANT ALL PRIVILEGES ON  {dbName}. * TO '{mysqlUsername}'@'localhost';FLUSH PRIVILEGES;", con);
                        //    int result = mySQLCmd.ExecuteNonQuery();
                        //    con.Close();

                        //}
                        client.Disconnect();
                    }
                    else
                    {
                        Data.ObjectResult result = new Data.ObjectResult();
                        result.isSucceeded = false;
                        result.ErrorMessage = "Kết nối SSH đến VPS thất bại";
                        return result;
                    }
                    Data.ObjectResult rs = new Data.ObjectResult();
                    rs.isSucceeded = true;
                    rs.ErrorMessage = "Clone sản phẩm thành công";
                    return rs;
                }
                catch (Exception ex)
                {
                    Data.ObjectResult rs = new Data.ObjectResult();
                    rs.isSucceeded = false;
                    rs.ErrorMessage = "Lỗi - " + ex.Message;
                    return rs;
                }
                finally
                {
                    client.Disconnect();
                }
            }

        }
        //private bool CopySource(string fromLocation, string toDestination)
        //{
        //    string navigateCommand = $"cd '" + fromLocation + "'" ;
        //    string coppyCommand = $"cp * '" + toDestination + "'";

        //}
        //[HttpGet]
        //[Route("GenerateMySQLDatabase/{productCode}")]
        //private async Task<bool> GenerateMySQLDatabase(string databaseName
        //    , string sourceLocation
        //    , string mySqlConnectionString
        //    , string userName
        //    , string password)
        //{
        //    try
        //    {
        //        DatabaseConnect mySqlConnector = new DatabaseConnect(mySqlConnectionString);

        //        string createDatabaseScript = $"DROP DATABASE IF EXISTS `{databaseName}`;  CREATE DATABASE IF NOT EXISTS `{databaseName}`;  CREATE USER '{userName}'@'localhost' IDENTIFIED BY '{password}'; GRANT ALL PRIVILEGES ON  {databaseName}. * TO '{userName}'@'localhost';FLUSH PRIVILEGES; USE `{databaseName}`;";

        //        //string createDatabaseScript = $"DROP DATABASE IF EXISTS `{databaseName}`;  CREATE DATABASE IF NOT EXISTS `{databaseName}`; USE `{databaseName}`; ";
        //        //string scriptTable = System.IO.File.ReadAllText(@"E:\Lib\Ky 2 nam 4\Wordpress_DB\wordpress.sql");
        //        string scriptTable = System.IO.File.ReadAllText(sourceLocation + "/scriptDB/scriptdb.sql");
        //        string fullScript = createDatabaseScript + " " + scriptTable;
        //        if (await mySqlConnector.ExecuteCommand(fullScript))
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }


        //}

        //[HttpPost]
        //public async Task<ActionResult> Post([FromBody]MyModelGen model)
        //{
        //    var isSQLSuccess = true;
        //    var messageSql = "";
        //    try
        //    {
        //        if (!Directory.Exists(model.Source))
        //        {
        //            return Json(new
        //            {
        //                Message = "Nhập sai đường dẫn nguồn",
        //                Data = "",
        //                IsSQLSuccess = false,
        //                MessageSql = ""
        //        });
        //        }
        //        if (!Directory.Exists(model.Destination))
        //        {
        //            Directory.CreateDirectory(model.Destination);
        //        }
        //        this.Copy(model.Source, model.Destination);
        //        isSQLSuccess = await this.GenerateMySQLDatabase(model.DatabaseName
        //            , model.Source
        //            , model.MySQlConnectionString
        //            , model.DatabaseUser
        //            , model.Password);

        //        if (isSQLSuccess)
        //        {
        //            messageSql = "Gen mysql thành công!";
        //        }
        //        else
        //        {
        //            messageSql = "Gen mysql thất bại!";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            Message = ex.Message,
        //            Data = ex,
        //            IsSQLSuccess = isSQLSuccess,
        //            MessageSql = messageSql,
        //        });
        //    }
        //    return Json(new
        //    {
        //        Message = "Sao chép thành công!",
        //        IsSQLSuccess = isSQLSuccess,
        //        MessageSql = messageSql,
        //        Data = ""
        //    });
        //}

        //private void Copy(string sourceDirectory, string targetDirectory)
        //{
        //    DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        //    DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        //    CopyAll(diSource, diTarget);
        //}

        //private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        //{
        //    Directory.CreateDirectory(target.FullName);

        //    // Copy each file into the new directory.
        //    foreach (FileInfo fi in source.GetFiles())
        //    {
        //        // Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
        //        fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        //    }

        //    // Copy each subdirectory using recursion.
        //    foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        //    {
        //        DirectoryInfo nextTargetSubDir =
        //            target.CreateSubdirectory(diSourceSubDir.Name);
        //        CopyAll(diSourceSubDir, nextTargetSubDir);
        //    }
        //}
        //private void DeleteFolder(string path)
        //{
        //    System.IO.DirectoryInfo di = new DirectoryInfo(path);

        //    foreach (FileInfo file in di.GetFiles())
        //    {
        //        file.Delete();
        //    }
        //    foreach (DirectoryInfo dir in di.GetDirectories())
        //    {
        //        dir.Delete(true);
        //    }
        //    di.Delete();
        //}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using testCloneOnLinux.Models;
using Renci.SshNet;
using testCloneOnLinux.Data;
using testCloneOnLinux.ViewModels;
using System.Net.Http;
using Newtonsoft.Json;
namespace testCloneOnLinux.Controllers
{
    [Produces("application/json")]
    [Route("api/TenantControl")]
    public class TenantControlController : Controller
    {
       
        [HttpPost]
        [Route("UpdateOperationState")]
        public async Task<Data.ObjectResult> Post([FromBody] WebControlViewModel webControlViewModel)
        {
            HttpClient apiClient = new HttpClient();
            IEnumerable<CmAllcode> listMySqlConn;
            try
            {


                Uri address= new Uri("http://103.7.41.51:9823/api/AllCode/MYSQL_CONN");
                string jsonString = await apiClient.GetStringAsync(address);
                listMySqlConn = JsonConvert.DeserializeObject<IEnumerable<CmAllcode>>(jsonString);


            }
            catch (Exception ex)
            {
                listMySqlConn = null;
                Data.ObjectResult result = new Data.ObjectResult();
                result.isSucceeded = false;
                result.ErrorMessage = "Lỗi trong quá trình tạo kết nối đến MySQL - " + ex.Message;
                return result;
            }
            finally
            {
                apiClient.Dispose();
            }
            using (var client = new SshClient("103.7.41.51", "root", "Gsoft@235"))
            {
                
                try
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        var portForwarded = new ForwardedPortLocal("127.0.0.1", 0, "127.0.0.1", 3306);
                        client.AddForwardedPort(portForwarded);
                        portForwarded.Start();
                        string mySQLConnectionString = "";
                        if (listMySqlConn != null)
                        {
                            mySQLConnectionString = listMySqlConn.ToList()[0].Content;
                        }
                        DatabaseConnect mySqlConnector = new DatabaseConnect(mySQLConnectionString);

                        Data.ObjectResult result = new Data.ObjectResult();
                        result = mySqlConnector.OpenConnection();
                        if (result.isSucceeded == false)
                        {
                            mySqlConnector.CloseConnection();
                            return result;
                        }
                        string useDatabase = $"USE {webControlViewModel.PrjProjectMaster.DATABASE_NAME};";
                        result = await mySqlConnector.ExecuteCommand(useDatabase);
                        if (result.isSucceeded == false)
                        {
                            mySqlConnector.CloseConnection();
                            return result;
                        }
                        string warningPage = "http://warning.gwebsite.net";
                        //HttpClient getWarningClient = new HttpClient();
                        //IEnumerable<CmAllcode> warningURL = null;
                        //try
                        //{
                        //    Uri address = new Uri("http://103.7.41.51:9823/api/AllCode/WARNING_URL");
                        //    string jsonString = await apiClient.GetStringAsync(address);
                        //    warningURL = JsonConvert.DeserializeObject<IEnumerable<CmAllcode>>(jsonString);
                        //    if (warningURL != null)
                        //    {
                        //        warningPage = warningURL.ToList()[0].Content;
                        //    }
                        //}
                        //catch (Exception ex)
                        //{

                        //    result = new Data.ObjectResult();
                        //    result.isSucceeded = false;
                        //    result.ErrorMessage = "Lỗi trong quá trình lấy dữ liệu - " + ex.Message;
                        //    return result;
                        //}
                        //finally
                        //{
                        //    getWarningClient.Dispose();
                        //    warningURL = null;
                        //}
                        string siteURL = "";
                        //OPERATION_STATE = 1 nghia la Website dang hoat dong (kiem tra bang CM_ALLCODE)
                        if (webControlViewModel.CwWebControl.OPERATION_STATE.Equals("1"))
                        {
                            siteURL = "http://" + webControlViewModel.PrjProjectMaster.SUB_DOMAIN + "." + webControlViewModel.PrjProjectMaster.DOMAIN;
                        }
                        //OPERATION_STATE = 0 nghia la Website tam dung hoat dong, redirect ve trang warning.gwebsite.net (kiem tra bang CM_ALLCODE)
                        else if (webControlViewModel.CwWebControl.OPERATION_STATE.Equals("0"))
                        {
                            siteURL = warningPage;
                        }
                        string updateSiteURLQuery = $"Update wp_options SET option_value = '{siteURL}' WHERE option_name = 'siteurl'";
                        result = await mySqlConnector.ExecuteCommand(updateSiteURLQuery);
                        if (result.isSucceeded == false)
                        {
                            mySqlConnector.CloseConnection();
                            return result;
                        }
                        string updateHomeQuery = $"Update wp_options SET option_value = '{siteURL}' WHERE option_name = 'home'";
                        result = await mySqlConnector.ExecuteCommand(updateHomeQuery);
                        if (result.isSucceeded == false)
                        {
                            mySqlConnector.CloseConnection();
                            return result;

                        }

                        mySqlConnector.CloseConnection();
                        result.isSucceeded = true;
                        result.ErrorMessage = $"Chuyển trạng thái hoạt động của website {webControlViewModel.PrjProjectMaster.SUB_DOMAIN + "." + webControlViewModel.PrjProjectMaster.DOMAIN} thành công";
                        return result;
                    }
                    else
                    {
                        Data.ObjectResult result = new Data.ObjectResult();
                        result.isSucceeded = false;
                        result.ErrorMessage = "Lỗi kết nối SSH";
                        return result;
                    }
                    
                }
                catch (Exception ex)
                {
                    Data.ObjectResult rs = new Data.ObjectResult();
                    rs.isSucceeded = false;
                    rs.ErrorMessage = "Lỗi - " + ex.Message;
                    return rs;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using testCloneOnLinux.Models;
using Renci.SshNet;
using System.IO;
using testCloneOnLinux.ViewModels;

namespace testCloneOnLinux.Controllers
{
    [Produces("application/json")]
    [Route("api/PluginDistribution")]
    public class PluginDistributionController : Controller
    {
        [HttpPost]
        public async Task<Data.ObjectResult> Post([FromBody] List<PrjInstalledPluginViewModel> listModel)
        {
            using (var client = new SshClient("103.7.41.51", "root", "Gsoft@235"))
            {
                try
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(300);
                    client.Connect();
                    if (client.IsConnected)
                    {
                        foreach(var item in listModel)
                        {
                            string pluginDirectoryName = new DirectoryInfo(item.PrdPlugin.PluginLocation).Name;
                            client.RunCommand("rm -rf home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins/" + pluginDirectoryName);
                            if (item.IsChecked)
                            {
                                client.RunCommand($"mkdir '{"/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins/" + pluginDirectoryName}' && cp -a '{item.PrdPlugin.PluginLocation + "/."}' '{"/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins/" + pluginDirectoryName}'");

                                //Fix permission cua tat ca folder ve 0755 va cac file ve 0644
                                //string chmodCommand = "find " + "'/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + @"' -type d -exec chmod 0755 {} \;";
                                //client.RunCommand(chmodCommand);
                                string chmodAllSubFolder = "find " + "'/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins/.'" + @" -type d -exec chmod 0755 {} \;";
                                client.RunCommand(chmodAllSubFolder);
                                string chmodAllFile = "find " + "'/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins/.'" + @" -type f -exec chmod 0644 {} \;";
                                client.RunCommand(chmodAllFile);
                                //Fix permission chuyen owner cua tat ca cac file va folder source tu root -> gwebsite
                                string chownFolder = "chown gwebsite:gwebsite " + "'/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins'";
                                client.RunCommand(chownFolder);
                                string chownAllContent = "chown -R gwebsite:gwebsite " + "'/home/gwebsite/public_html/" + item.PrjInstalledPlugin.SUBDOMAIN + "/wp-content/plugins'";
                                client.RunCommand(chownAllContent);
                            }
                        }


                        client.Disconnect();
                        Data.ObjectResult result = new Data.ObjectResult();
                        result.isSucceeded = false;
                        result.ErrorMessage = "Cập nhật và cài đặt plugin thành công!";
                        return result;
                    }
                    else
                    {
                        client.Disconnect();
                        Data.ObjectResult result = new Data.ObjectResult();
                        result.isSucceeded = false;
                        result.ErrorMessage = "Kết nối SSH đến VPS thất bại";
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    client.Disconnect();
                    Data.ObjectResult result = new Data.ObjectResult();
                    result.isSucceeded = false;
                    result.ErrorMessage = "Đã có lỗi xảy ra - " + ex.Message;
                    return result;
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }
    }
}
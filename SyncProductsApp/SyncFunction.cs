using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProductsFunctionApp.Models;

namespace SyncProductsApp
{
    //Azure Functions used for integration two different API's to update the system. 
    //Azure function calls the system A api and read's the latest information and update it to system B via API. 
    //Same way Azure function calls the system B api and read's the latest information and update it to system A via API.

    public class SyncFunction
    {
        [FunctionName("UpdateLatestInformation")]
        public void Run([TimerTrigger("0 0 6,19 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");


            List<SysAProduct> Sys1api = GetSysAProduct().Result;
            List<SysBProduct> Sys2api = GetSysBProduct().Result;



            log.LogInformation($"Api 1 Count: {Sys1api.Count}");
            log.LogInformation($"Api 2 Count: {Sys2api.Count}");
            if (Sys1api.Count != Sys2api.Count) //Check If total no of items in Sys A is not equal to Sys B
            {
                if (Sys1api.Count > Sys2api.Count) // If sys A(no.of.items) is greater than sys B(no.of.items)
                {
                    int diff_of_productslist = Sys1api.Count - Sys2api.Count;


                    // Order by Productid but descending
                    List<SysAProduct> DescSortedList = Sys1api.OrderByDescending(o => o.ProductId).ToList();


                    foreach (var item in DescSortedList)
                    {
                        log.LogInformation($"Object yet to update: {diff_of_productslist}");
                        if (diff_of_productslist == 0)
                        {
                            log.LogInformation($"Updated Successfully");
                            break;
                        }
                        //Update Products to Sys B Api 
                        SysBProduct sysBobj = new SysBProduct()
                        {
                            ProductName = item.ProductName,
                            Quantity = Convert.ToInt32(item.Stock),
                            Price = Convert.ToInt32(item.Price),
                            Category = 1
                        };
                        bool res1 = PostSysAToSysB(sysBobj).Result;


                        diff_of_productslist--;

                    }


                }
                else
                {
                    int diff_of_productslist = Sys2api.Count - Sys1api.Count;



                    // Order by Productid but descending
                    List<SysBProduct> DescSortedList = Sys2api.OrderByDescending(o => o.ProductId).ToList();


                    foreach (var item in DescSortedList)
                    {

                        log.LogInformation($"Item yet to updated: {diff_of_productslist}");
                        if (diff_of_productslist == 0)
                        {

                            log.LogInformation($"Updated Successfully");
                            break;
                        }
                        //Update Products to Sys A Api 
                        SysAProduct sysAobj = new SysAProduct()
                        {
                            ProductName = item.ProductName,
                            Stock = item.Quantity.ToString(),
                            Price = item.Price.ToString(),

                        };
                        bool res2 = PostSysBToSysA(sysAobj).Result;


                        diff_of_productslist--;

                    }
                }
            }
            else
            {
                log.LogInformation($"If Sys A count is not equal to Sys B ");
                bool isUpdateforSysB = true;
                foreach (var api1 in Sys1api)
                {
                    
                    foreach(var api2 in Sys2api)
                    {
                        if(api1.ProductName == api2.ProductName)
                        {
                            isUpdateforSysB = false;
                            break;
                            
                        }
                        else
                        {
                            isUpdateforSysB = true;
                        }

                    }
                    if (isUpdateforSysB)
                    {
                        SysBProduct sysBobj = new SysBProduct()
                        {
                            ProductName = api1.ProductName,
                            Quantity = Convert.ToInt32(api1.Stock),
                            Price = Convert.ToInt32(api1.Price),
                            Category = 1
                        };
                        bool res1 = PostSysAToSysB(sysBobj).Result;
                        log.LogInformation($"Sys B Updated Successfully");
                    }

                }
                


                bool isUpdateforSysA = true;
                foreach (var api2 in Sys2api)
                {
                   
                    foreach (var api1 in Sys1api)
                    {
                        if (api2.ProductName == api1.ProductName)
                        {
                            isUpdateforSysA = false;
                            break;
                            
                        }
                        else
                        {
                            isUpdateforSysA=true;
                        }
                    
                    }
                    if (isUpdateforSysA)
                    {
                        SysAProduct sysAobj = new SysAProduct()
                        {
                            ProductName = api2.ProductName,
                            Stock = api2.Quantity.ToString(),
                            Price = api2.Price.ToString(),

                        };
                        bool res2 = PostSysBToSysA(sysAobj).Result;
                        log.LogInformation($"Sys A Updated Successfully");

                    }
                }
                
            }

        }





        public async Task<List<SysAProduct>> GetSysAProduct()
        {
            List<SysAProduct> Sys1api = new List<SysAProduct>();

            //System 1 API call - Get Products
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri("https://apisys1.azurewebsites.net");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage Res = await client.GetAsync("/api/products");

                if (Res.IsSuccessStatusCode)
                {
                    var sysAproductApiResponse = Res.Content.ReadAsStringAsync().Result;
                    Sys1api = JsonConvert.DeserializeObject<List<SysAProduct>>(sysAproductApiResponse);

                }

            }
            return Sys1api;
        }
        public async Task<List<SysBProduct>> GetSysBProduct()
        {
            List<SysBProduct> Sys2api = new List<SysBProduct>();
            //System 2 API call - Get Products
            using (var client = new HttpClient())
            {

                client.BaseAddress = new Uri("https://apisys2.azurewebsites.net");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage Res = await client.GetAsync("/api/products");

                if (Res.IsSuccessStatusCode)
                {
                    var sysBproductApiResponse = Res.Content.ReadAsStringAsync().Result;
                    Sys2api = JsonConvert.DeserializeObject<List<SysBProduct>>(sysBproductApiResponse);

                }
            }
            return Sys2api;
        }

        public async Task<bool> PostSysAToSysB(SysBProduct sysBobj)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://apisys2.azurewebsites.net");
                StringContent content = new StringContent(JsonConvert.SerializeObject(sysBobj), Encoding.UTF8, "application/json");
                //Post products to Sys B Api
                using (var response = await httpClient.PostAsync("/api/products", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //sysBobj = JsonConvert.DeserializeObject<SysBProduct>(apiResponse);
                }
            }
            return true;
        }
        public async Task<bool> PostSysBToSysA(SysAProduct sysAobj)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://apisys1.azurewebsites.net");
                StringContent content = new StringContent(JsonConvert.SerializeObject(sysAobj), Encoding.UTF8, "application/json");
                //Post products to Sys A Api
                using (var response = await httpClient.PostAsync("/api/products", content))
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    //sysAobj = JsonConvert.DeserializeObject<SysAProduct>(apiResponse);
                }
            }
            return true;
        }
    }
}
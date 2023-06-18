using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FunctionApp_Example
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Retrieve query parameters from the RequestUri
            var queryParameters = HttpUtility.ParseQueryString(req.QueryString.ToString());
            string owner = queryParameters["owner"];
            string repo = queryParameters["repo"];

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return new BadRequestObjectResult("Vui lòng cung cấp thông tin chính xác về repository.");
            }

            // Khởi tạo GitHubClient với Personal Access Token
            string githubAccessToken = "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";
            var githubClient = new GitHubClient(new ProductHeaderValue("Azure-Function-GitHub"));
            githubClient.Credentials = new Credentials(githubAccessToken);

            try
            {
                // Gửi yêu cầu API để lấy danh sách commit
                var commits = await githubClient.Repository.Commit.GetAll(owner, repo);

                // Xử lý dữ liệu commit và tạo thông báo Microsoft Teams
                StringBuilder teamsMessageBuilder = new StringBuilder();
                teamsMessageBuilder.AppendLine("Danh sách commit:");

                foreach (var commit in commits)
                {
                    teamsMessageBuilder.AppendLine($"- {commit.Sha.Substring(0, 7)}: {commit.Commit.Message}");
                }

                string teamsMessage = teamsMessageBuilder.ToString();

                // Gửi thông báo đến Microsoft Teams
                string teamsWebhookUrl = "https://storai.webhook.office.com/webhookb2/248ada60-dab6-4779-9bc7-f229ed5811e8@6e40d558-bf93-4d3a-8723-948132358ceb/IncomingWebhook/46905151f02841e09292d37d4152c906/77de5f65-8817-4ee0-ab73-2962f57c557a";
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://storai.webhook.office.com/");
                var payload = new { text = teamsMessage };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return new OkObjectResult("Lấy lịch sử commit và gửi thông báo thành công.");
                }
                else
                {
                    return new StatusCodeResult((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                return new ObjectResult("Đã xảy ra lỗi khi lấy lịch sử commit: " + ex.Message)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}


//comment check
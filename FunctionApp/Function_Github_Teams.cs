using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FunctionApp_Example
{
    public static class Function_Github_Teams
    {
        [FunctionName("Function_Github_Teams")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var queryParameters = HttpUtility.ParseQueryString(req.QueryString.ToString());
            string owner = queryParameters["owner"];
            string repo = queryParameters["repo"];

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return new BadRequestObjectResult("Vui lòng cung cấp thông tin chính xác về repository.");
            }

            string githubAccessToken = "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";
            var githubClient = new GitHubClient(new ProductHeaderValue("Azure-Function-GitHub"));
            githubClient.Credentials = new Credentials(githubAccessToken);

            try
            {
                var commits = await githubClient.Repository.Commit.GetAll(owner, repo);

                var latestCommit = commits.OrderByDescending(c => c.Commit.Author.Date).FirstOrDefault();
                if (latestCommit != null)
                {
                    var commitInfo = await githubClient.User.Get(latestCommit.Author.Login);

                    StringBuilder teamsMessageBuilder = new StringBuilder();

                    teamsMessageBuilder.AppendLine("***Người commit:*** " + commitInfo.Name + commitInfo.Login);
                    teamsMessageBuilder.AppendLine(); // Xuống dòng
                    teamsMessageBuilder.AppendLine("***Nội dung commit:*** " + latestCommit.Commit.Message); //định dạng Markdown

                    string teamsMessage = teamsMessageBuilder.ToString();

                    string teamsWebhookUrl = "https://storai.webhook.office.com/webhookb2/248ada60-dab6-4779-9bc7-f229ed5811e8@6e40d558-bf93-4d3a-8723-948132358ceb/IncomingWebhook/46905151f02841e09292d37d4152c906/77de5f65-8817-4ee0-ab73-2962f57c557a";
                    var httpClient = new HttpClient();
                    var payload = new { text = teamsMessage };
                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        return new OkObjectResult("Lấy commit và gửi tin nhắn thành công.");
                    }
                    else
                    {
                        return new StatusCodeResult((int)response.StatusCode);
                    }
                }
                else
                {
                    return new OkObjectResult("Không tìm thấy commit mới.");
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

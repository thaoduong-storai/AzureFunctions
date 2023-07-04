using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionApp
{
    public static class Function_Github_Teams
    {
        [FunctionName("Function_Github_Teams")]
        public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
    ILogger log)
        {
            log.LogInformation("Function_Github_Teams is processing...");

            var queryParameters = req.Query;
            string owner = queryParameters["owner"];
            string repo = queryParameters["repo"];
            string branchName = queryParameters["branch"]; // Tên nhánh cụ thể

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(branchName))
            {
                return new BadRequestObjectResult("Please provide correct information about the repository and branch!!");
            }

            string githubAccessToken = Environment.GetEnvironmentVariable("GitHubAccessToken");
            var githubClient = new GitHubClient(new ProductHeaderValue("AzureFunctions"));
            githubClient.Credentials = new Credentials(githubAccessToken);

            try
            {
                string commitUrlFormat = "https://github.com/{0}/{1}/commit/{2}";

                StringBuilder teamsMessageBuilder = new StringBuilder();

                // Lấy thông tin branch cụ thể
                var branch = await githubClient.Repository.Branch.Get(owner, repo, branchName);

                // Lấy thông tin commit mới nhất trên branch cụ thể
                var branchCommit = await githubClient.Repository.Commit.Get(owner, repo, branch.Commit.Sha);

                var commitInfo = await githubClient.User.Get(branchCommit.Author.Login);
                string commitUrl = string.Format(commitUrlFormat, owner, repo, branchCommit.Sha);

                teamsMessageBuilder.AppendLine("***The committer on branch " + branchName + ":*** " + commitInfo.Name + commitInfo.Login);
                teamsMessageBuilder.AppendLine();
                teamsMessageBuilder.AppendLine("***Commit content:*** " + branchCommit.Commit.Message);
                teamsMessageBuilder.AppendLine();
                teamsMessageBuilder.AppendLine("[See details on Git](" + commitUrl + ")");
                teamsMessageBuilder.AppendLine();

                if (teamsMessageBuilder.Length > 0)
                {
                    string teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");

                    var httpClient = new HttpClient();
                    var payload = new { text = teamsMessageBuilder.ToString() };
                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new StatusCodeResult((int)response.StatusCode);
                    }
                }

                log.LogInformation("Function_Github_Teams completed successfully.");
                return new OkObjectResult("Get the latest commit from the branch and send the message successfully!");
            }
            catch (Exception ex)
            {
                return new ObjectResult("An error occurred while getting the commit: " + ex.Message)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}

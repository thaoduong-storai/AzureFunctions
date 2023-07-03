using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
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

            //var queryParameters = HttpUtility.ParseQueryString(req.QueryString.ToString());

            var queryParameters = req.Query;
            string owner = queryParameters["owner"];
            string repo = queryParameters["repo"];

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return new BadRequestObjectResult("Please provide correct information about the repository!!");
            }

            string githubAccessToken = Environment.GetEnvironmentVariable("GitHubAccessToken");
            var githubClient = new GitHubClient(new ProductHeaderValue("AzureFunctions"));
            githubClient.Credentials = new Credentials(githubAccessToken);

            try
            {
                string commitUrlFormat = "https://github.com/{0}/{1}/commit/{2}";

                var commits = await githubClient.Repository.Commit.GetAll(owner, repo);

                var latestCommit = commits.OrderByDescending(c => c.Commit.Author.Date).FirstOrDefault();
                if (latestCommit != null)
                {
                    var commitInfo = await githubClient.User.Get(latestCommit.Author.Login);

                    StringBuilder teamsMessageBuilder = new StringBuilder();

                    string commitUrl = string.Format(commitUrlFormat, owner, repo, latestCommit.Sha);

                    teamsMessageBuilder.AppendLine("***The commiter:*** " + commitInfo.Name + commitInfo.Login);
                    teamsMessageBuilder.AppendLine();
                    teamsMessageBuilder.AppendLine("***Commit content:*** " + latestCommit.Commit.Message);
                    teamsMessageBuilder.AppendLine();
                    teamsMessageBuilder.AppendLine("[See details on Git](" + commitUrl + ")");

                    string teamsMessage = teamsMessageBuilder.ToString();

                    string teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");

                    var httpClient = new HttpClient();
                    var payload = new { text = teamsMessage };
                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        log.LogInformation("Function_Github_Teams completed successfully.");
                        return new OkObjectResult("Get the commit and send the message successfully!");
                    }
                    else
                    {
                        return new StatusCodeResult((int)response.StatusCode);
                    }
                }
                else
                {
                    return new OkObjectResult("New commit not found!!");
                }
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
//check

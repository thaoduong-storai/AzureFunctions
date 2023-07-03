﻿using Microsoft.AspNetCore.Http;
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

                var branches = await githubClient.Repository.Branch.GetAll(owner, repo);

                StringBuilder teamsMessageBuilder = new StringBuilder();

                foreach (var branch in branches)
                {
                    var commits = await githubClient.Repository.Commit.GetAll(owner, repo);

                    foreach (var commit in commits)
                    {
                        var commitInfo = await githubClient.User.Get(commit.Author.Login);

                        string commitUrl = string.Format(commitUrlFormat, owner, repo, commit.Sha);

                        teamsMessageBuilder.AppendLine("***The committer:*** " + commitInfo.Name + commitInfo.Login);
                        teamsMessageBuilder.AppendLine();
                        teamsMessageBuilder.AppendLine("***Commit content:*** " + commit.Commit.Message);
                        teamsMessageBuilder.AppendLine();
                        teamsMessageBuilder.AppendLine("[See details on Git](" + commitUrl + ")");

                        teamsMessageBuilder.AppendLine();
                    }
                }

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
                    return new OkObjectResult("Get the commits and send the messages successfully!");
                }
                else
                {
                    return new StatusCodeResult((int)response.StatusCode);
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

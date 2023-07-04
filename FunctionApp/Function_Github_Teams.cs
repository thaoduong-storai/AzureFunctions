﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using System;
using System.IO;
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

            //var queryParameters = req.Query;
            //string owner = queryParameters["owner"];
            //string repo = queryParameters["repo"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"{requestBody}");

            dynamic payload = JsonConvert.DeserializeObject(requestBody);
            string owner = payload.repository.owner.login;
            string repo = payload.repository.name;

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return new BadRequestObjectResult("Please provide correct information about the owner and repository!!");
            }

            string githubAccessToken = Environment.GetEnvironmentVariable("GitHubAccessToken");
            var githubClient = new GitHubClient(new ProductHeaderValue("AzureFunctions"));
            githubClient.Credentials = new Credentials(githubAccessToken);

            try
            {
                string commitUrlFormat = "https://github.com/{0}/{1}/commit/{2}";
                string teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");

                string sha = payload.head_commit?.id;
                string commitUrl = string.Format(commitUrlFormat, owner, repo, sha);
                string commitMessage = payload.head_commit?.message;
                string committerName = payload.head_commit?.committer.name;

                string teamsMessage = $"***The committer:*** {committerName}\n\n";
                teamsMessage += $"***Commit content:*** {commitMessage}\n\n";
                teamsMessage += $"[See details on Git]({commitUrl})\n\n";

                if (!string.IsNullOrEmpty(teamsWebhookUrl))
                {
                    var httpClient = new HttpClient();
                    var payloadData = new { text = teamsMessage };
                    var jsonPayload = JsonConvert.SerializeObject(payloadData);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new StatusCodeResult((int)response.StatusCode);
                    }
                }

                //StringBuilder teamsMessageBuilder = new StringBuilder();
                //var branches = await githubClient.Repository.Branch.GetAll(owner, repo);

                //var latestCommit = branches
                //    .Select(branch => githubClient.Repository.Commit.Get(owner, repo, branch.Commit.Sha))
                //    .Select(task => task.Result)
                //    .OrderByDescending(commit => commit.Commit.Author.Date)
                //    .FirstOrDefault();

                //if (latestCommit != null)
                //{
                //    var commitInfo = await githubClient.User.Get(latestCommit.Author.Login);
                //    string commitUrl = string.Format(commitUrlFormat, owner, repo, latestCommit.Sha);

                //    teamsMessageBuilder.AppendLine("***The committer:*** " + commitInfo.Name + commitInfo.Login);
                //    teamsMessageBuilder.AppendLine();
                //    teamsMessageBuilder.AppendLine("***Commit content:*** " + latestCommit.Commit.Message);
                //    teamsMessageBuilder.AppendLine();
                //    teamsMessageBuilder.AppendLine("[See details on Git](" + commitUrl + ")");
                //    teamsMessageBuilder.AppendLine();
                //}

                //if (teamsMessageBuilder.Length > 0)
                //{
                //    string teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");

                //    var httpClient = new HttpClient();
                //    var payloadData = new { text = teamsMessageBuilder.ToString() };
                //    var jsonPayload = JsonConvert.SerializeObject(payloadData);
                //    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                //    var response = await httpClient.PostAsync(teamsWebhookUrl, content);

                //    if (!response.IsSuccessStatusCode)
                //    {
                //        return new StatusCodeResult((int)response.StatusCode);
                //    }
                //}

                log.LogInformation("Function_Github_Teams completed successfully.");
                return new OkObjectResult("Get the latest commit and send the message successfully!");
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

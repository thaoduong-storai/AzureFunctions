﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
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

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"{requestBody}");

            dynamic payload = JsonConvert.DeserializeObject(requestBody);

            try
            {
                string teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");

                string sha = payload.head_commit != null ? payload.head_commit.id : null;
                string commitUrl = payload.head_commit != null ? payload.head_commit.url : null;
                string commitMessage = payload.head_commit != null ? payload.head_commit.message : null;
                string committerName = payload.head_commit != null ? payload.head_commit.author?.name : null;
                string commitTimestamp = payload.head_commit != null ? payload.head_commit.timestamp : null;

                string repositoryFullName = payload.repository != null ? payload.repository.full_name : null;
                int pullRequestNumber = payload.pull_request != null ? payload.pull_request.number : 0;
                string pullRequestState = payload.pull_request != null ? payload.pull_request.state : null;
                bool pullRequestMerged = payload.pull_request != null ? payload.pull_request.merged : false;
                bool isPullRequest = !string.IsNullOrEmpty(pullRequestState);

                string teamsMessage = $"***Id:*** {sha}\n\n";
                teamsMessage += $"***The committer:*** {committerName}\n\n";
                teamsMessage += $"***Commit content:*** {commitMessage}\n\n";
                teamsMessage += $"***Timestamp:*** {commitTimestamp}\n\n";
                teamsMessage += $"[See details on Git]({commitUrl})\n\n";

                if (isPullRequest)
                {
                    bool isApproved = pullRequestMerged && pullRequestState == "closed";

                    if (isApproved)
                    {
                        log.LogInformation("Pull request has been approved and merged.");
                        teamsMessage += "\n\nPull request has been approved and merged.";
                    }
                    else
                    {
                        log.LogInformation("Pull request has not been approved and merged.");
                        teamsMessage += "\n\nPull request has not been approved and merged.";
                    }
                }

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



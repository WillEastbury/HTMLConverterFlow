using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using FastTrackForAzure.DTO;
using System.Collections.Generic;
using System.Net.Http.Headers;
using DotLiquid;
using System.Linq;

namespace FastTrackForAzure
{
    public class GenerateHTMLReport
    {
        [FunctionName("GenerateHTMLReport")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GenerateHTMLReport/{TemplateToUse}/{IncludeBuckets}/{MatchProjectType}/{HoursToFilter}/{HoursToYellow}/{ReportTitle}/{SuppressIfTaken}")] HttpRequestMessage req, 
            string TemplateToUse,
            string IncludeBuckets,
            string MatchProjectType,
            int HoursToFilter,
            int HoursToYellow,
            string ReportTitle,
            bool SuppressIfTaken,
            ILogger log)
        {
            try
            {
                RootDTO DTO = JsonConvert.DeserializeObject<RootDTO>(await req.Content.ReadAsStringAsync());
                string templatecontent = await new HttpClient().GetStringAsync($"https://ftaautoemea.blob.core.windows.net/htmlcontainer/{TemplateToUse}.html");
                RenderViewModel PopulateProcessing = new RenderViewModel();
                IEnumerable<Bucket> selectedBuckets = null;
                PopulateProcessing.HrsFilter = HoursToFilter; 
                PopulateProcessing.HrsYellow = HoursToYellow; 
                PopulateProcessing.IncludeBuckets = IncludeBuckets;
                PopulateProcessing.ReportTitle = ReportTitle;
                PopulateProcessing.FilteredBy = MatchProjectType;
                PopulateProcessing.SuppressTakenItems = SuppressIfTaken; 

                if (IncludeBuckets == "All")
                {
                    selectedBuckets  = DTO.Buckets; 
                }
                else
                {
                    selectedBuckets = DTO.Buckets.Where(e => IncludeBuckets.Split(",").Contains(e.Name)); 
                }

                SortedDictionary<string,SummaryItem> thisdict = new SortedDictionary<string,SummaryItem>();
                
                foreach (Bucket bucket in selectedBuckets)
                {
                    switch (MatchProjectType)
                    {
                        case "All":
                        {
                            log.LogInformation($"Processing Bucket {bucket.Name}");
                            PopulateProcessing.Buckets.Add(new ViewBucket() {
                                Name = bucket.Name, 
                                BucketTasks = DTO.Tasks
                                    .Where(e => e.BucketId == bucket.Id 
                                        && e.DueInHours() <= HoursToFilter 
                                        && e.OutputConsideringSuppressIfTaken(SuppressIfTaken))
                                    .OrderBy(t => t.SortOrderTaken())
                                    .ThenBy(tsk => tsk.DueInHours())
                                .ToList()
                            });
                            break;
                        }

                        default:
                        {
                            log.LogInformation($"Processing Bucket {bucket.Name} for entries of {MatchProjectType}");
                            PopulateProcessing.Buckets.Add(new ViewBucket() {
                                Name = bucket.Name, 
                                BucketTasks = DTO.Tasks
                                    .Where(e => e.BucketId == bucket.Id 
                                        && e.DueInHours() <= HoursToFilter 
                                        && e.AskType() == MatchProjectType 
                                        && e.OutputConsideringSuppressIfTaken(SuppressIfTaken))
                                    .OrderBy(t => t.SortOrderTaken())
                                    .ThenBy(tsk => tsk.DueInHours())
                                .ToList()
                            });
                            break;
                        }
                    }
                }

                // For some types of engagements we want to add some additional fidelity around the ask type to the Summary Box
                List<string> enhancesplicer = new List<string>() {"Architecture Design Session", ""};

                // Loop through every task in every bucket and generate the summary data.
                foreach (ViewBucket bucket1 in PopulateProcessing.Buckets)
                {   
                    foreach (DTO.Task thistask in bucket1.BucketTasks)
                    {
                        // If this item is open and not taken.
                        if (thistask.OutputConsideringSuppressIfTaken(true))
                        {
                            log.LogInformation($"Found Output {thistask.Category()}-{thistask.SubType()} ({thistask.AskType()})");
                            // Do we already have a row for this buckettype, if so load it, if not use the default
                            string BucketTitle = "";
                            if (thistask.Category() == "Engagement")
                            {
                                log.LogInformation($"Testing for format:{thistask.SubType().Trim()}, {enhancesplicer.Contains(thistask.SubType().Trim())}");

                                if(enhancesplicer.Contains(thistask.SubType().Replace("-", "").Trim()))
                                {
                                     BucketTitle = $"{thistask.Category()}-{thistask.SubType().Replace("-", "").Trim()} ({thistask.AskType()})";
                                }
                                else
                                {
                                     BucketTitle = $"{thistask.Category()}-{thistask.SubType()}";
                                }
                            }
                            else
                            {
                                BucketTitle = $"{thistask.Category()}-{thistask.AskType()}";
                            }

                            BucketTitle = BucketTitle.Replace("- - ", "-");

                            SummaryItem thisitem = new SummaryItem(){SummaryBucketArea = BucketTitle, SummaryOldest = 800};
                            if (thisdict.ContainsKey(BucketTitle))
                            {
                                thisitem = thisdict[BucketTitle]; 
                            }

                            // How far back (AGE) is the oldest item, is it older than the current record for this item
                            if(thisitem.SummaryOldest > thistask.DueInHours()) {thisitem.SummaryOldest = thistask.DueInHours();}

                            thisitem.SummaryOldestDays = (int) Decimal.Round(thisitem.SummaryOldest / 24, 0);

                            //  this item is open so +1 
                            thisitem.SummaryOutstanding ++;

                            // How many open items are overdue? 
                            if(thistask.OutputConsideringSuppressIfTaken(true) && thistask.IsOverDue()) {thisitem.SummaryOverdueOutstanding ++;}

                            // Write it away (update if exists, add if it doesnt)
                            if (thisdict.ContainsKey(BucketTitle))
                            {
                                thisitem = thisdict[BucketTitle]; 
                            }
                            else
                            {
                                thisdict.Add(BucketTitle, thisitem);
                            }
                        }
                    }
                }

                PopulateProcessing.SummaryItems = thisdict.Values.ToList();
                
                if((PopulateProcessing.Buckets[0].BucketTasks.Count() + PopulateProcessing.Buckets[1].BucketTasks.Count() > 0))
                {

                    // We found some open tasks! 
                    Template template = Template.Parse(templatecontent);
                    Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();

                    Dictionary<string,object> renderparams = new Dictionary<string, object>();
                    renderparams.Add("data1", PopulateProcessing);
                    string outp = template.Render(Hash.FromDictionary(renderparams));
                    foreach(Exception err in template.Errors)
                    {
                        log.LogError(err.Message);

                    }
                    var response = new HttpResponseMessage(HttpStatusCode.OK); 
                    response.Content = new StringContent(outp);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    return response;

                }
                else
                {

                    var response = new HttpResponseMessage(HttpStatusCode.InternalServerError); 
                    response.Content = new StringContent("No Active Tasks Were Found in these buckets. Nothing to send!");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    return response;

                }
            }
            catch (Exception ex)
            {

                    var response = new HttpResponseMessage(HttpStatusCode.InternalServerError); 
                    response.Content = new StringContent($"An unhandled processing error occurred, ({ex.Message}) exiting.");
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                    return response;

            }

        }
    }
}



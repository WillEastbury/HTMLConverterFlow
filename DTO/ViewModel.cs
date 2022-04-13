using System;
using System.Collections.Generic;
using DotLiquid;
using FastTrackForAzure.DTO;

public class RenderViewModel : Drop
{
        public List<ViewBucket> Buckets = new List<ViewBucket>(); 
        public List<SummaryItem> SummaryItems = new List<SummaryItem>(); 
        public ViewBucket[] ViewBucketArray() {return Buckets.ToArray();}
        public SummaryItem[] SummaryItemArray() {return SummaryItems.ToArray();}
        public DateTime Now() {return DateTime.Now;}
        public string FilteredBy {get;set;} 
        public int HrsFilter {get;set;}
        public int HrsYellow {get;set;}
        public string IncludeBuckets {get;set;}
        public string ReportTitle {get;set;}
        public bool SuppressTakenItems {get;set;}
    }
public class ViewBucket : Drop
{
    public string Name {get;set;}
    public List<FastTrackForAzure.DTO.Task> BucketTasks = new List<FastTrackForAzure.DTO.Task>();

    public FastTrackForAzure.DTO.Task[] ViewBucketTasksArray() {return BucketTasks.ToArray();}
}

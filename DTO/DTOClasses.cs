using System.Collections.Generic;
using DotLiquid;
using System;
using System.Linq;

namespace FastTrackForAzure.DTO 
{
    public class Bucket {
        public string Id { get; set; } 
        public string Name { get; set; } 
    }
    public class Assignment : Drop   {
        public List<string> businessPhones { get; set; } 
        public string displayName { get; set; } 
        public string givenName { get; set; } 
        public string jobTitle { get; set; } 
        public string mail { get; set; } 
        public string mobilePhone { get; set; } 
        public string officeLocation { get; set; } 
        public string surname { get; set; } 
        public string userPrincipalName { get; set; } 
        public string id { get; set; } 
    }

    public class SummaryItem : Drop {
        public string SummaryBucketArea {get;set;}
        public int SummaryOldest {get;set;}
        public int SummaryOldestDays {get;set;}
        
		public int SummaryOutstanding {get;set;}
		public int SummaryOverdueOutstanding {get;set;}

    }
    public class Task : Drop {
        public string Title { get; set; } 
        public string Description { get; set; } 

        public string AbbreviatedDescription()
        {
            if (Description.Length < 1024)

                return Description.Replace("/r/n","</br>");

            else

                return Description.Replace("/r/n","</br>").Substring(0,1024);


        } 

        public bool OutputConsideringSuppressIfTaken(bool SuppressEnabled)
        {
            if (SuppressEnabled) 
            {
                if(IsTaken())
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public bool IsTaken() {

            return Assignments.Where(e => e.displayName != CreatedBy).Count() >= 1;

        }

        public int SortOrderTaken() {

            // So Taken stuff appears at the bottom but NOT sorted based on the number of assignees :) Otherwise any shadowing stuff appears right at the bottom.
            return (IsTaken() ? 1 : 0);
        }

        public int Takers() {

            return Assignments.Where(e => e.displayName != CreatedBy).Count();

        }

        public string IsTakenBy() {

            return string.Join(", ", 
            Assignments
                .Select(e => e.displayName)
                .Where(f => f != CreatedBy)
                );

        }

        public bool IsOverDue() {

            DateTime dt = DueDateTime ?? DateTime.Now;
            return dt <= DateTime.Now;

        }

        public int DueInHours() {

            DateTime dt = DueDateTime ?? DateTime.Now;
            return (int) Math.Round(dt.Subtract(DateTime.Now).TotalHours);

        }


        public DateTime? DueDateTime { get; set; } 
        public string InternationallyFormattedDueDateTime() {

            DateTime dt = DueDateTime ?? DateTime.MinValue;
            return dt.ToString("MMMM dd, yyyy HH:mm");
           
        }
        public string Id { get; set; } 
        public string BucketId { get; set; } 
        public string CreatedBy { get; set; } 
        public List<Assignment> Assignments { get; set; } 

        public string Customer() {

            return Title.Split("|")[0].Replace("Cx: ","").Trim();

        }
        public string Category() {
            
            if(Title.Split("|").Length > 1)
            {
                return Title.Split("|")[1].Trim();
            }
            else
            {
                return Title;
            }
        }
        public string AskType() {

            if(Title.Split("|").Length > 2)
            {
                return Title.Split("|")[2].Replace("Proj: ","").Replace("Eng: ","").Trim();
            }
            else
            {
                return Title;
            }
        }

        public string SubType() {

            if(Title.Split("|").Length > 3)
            {
                return " - " + Title.Split("|")[3].Replace("Proj: ","").Replace("Eng: ","").Trim();
            }
            else
            {
                return "";
            }
        }
    }
    public class RootDTO {
        public List<Bucket> Buckets { get; set; } 
        public List<DTO.SummaryItem> SummaryItems { get; set; } 
        public List<DTO.Task> Tasks { get; set; } 

        
    }
    
}
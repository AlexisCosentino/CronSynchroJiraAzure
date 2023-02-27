using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronSynchroJiraAzure
{
    public class JiraEntity
    {
        public string issueNb { get; set; }
        public string project { get; set; }
        public string reporter { get; set; }
        public string assignee { get; set; }
        public string creator { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
        public string created { get; set; }
        public string updated { get; set; }
        public string dueDate { get; set; }
        public string projectName { get; set; }
        public string issueType { get; set; }
        public string issueStatus { get; set; }
        public string priority { get; set; }
        public string componentList { get; set; }
        public string fixedVersionList { get; set; }
        public string labelList { get; set; }
        public string sprintList { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string worklog { get; set; }
        public string linkToJira { get; set; }
        public string projectCategoryType { get; set; }
        public string service { get; set; }
        public string originalEstimate { get; set; }
        public string linkChild { get; set; }
        public string linkParent { get; set; }
        public string azureLink { get; set; }   
        public string areaPath { get; set; }
        public string azureProject { get; set; }

        public JiraEntity() { }

        public void doMethods()
        {
            translateStatusToAzure();
            GetTypeOfPBI();
            GetProjectAndArea();
        }

        public void translateStatusToAzure()
        {
            switch (this.issueStatus)
            {
                case "Acceptée":
                    this.issueStatus = "New";
                    return;
                case "A Compléter":
                    this.issueStatus = "Unknown";
                    return ;
                case "Attente test":
                    this.issueStatus = "Done";
                    return ;
                case "Cloturée":
                    this.issueStatus = "Done";
                    return ;
                case "Demande":
                    this.issueStatus = "Unknown";
                    return ;
                case "EN ATTENTE":
                    this.issueStatus = "Approved";
                    return ;
                case "En cours":
                    this.issueStatus = "Approved";
                    return ;
                case "Rejetée":
                    this.issueStatus = "Removed";
                    return ;
                case "Terminée":
                    this.issueStatus = "DevDone";
                    return ;
                case "Test KO":
                    this.issueStatus = "Approved";
                    return ;
                case "A tester":
                    this.issueStatus = "To test";
                    return;
                case "A Valider":
                    this.issueStatus = "Unknown";
                    return;
                default:
                    this.issueStatus = "Unknown";
                    return;
            }
        }

        public void GetTypeOfPBI()
        {
            if (this.issueType == "Evolution " && this.projectName == "VEGA9008-Dev pour Maint Niv2")
            {
                this.issueType = "Evolution Support";
            }
            else if (this.issueType == "Evolution " && this.projectCategoryType == "Projet client (DPS)" && string.IsNullOrEmpty(this.componentList))
            {
                this.issueType = "Evolution Consultants";
            }
            else if (this.issueType == "Evolution " && this.projectCategoryType == "Projet interne (DDP)" && this.projectName != "VEGA9008-Dev pour Maint Niv2")
            {
                this.issueType = "Roadmap";
            }
            else if (this.issueType == "Evolution " && this.projectCategoryType == "Projet interne (DID)")
            {
                this.issueType = "Roadmap";
            }
            else if (this.projectCategoryType == "Projet client (DPS)" && !string.IsNullOrEmpty(this.componentList) && !this.componentList.Contains("VEGAMAINT"))
            {
                this.issueType = "Contrat";
            }
            else if (this.projectName == "VEGA0000 - Interne VEGA")
            {
                this.issueType = "Interne";
            }
            else if ((this.projectCategoryType == "Projet client (DPS)" || this.projectCategoryType == "Projet interne (DDP)" || this.projectCategoryType == "Projet interne (DID)") && (this.componentList.Contains("VEGAMAINT") || (this.issueType == "Bug")))
            {
                this.issueType = "Bug";
            }
            else if (this.projectName == "VEGA9008-Dev pour Maint Niv2" && this.issueType == "Bug")
            {
                this.issueType = "Bug";
            }
            else
            {
                this.issueType = "Bug";
            }

        }

        public void GetProjectAndArea()
        {
            if (this.projectCategoryType == "Projet interne (DID)" || this.projectCategoryType == "Projet interne (DDP)")
            {
                if (this.projectName == "VEGA9010 - Locpro Windows" || this.projectName == "VEGA9008-Dev pour Maint Niv2" || this.projectName == "VEGA9009 - Ergonomie / Design" || this.description.IndexOf("locpro win", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\LpWindows";
                }
                else if (this.projectName == "VEGA901W - Locpro Web" || this.description.IndexOf("locpro web", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\LpWeb";
                }
                else if (this.projectName == "VEGA901D - DevisLoc" || this.description.IndexOf("devisloc", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\DevisLoc";
                }
                else if (this.projectName == "VEGA9010 - Locpro Windows" && (this.labelList.Contains("LPservice") || this.labelList.Contains("LPService")))
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\LpService";
                }
                else if (this.projectName == "VEGA9010 - Locpro Windows" && this.labelList.Contains("LPSMS"))
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\LpSMS";
                }
                else if (this.projectName == "VEGA8001 - Master" || this.projectName == "VEGA8008 - Master AKANEA")
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\Master";
                }
                else if (this.projectName == "VEGA8005 - LpReportServer")
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\LpReportServer";
                }
                else if (this.projectName == "VEGA9006 - Tests Irium" || this.projectName == "VEGA9002 - Qualité / Tests")
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro\\\\Tests";
                }
                else if (this.projectName == "GYZMO10000 - Mobility" || this.description.IndexOf("Application (", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\_Global";
                }
                else if (this.projectName == "GYZMO10001 - Interventions")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\Interventions";
                }
                else if (this.projectName == "GYZMO10002 - iMob Check +")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobCheckPlus";
                }
                else if (this.projectName == "GYZMO10003 - Entrées / Sorties")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\EntréesSorties";
                }
                else if (this.projectName == "GYZMO10004 - Little Move")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\LittleMove";
                }
                else if (this.projectName == "GYZMO10005 - Gestion RH")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\GestionRH";
                }
                else if (this.projectName == "GYZMO10007 - iMob Rent 24/7")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobRent247";
                }
                else if (this.projectName == "GYZMO10008 - iMob Delivery")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobDelivery";
                }
                else if (this.projectName == "GYZMO10009 - H24 BTP")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobRentBTP";
                }
                else if (this.projectName == "GYZMO10010 - iMob Clock")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobClock";
                }
                else if (this.projectName == "GYZMO10011 - iMob Stock")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobStock";
                }
                else if (this.projectName == "GYZMO10012 - iMobService")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobService";
                }
                else if (this.projectName == "GYZMO10013 - IMobContact")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobContact";
                }
                else if (this.projectName == "GYZMO10014 - IMobcheck")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobCheck";
                }
                else if (this.projectName == "GYZMO10015 - iMob Expertises")
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility\\\\iMobExpertise";
                }
                else if (this.projectName == "VEGA7001 - SAAS LP TRACKER")
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\iTracker";
                }
                else if (this.projectName == "VEGA8002 - Site Web" || this.description.IndexOf("site internet", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\iWebRent";
                }
                else if (this.projectName == "VEGA8003 - LP3K" || this.description.IndexOf("locpro 3k", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\Lp3K";
                }
                else if (this.projectName == "VEGA8004 - LpAPI Back End")
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\LpBackEnd";
                }
                else if (this.projectName == "VEGA8006 - BI Noyau")
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\BI";
                }
                else if (this.projectName == "VEGA8007 - Locpro Fleet")
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital\\\\iWebFleet";
                }
                else if (this.projectName == "VEGA0000 - Interne VEGA")
                {
                    this.azureProject = "Interne Service";
                    this.areaPath = "Interne Service";
                    this.issueType = "Issue";
                }
                else if (this.projectName == "VEGA9999 - Recherche")
                {
                    this.azureProject = "Interne Service";
                    this.areaPath = "Interne Service\\\\Recherche";
                    this.issueType = "Issue";
                }
                else if (this.projectName == "INEOS")
                {
                    this.azureProject = "Interne Service";
                    this.areaPath = "Interne Service\\\\Neos";
                    this.issueType = "Issue";
                }
                else
                {
                    this.azureProject = "TEST_ALEXIS";
                    this.areaPath = "TEST_ALEXIS";
                }
            }
            else if (this.projectCategoryType == "Projet client (DPS)")
            {
                if (this.service == "DevAgilité" || this.service == "Développement" || this.description.IndexOf("locpro win", StringComparison.CurrentCultureIgnoreCase) >= 0 || this.description.IndexOf("locpro web", StringComparison.CurrentCultureIgnoreCase) >= 0 || this.description.IndexOf("devisloc", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Locpro";
                    this.areaPath = "Locpro";
                }
                else if (this.service == "Mobilité" || this.description.IndexOf("Application (", StringComparison.CurrentCultureIgnoreCase) >= 0 || this.description.IndexOf("site internet", StringComparison.CurrentCultureIgnoreCase) >= 0 || this.description.IndexOf("locpro 3k", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    this.azureProject = "Mobility";
                    this.areaPath = "Mobility";
                }
                else if (this.service == "Digital")
                {
                    this.azureProject = "Digital";
                    this.areaPath = "Digital";
                }
                else
                {
                    this.azureProject = "TEST_ALEXIS";
                    this.areaPath = "TEST_ALEXIS";
                }

            }
            else
            {
                this.azureProject = "TEST_ALEXIS";
                this.areaPath = "TEST_ALEXIS";
            }

        }

        public void printEntity()
        {
            foreach (var prop in GetType().GetProperties())
            {
                Console.WriteLine($"{prop.Name}: {prop.GetValue(this)}");
            }
            Console.WriteLine("---------------------------------------------");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
namespace BasicPlugin
{
    public class FollowupPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider) {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity) {
                Entity entity = (Entity)context.InputParameters["Target"];

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try {
                    //Create a task
                    Entity followup = new Entity("task");
                    followup["subject"] = "Send email to the new customer";
                    followup["description"] = "Followup with the customer,Check if there are new issue that needs resolution";
                    followup["scheduledstart"] = DateTime.Now.AddDays(7);
                    followup["scheduledend"] = DateTime.Now.AddDays(7);
                    followup["category"] = context.PrimaryEntityName;

                    if (context.OutputParameters.Contains("id")) {
                        Guid accountid = new Guid(context.OutputParameters["id"].ToString());
                        followup["regardingobjectid"] = new EntityReference("account", accountid);
                    };

                    tracingService.Trace("Followup Plugin : Craeting the task activity");
                    service.Create(followup);
                }
                catch (FaultException<OrganizationServiceFault> ex) {
                    throw new InvalidPluginExecutionException("An error occured in followup plugin ",ex);
                }
                catch (Exception ex) {
                    tracingService.Trace("Followup PLugin : {0}", ex.ToString());
                    throw;
                }
            };

        }
    }

    public class ValidateAccountName : IPlugin
    {
        private List<String> invalidNames = new List<String>();
        public ValidateAccountName(string unsecureConfig)
        {
            if (!string.IsNullOrWhiteSpace(unsecureConfig))
            {
                unsecureConfig.Split(',').ToList().ForEach(s => {
                    invalidNames.Add(s.Trim());
                });
            }
        }
        public void Execute(IServiceProvider serviceProvider)
        {
            //Obtain tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try
            {

                //Obtain Execution Context
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                //Check if target exists and is entity
                if (context.InputParameters.Contains("Target") &&
                    context.InputParameters["Target"] is Entity &&
                    ((Entity)context.InputParameters["Target"]).LogicalName.Equals("account") &&
                    ((Entity)context.InputParameters["Target"])["name"] != null &&
                    context.MessageName.Equals("update") &&
                    context.PreEntityImages["a"] != null &&
                    context.PreEntityImages["a"]["name"] != null
                    )
                {
                    Entity entity = (Entity)context.InputParameters["Target"];
                    var oldAccountName = (string)context.PreEntityImages["a"]["name"];
                    var newAccountName = (string)entity["name"];
                    if (invalidNames.Count > 0)
                    {
                        if (invalidNames.Contains(newAccountName.ToLower().Trim()))
                        {
                            tracingService.Trace("ValidateAccountName: new name '{0}' found in invalid names.", newAccountName);
                            if (!oldAccountName.ToLower().Contains(newAccountName.ToLower().Trim()))
                            {
                                tracingService.Trace("ValidateAccountName: new name '{0}' not found in '{1}'.", newAccountName, oldAccountName);
                                string message = string.Format("You can't change the name of this account from '{0}' to '{1}'.", oldAccountName, newAccountName);
                                throw new InvalidPluginExecutionException(message);
                            }
                        }
                    }
                    else
                    {
                        tracingService.Trace("VlidateAccountName : No invalid names passed in configuration");
                    }
                }
                else
                {
                    tracingService.Trace("ValidateAccountName : plugin step is not configured correctly");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("BasicPlugin: {0}", ex.ToString());
                throw;
            };
        }
    }
}

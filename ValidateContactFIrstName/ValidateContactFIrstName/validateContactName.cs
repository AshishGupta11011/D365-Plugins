using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace ValidateContactFIrstName
{
    public class PreValidationContactFirstName : IPlugin
    {
        private string str = "";
        public PreValidationContactFirstName(string unsecureConfig) {
            str = unsecureConfig;
        }
        public void Execute(IServiceProvider serviceProvider) {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            try {
                tracingService.Trace("ValidateContactName: preval started");
                //Code here
                if (context.InputParameters.Contains("Target") &&
                    context.InputParameters["Target"] is Entity &&
                    ((Entity)context.InputParameters["Target"]).LogicalName.Equals("contact") &&
                     ((Entity)context.InputParameters["Target"])["firstname"] != null &&
                     context.MessageName.Equals("Update")
                    )
                {
                    var conName = (string)(((Entity)context.InputParameters["Target"])["firstname"]);

                    if (conName.Contains("abcd"))
                    {
                        tracingService.Trace("ValidateContactName (PreVal) :  FirstName contains abcd");
                        throw new InvalidPluginExecutionException("ValidateContactName : FirstName contains abcd");
                    };

                    tracingService.Trace("ValidateContactName : preval Ended");
                }
                else {
                tracingService.Trace("ValidateContactName (PreVal) : PLugin is not configured correctly");
                }
            }
            catch (Exception ex) {
                tracingService.Trace("ValidateContactName (PreVal) : Error Occured {0}",ex.ToString());
                throw;
            };
        }

    }

    public class PreOperationContactFirstName : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            try {
                tracingService.Trace("ValidateContactName (Pre) : Plugin Starts");
                if (
                    context.InputParameters.Contains("Target") &&
                    context.InputParameters["Target"] is Entity &&
                    ((Entity)context.InputParameters["Target"]).LogicalName.Equals("contact") &&
                    ((Entity)context.InputParameters["Target"])["firstname"] != null &&
                    context.PreEntityImages["a"] != null &&
                    context.PreEntityImages["a"]["firstname"] != null &&
                    context.MessageName.Equals("Update")
                    ) {
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    var originalConName = (string)context.PreEntityImages["a"]["firstname"];
                    //Check if Original FirstName  contains Ashish
                    if (originalConName.Contains("Ashish"))
                    {
                        Entity FollowupTask = new Entity("task");
                        FollowupTask["subject"] = "FirstName Contains Ashish";
                        //Guid contactId = new Guid(context.OutputParameters["id"].ToString());
                        Guid contactId = new Guid(context.PrimaryEntityId.ToString());
                        FollowupTask["regardingobjectid"] = new EntityReference("contact",contactId);
                        service.Create(FollowupTask);
                    };
                   
                    context.SharedVariables["xyz"] = "PPPPPPP";
           
                    tracingService.Trace("ValidateContactName (Pre) : Plugin Ends");
                }
                else {
                    tracingService.Trace("ValidateContactName (Pre) : Plugin is not configured correctly");
                };
            } catch (Exception ex) {
                tracingService.Trace("ValidateContactName (Pre) : Error Occured {0}", ex.ToString());
                throw; };
        }

    }

    public class PostOperationContactFirstName : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            try {
                tracingService.Trace("ValidateContactName (Post) : PLUGIN STARTS");
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                if (
                    context.InputParameters.Contains("Target") &&
                    context.InputParameters["Target"] is Entity &&
                    ((Entity)context.InputParameters["Target"]).LogicalName.Equals("contact") &&
                    context.MessageName.Equals("Update") &&
                    ((Entity)context.InputParameters["Target"])["firstname"] != null &&
                    context.PostEntityImages["postImage"] != null &&
                    context.PostEntityImages["postImage"]["lastname"] != null &&
                    context.PostEntityImages["postImage"]["accountid"] != null &&
                    context.SharedVariables.Contains("xyz")
                    )
                {
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    //Query lastname
                    string firstName = (string)((Entity)context.InputParameters["Target"])["firstname"];
                    string lastName = (string)context.PostEntityImages["postImage"]["lastname"];
                    Entity accountObj = (Entity)context.PostEntityImages["postImage"];
                    Guid accountId = new Guid(accountObj.GetAttributeValue<EntityReference>("accountid").Id.ToString());
                    //Check if lastname contains 
                    if (lastName.Contains("Gupta") || lastName.Equals(firstName))
                    {
                        if (accountId != null) {
                            Entity account = new Entity("account");
                            account.Id = accountId;
                            account["name"] = firstName + lastName ;
                            service.Update(account);
                        }
                    }
                    //Get the sharedvariable
                    string varXyz = (string)context.SharedVariables["xyz"];
                    Entity contact = new Entity("contact");
                    contact.Id = ((Entity)context.InputParameters["Target"]).Id;
                    contact["jobtitle"] = varXyz;
                    service.Update(contact);
                    tracingService.Trace("ValidateContactName (Post) : PLUGIN ENDS");
                }
                else
                {
                    tracingService.Trace("ValidatecontactName (post): Step is not configured correctly");
                }

            }
            catch (FaultException<OrganizationServiceFault> ex) {
                throw new InvalidPluginExecutionException("");
            }
            catch (Exception ex) {
                tracingService.Trace("");
                throw;
            }
        }

    }
}

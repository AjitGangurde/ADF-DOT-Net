using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using System;
using System.Collections.Generic;

namespace ADFDotNetSDK
{
    public class ADFClass
    {
        public string LinkServiceName { get; set; }
        public string LinkSrviceType { get; set; }
        public string DataSetName { get; set; }
        public string DataSetType { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            string resourceGroup = "Rsdautomation";
            string region = "westus";
            string dataFactoryName = "TestajitADFS-1";


            List<ADFClass> aDFClasses = new List<ADFClass>();
            ADFClass adfSQL = new ADFClass();
            ADFClass adfCDS = new ADFClass();
            ADFClass adfCRM = new ADFClass();
            ADFClass adfKeyVault = new ADFClass();
            adfSQL.DataSetName = "CSMEDataSet";
            adfCDS.DataSetName = "CDS-SCD";
            adfCRM.DataSetName = "Test-CRM";


            adfSQL.LinkServiceName = "sqlLinkServiceName";
            adfCDS.LinkServiceName = "cdsLinkServiceName";
            adfCRM.LinkServiceName = "crmLinkServiceName";
            adfKeyVault.LinkServiceName = "keyVaultLinkServiceName";

            adfSQL.LinkSrviceType = nameof(AzureSqlDatabaseLinkedService);
            adfCDS.LinkSrviceType = nameof(CommonDataServiceForAppsLinkedService);
            adfCRM.LinkSrviceType = nameof(DynamicsCrmLinkedService);
            adfKeyVault.LinkSrviceType = nameof(AzureKeyVaultLinkedService);

            adfSQL.DataSetName = "CSMEDataSet";
            adfCDS.DataSetName = "CDS-SCD";

            adfSQL.DataSetType = nameof(AzureSqlTableDataset);
            adfCDS.DataSetType = nameof(CommonDataServiceForAppsEntityDataset);
            aDFClasses.Add(adfCRM);
            aDFClasses.Add(adfSQL);
            aDFClasses.Add(adfCDS);
            aDFClasses.Add(adfKeyVault);

            var client = CreateDataFactoryManagementClient();
            if (client != null)
            {
                    Console.WriteLine("Creating ADF........");
                if (CreateDataFactory(client, resourceGroup, dataFactoryName, region))
                {
                    Console.WriteLine("ADF Created successfully..");
                    foreach (var item in aDFClasses)
                    {
                        Console.WriteLine("Creating Link Service........");
                        CreateLinkService(client, resourceGroup, dataFactoryName, item.LinkSrviceType, item.LinkServiceName);
                        Console.WriteLine("Link Service Created successfully..");
                        if (!string.IsNullOrEmpty(item.LinkServiceName) && !string.IsNullOrEmpty(item.DataSetType))
                        {
                            Console.WriteLine("Creating DataSet........");
                            CreateDataSet(client, resourceGroup, dataFactoryName, item.DataSetType, item.DataSetName, item.LinkServiceName);
                            Console.WriteLine("Data Set Created successfully..");
                        }
                    }

                    Console.WriteLine("Creating Pipeline........");
                    CreatePipeLine(client, resourceGroup, dataFactoryName, "SourceName", "CopyDataFromSQLToCRM",
                       adfSQL.DataSetName,
                        adfCDS.DataSetName, "CopyDataFromSQLToCRM");
                    Console.WriteLine("Pipeline Created successfully..");
                    Console.ReadLine();
                }
            }
        }


        public static DataFactoryManagementClient CreateDataFactoryManagementClient()
        {
            try
            {
                string tenantID = Config.TenantId;
                string applicationId = Config.ProdClientId;
                string authenticationKey = KeyVault.DefaultInstance.GetSecretValue("ClientConnector");

                string subscriptionId = Config.SubscriptionId;



                // Authenticate and create a data factory management client
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(applicationId)
                 .WithAuthority("https://login.microsoftonline.com/" + tenantID)
                 .WithClientSecret(authenticationKey)
                 .WithLegacyCacheCompatibility(false)
                 .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                 .Build();

                AuthenticationResult result = app.AcquireTokenForClient(
                  new string[] { "https://management.azure.com//.default" })
                   .ExecuteAsync().Result;
                ServiceClientCredentials cred = new TokenCredentials(result.AccessToken);
                var client = new DataFactoryManagementClient(cred)
                {
                    SubscriptionId = subscriptionId
                };

                return client;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool CreateDataFactory(DataFactoryManagementClient client, string resourceGroup, string dataFactoryName, string region)
        {
            try
            {
                Console.WriteLine("Creating data factory " + dataFactoryName + "...");
                Factory dataFactory = new Factory
                {
                    Location = region,
                    Identity = new FactoryIdentity()
                };
                client.Factories.CreateOrUpdate(resourceGroup, dataFactoryName, dataFactory);
                Console.WriteLine(
                    SafeJsonConvert.SerializeObject(dataFactory, client.SerializationSettings));

                while (client.Factories.Get(resourceGroup, dataFactoryName).ProvisioningState ==
                       "PendingCreation")
                {
                    System.Threading.Thread.Sleep(1000);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool CreateLinkService(DataFactoryManagementClient client, string resourceGroup, string dataFactoryName, string type, string name)
        {
            try
            {
                LinkedServiceResource linkserviceResourceObject = null;
                // Create an Azure Storage linked service
                Console.WriteLine("Creating linked service " + name + "...");
                switch (type)
                {
                    case nameof(AzureSqlDatabaseLinkedService):
                        linkserviceResourceObject = new LinkedServiceResource(
                               new AzureSqlDatabaseLinkedService
                               {
                                   ConnectionString = new SecureString(KeyVault.DefaultInstance.GetSecretValue("CSMERSDAutomation-Prod-Connection"))
                               }
                       );
                        break;
                    case nameof(CommonDataServiceForAppsLinkedService):
                        linkserviceResourceObject = new LinkedServiceResource(
                              new CommonDataServiceForAppsLinkedService
                              {
                                  DeploymentType = "Online",
                                  ServiceUri = Config.DynamicsLink,
                                  AuthenticationType = "AADServicePrincipal",
                                  ServicePrincipalCredentialType = "ServicePrincipalKey",
                                  ServicePrincipalId = Config.ProdClientId,
                                  ServicePrincipalCredential = new SecureString(KeyVault.DefaultInstance.GetSecretValue("SCD-DEV-API")),
                                  ///App Secret value.
                                  //servicePrincipalCredentialType
                              });
                        break;
                    case nameof(AzureBlobStorageLinkedService):
                        break;
                    case nameof(DynamicsCrmLinkedService):
                        Console.WriteLine("Creating CRM linked service crmLinkService-POC..");

                        linkserviceResourceObject = new LinkedServiceResource(
                            new DynamicsCrmLinkedService
                            {
                                DeploymentType = "Online",
                                ServiceUri = Config.DynamicsLink,
                                AuthenticationType = "AADServicePrincipal",
                                ServicePrincipalCredentialType = "ServicePrincipalKey",
                                ServicePrincipalId = Config.ProdClientId,
                                ServicePrincipalCredential = new SecureString(KeyVault.DefaultInstance.GetSecretValue("SCD-DEV-API"))
                                ///App Secret value.
                            });
                        break;
                    case nameof(AzureKeyVaultLinkedService):
                        string KeyVaulyLink = "SCDKeyVault";
                        // Create an Azure Storage linked service
                        Console.WriteLine("Creating linked service " + KeyVaulyLink + "...");

                        linkserviceResourceObject = new LinkedServiceResource(
                            new AzureKeyVaultLinkedService(
                                Config.VaultUrl,
                            null,
                            null,
                            "Description about KeyVault Link Service")
                            );
                        break;

                }

                if (linkserviceResourceObject != null && !string.IsNullOrEmpty(name))
                {
                    client.LinkedServices.CreateOrUpdate(
                        resourceGroup, dataFactoryName, name, linkserviceResourceObject);
                    Console.WriteLine(SafeJsonConvert.SerializeObject(
                        linkserviceResourceObject, client.SerializationSettings));
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }

        }


        public static bool CreateDataSet(DataFactoryManagementClient client, string resourceGroup, string dataFactoryName, string type, string name, string linkserviceReference)
        {
            try
            {
                DatasetResource datasetResource = null;
                switch (type)
                {
                    case nameof(AzureSqlTableDataset):

                        LinkedServiceReference linkedServiceReference = new LinkedServiceReference
                        {
                            ReferenceName = linkserviceReference
                        };
                        AzureSqlTableDataset azureSqlTableDataset = new AzureSqlTableDataset(linkedServiceReference,
                            null, "Dataset Description ", null, null, null, null, null,
                            "tbl_userinfo",
                            null,
                            "tbl_userinfo");
                        //Replace table Names as  your requirement.
                        string sqlDatasetName = nameof(sqlDatasetName);
                        Console.WriteLine("Creating dataset " + sqlDatasetName + "...");

                        datasetResource = new DatasetResource(
                            azureSqlTableDataset, "CSMESQL"
                        );
                        break;
                    case nameof(CommonDataServiceForAppsEntityDataset):
                        datasetResource = new DatasetResource(
                            new CommonDataServiceForAppsEntityDataset
                            {
                                LinkedServiceName = new LinkedServiceReference
                                {
                                    ReferenceName = linkserviceReference
                                },
                                EntityName = "contact",
                                ///Entity name from the Microsoft Dynamics
                               
                            }
                        );
                        break;
                }

                if (datasetResource != null)
                {
                    client.Datasets.CreateOrUpdate(
                  resourceGroup, dataFactoryName, name, datasetResource);
                    Console.WriteLine(
                    SafeJsonConvert.SerializeObject(datasetResource, client.SerializationSettings));
                }
                return true;

            }
            catch (Exception e)
            {
                throw;
            }
        }


        public static bool CreatePipeLine(DataFactoryManagementClient client, string resourceGroup, string dataFactoryName, string sourceName,
            string detinationRefrenceName, string inputDataSetRefernceName, string outputDataSetRefrenceName, string name)
        {
            try
            {
                PipelineResource pipeline = new PipelineResource
                {
                    ///Declare the parameters 
                    ////Parameters = new Dictionary<string, ParameterSpecification>
                    ////{
                    ////    { "inputPath", new ParameterSpecification { Type = ParameterType.String } },
                    ////    { "outputPath", new ParameterSpecification { Type = ParameterType.String } }
                    ////},
                    Activities = new List<Activity>
                    {
                    new CopyActivity
                    {
                        Name = "sourrceName",
                        Inputs = new List<DatasetReference>
                        {
                            new DatasetReference()
                            {
                                ReferenceName = inputDataSetRefernceName ,//"sqlDatasetName",
                                //Parameters = new Dictionary<string, object>
                                //{
                                //    { "path", "@pipeline().parameters.inputPath" }
                                //}
                            }
                        },
                        Outputs = new List<DatasetReference>
                        {

                            new DatasetReference
                            {
                                ReferenceName =outputDataSetRefrenceName,// "scdDataSetPOC",
                                //Parameters = new Dictionary<string, object>
                                //{
                                //    { "path", "@pipeline().parameters.outputPath" }
                                //}
                            }
                        },
                       //Translator= columnmappingtabuler,
                        Source = new SqlSource { },
                        //AlternateKeyName="scd_employee",
                        Sink = new CommonDataServiceForAppsSink {IgnoreNullValues=true,WriteBatchSize=50 },
                    }
                }
                };
                client.Pipelines.CreateOrUpdate(resourceGroup, dataFactoryName, name, pipeline);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }


}

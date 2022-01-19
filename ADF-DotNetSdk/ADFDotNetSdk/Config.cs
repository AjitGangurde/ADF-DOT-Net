using System;
using System.Configuration;

namespace ADFDotNetSDK
{
    public class Config
    {
        private static bool keyvaultUsingCertificate;
        private static string vaultUrl;
        private static string subscriptionId;
        private static string clientId;
        private static string prodClientId;
        private static string certThumbprint;
        private static string authority;
        private static string tenantId;
        private static string dynamicsLink;
        public static bool KeyvaultUsingCertificate
        {
            get
            {
                keyvaultUsingCertificate = Convert.ToBoolean(ConfigurationManager.AppSettings[nameof(KeyvaultUsingCertificate)]);
                return keyvaultUsingCertificate;
            }
        }
        public static string VaultUrl
        {
            get
            {
                if (string.IsNullOrEmpty(vaultUrl))
                {
                    vaultUrl = Convert.ToString(ConfigurationManager.AppSettings[nameof(VaultUrl)]);
                }

                return vaultUrl;
            }
        }
        public static string ClientId
        {
            get
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = Convert.ToString(ConfigurationManager.AppSettings[nameof(ClientId)]);
                }

                return clientId;
            }
        }
        public static string CertThumbprint
        {
            get
            {
                if (string.IsNullOrEmpty(certThumbprint))
                {
                    certThumbprint = Convert.ToString(ConfigurationManager.AppSettings[nameof(CertThumbprint)]);
                }

                return certThumbprint;
            }
        }
        public static string Authority
        {
            get
            {
                if (string.IsNullOrEmpty(authority))
                {
                    authority = Convert.ToString(ConfigurationManager.AppSettings[nameof(Authority)]);
                }

                return authority;
            }
        }
        public static string TenantId
        {
            get
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = Convert.ToString(ConfigurationManager.AppSettings[nameof(TenantId)]);
                }

                return tenantId;
            }
        }
        public static string ProdClientId
        {
            get
            {
                if (string.IsNullOrEmpty(prodClientId))
                {
                    prodClientId = Convert.ToString(ConfigurationManager.AppSettings[nameof(ProdClientId)]);
                }

                return prodClientId;
            }
        }

        public static string SubscriptionId
        {
            get
            {
                if (string.IsNullOrEmpty(subscriptionId))
                {
                    subscriptionId = Convert.ToString(ConfigurationManager.AppSettings[nameof(SubscriptionId)]);
                }

                return subscriptionId;
            }
        }


        public static string DynamicsLink
        {
            get
            {
                if (string.IsNullOrEmpty(dynamicsLink))
                {
                    dynamicsLink = Convert.ToString(ConfigurationManager.AppSettings[nameof(DynamicsLink)]);
                }

                return dynamicsLink;
            }
        }
    }
}
